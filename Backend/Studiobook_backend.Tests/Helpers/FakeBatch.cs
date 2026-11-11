using Studiobook_backend.Application.Batch;
using Studiobook_backend.Application.Interfaces;

namespace Studiobook_backend.Tests.Helpers;

/// <summary>ロックの取得結果を切り替えられるテスト用のバッチロック。</summary>
public sealed class FakeBatchJobLock : IBatchJobLock
{
    /// <summary>false にすると「ほかで実行中」として null を返す。</summary>
    public bool CanAcquire { get; set; } = true;

    public List<string> AcquiredJobNames { get; } = new();

    public Task<IAsyncDisposable?> TryAcquireAsync(string jobName, CancellationToken cancellationToken = default)
    {
        if (!CanAcquire)
        {
            return Task.FromResult<IAsyncDisposable?>(null);
        }

        AcquiredJobNames.Add(jobName);
        return Task.FromResult<IAsyncDisposable?>(new Releaser());
    }

    private sealed class Releaser : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

/// <summary>実行回数を数えるテスト用のジョブ。</summary>
public sealed class FakeBatchJob : IBatchJob
{
    public FakeBatchJob(string name = "fake_job")
    {
        Name = name;
    }

    public string Name { get; }

    public int RunCount { get; private set; }

    public Exception? ExceptionToThrow { get; set; }

    public Task<BatchJobResult> RunAsync(CancellationToken cancellationToken)
    {
        RunCount++;

        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult(new BatchJobResult(3, "deleted=3"));
    }
}
