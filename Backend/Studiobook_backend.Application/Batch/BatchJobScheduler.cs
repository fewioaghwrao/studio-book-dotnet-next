using Microsoft.Extensions.Options;
using Studiobook_backend.Application.Settings;

namespace Studiobook_backend.Application.Batch;

/// <summary>
/// 登録済みのジョブのうち、実行時刻を過ぎて未実行のものを実行する（定期実行の 1 回分）。
/// </summary>
public class BatchJobScheduler
{
    private readonly IEnumerable<IBatchJob> _jobs;
    private readonly BatchJobRunner _runner;
    private readonly BatchJobStateStore _stateStore;
    private readonly BatchSettings _settings;
    private readonly TimeProvider _timeProvider;

    public BatchJobScheduler(
        IEnumerable<IBatchJob> jobs,
        BatchJobRunner runner,
        BatchJobStateStore stateStore,
        IOptions<BatchSettings> settings,
        TimeProvider? timeProvider = null)
    {
        _jobs = jobs;
        _runner = runner;
        _stateStore = stateStore;
        _settings = settings.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <returns>実行したジョブ名と結果</returns>
    public async Task<IReadOnlyDictionary<string, BatchRunStatus>> RunDueJobsAsync(CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, BatchRunStatus>();
        var runAt = _settings.GetRunAtJapanTime();

        foreach (var job in _jobs)
        {
            var lastSucceededUtc = await _stateStore.GetLastSucceededUtcAsync(job.Name, cancellationToken);
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

            if (!BatchSchedule.IsDue(lastSucceededUtc, nowUtc, runAt))
            {
                continue;
            }

            results[job.Name] = await _runner.RunAsync(job, cancellationToken);
        }

        return results;
    }

    /// <summary>ジョブ名を指定して実行する（コマンドからの手動実行用）。見つからなければ null。</summary>
    public async Task<BatchRunStatus?> RunByNameAsync(string jobName, CancellationToken cancellationToken = default)
    {
        var job = _jobs.FirstOrDefault(x => string.Equals(x.Name, jobName, StringComparison.Ordinal));

        return job == null
            ? null
            : await _runner.RunAsync(job, cancellationToken);
    }

    public IReadOnlyList<string> JobNames => _jobs.Select(x => x.Name).ToList();
}
