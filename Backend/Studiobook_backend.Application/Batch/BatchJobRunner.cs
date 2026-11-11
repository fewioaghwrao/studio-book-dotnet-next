using Microsoft.Extensions.Logging;
using Studiobook_backend.Application.Interfaces;

namespace Studiobook_backend.Application.Batch;

public enum BatchRunStatus
{
    /// <summary>実行して成功した。</summary>
    Succeeded,

    /// <summary>ほかで実行中だったため実行しなかった。</summary>
    SkippedLocked,

    /// <summary>実行したが失敗した（例外はログに記録済み。次の確認で再実行される）。</summary>
    Failed
}

/// <summary>
/// バッチジョブを 1 つ実行する：ロック取得 → 実行 → 結果を AppSettings とログに記録。
/// 失敗しても例外を外へ出さない（定期実行のループや Web アプリを止めない）。
/// </summary>
public class BatchJobRunner
{
    private readonly IBatchJobLock _lock;
    private readonly BatchJobStateStore _stateStore;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BatchJobRunner> _logger;

    public BatchJobRunner(
        IBatchJobLock jobLock,
        BatchJobStateStore stateStore,
        ILogger<BatchJobRunner> logger,
        TimeProvider? timeProvider = null)
    {
        _lock = jobLock;
        _stateStore = stateStore;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<BatchRunStatus> RunAsync(IBatchJob job, CancellationToken cancellationToken = default)
    {
        await using var acquired = await _lock.TryAcquireAsync(job.Name, cancellationToken);

        if (acquired == null)
        {
            _logger.LogInformation("Batch job skipped because it is already running. Job={Job}", job.Name);
            return BatchRunStatus.SkippedLocked;
        }

        var startedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        _logger.LogInformation("Batch job started. Job={Job}", job.Name);

        try
        {
            var result = await job.RunAsync(cancellationToken);

            await _stateStore.RecordSucceededAsync(job.Name, startedUtc, result.Summary, cancellationToken);

            _logger.LogInformation(
                "Batch job succeeded. Job={Job}, AffectedRows={AffectedRows}, Result={Result}",
                job.Name,
                result.AffectedRows,
                result.Summary);

            return BatchRunStatus.Succeeded;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // アプリの停止時。次回の起動後に再実行される
            _logger.LogInformation("Batch job canceled. Job={Job}", job.Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch job failed. Job={Job}", job.Name);

            try
            {
                await _stateStore.RecordFailedAsync(
                    job.Name,
                    _timeProvider.GetUtcNow().UtcDateTime,
                    ex.GetType().Name,
                    CancellationToken.None);
            }
            catch (Exception recordEx)
            {
                _logger.LogError(recordEx, "Failed to record batch job failure. Job={Job}", job.Name);
            }

            return BatchRunStatus.Failed;
        }
    }
}
