using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Studiobook_backend.Application.Batch;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Application.Settings;
using Studiobook_backend.Batch;
using Studiobook_backend.Infrastructure.Persistence;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Batch;

public class BatchJobSchedulerTests
{
    // 現在：日本時間 2026/11/12 8:00（UTC 11/11 23:00）。実行時刻 4:00 は過ぎている
    private static FixedTimeProvider Clock => FixedTimeProvider.AtUtc(2026, 11, 11, 23, 0);

    private static (BatchJobScheduler Scheduler, BatchJobStateStore Store) Create(
        AppDbContext context,
        FakeBatchJobLock jobLock,
        params IBatchJob[] jobs)
    {
        var store = new BatchJobStateStore(context);
        var runner = new BatchJobRunner(jobLock, store, NullLogger<BatchJobRunner>.Instance, Clock);

        var scheduler = new BatchJobScheduler(
            jobs,
            runner,
            store,
            Options.Create(new BatchSettings { RunAtJapanTime = "04:00" }),
            Clock);

        return (scheduler, store);
    }

    [Fact]
    public async Task RunDueJobsAsync_実行時刻を過ぎた未実行のジョブを実行し_結果をAppSettingsに記録する()
    {
        await using var context = TestDbContextFactory.Create();
        var job = new FakeBatchJob("log_cleanup");
        var (scheduler, store) = Create(context, new FakeBatchJobLock(), job);

        var results = await scheduler.RunDueJobsAsync();

        Assert.Equal(BatchRunStatus.Succeeded, results["log_cleanup"]);
        Assert.Equal(1, job.RunCount);

        Assert.Equal(
            new DateTime(2026, 11, 11, 23, 0, 0, DateTimeKind.Utc),
            await store.GetLastSucceededUtcAsync("log_cleanup"));

        var result = await context.AppSettings.SingleAsync(x => x.Key == "batch:log_cleanup:last_result");
        Assert.Equal("ok:deleted=3", result.Value);

        // 同じ日のうちは再実行しない
        var second = await scheduler.RunDueJobsAsync();
        Assert.Empty(second);
        Assert.Equal(1, job.RunCount);
    }

    [Fact]
    public async Task RunDueJobsAsync_ほかで実行中ならスキップし_成功として記録しない()
    {
        await using var context = TestDbContextFactory.Create();
        var job = new FakeBatchJob();
        var (scheduler, store) = Create(context, new FakeBatchJobLock { CanAcquire = false }, job);

        var results = await scheduler.RunDueJobsAsync();

        Assert.Equal(BatchRunStatus.SkippedLocked, results["fake_job"]);
        Assert.Equal(0, job.RunCount);
        Assert.Null(await store.GetLastSucceededUtcAsync("fake_job"));
    }

    [Fact]
    public async Task RunDueJobsAsync_ジョブが失敗しても例外を外に出さず_失敗を記録して次回再実行する()
    {
        await using var context = TestDbContextFactory.Create();
        var failing = new FakeBatchJob("failing") { ExceptionToThrow = new InvalidOperationException("db error") };
        var other = new FakeBatchJob("other");
        var (scheduler, store) = Create(context, new FakeBatchJobLock(), failing, other);

        var results = await scheduler.RunDueJobsAsync();

        Assert.Equal(BatchRunStatus.Failed, results["failing"]);
        Assert.Equal(BatchRunStatus.Succeeded, results["other"]); // 1 つ失敗してもほかは動く

        Assert.Null(await store.GetLastSucceededUtcAsync("failing"));
        Assert.Equal(
            "failed:InvalidOperationException",
            (await context.AppSettings.SingleAsync(x => x.Key == "batch:failing:last_result")).Value);

        // 成功していないので次の確認でも実行される
        await scheduler.RunDueJobsAsync();
        Assert.Equal(2, failing.RunCount);
        Assert.Equal(1, other.RunCount);
    }

    [Fact]
    public async Task BatchCommand_ジョブ名を指定して実行し_終了コードを返す()
    {
        await using var context = TestDbContextFactory.Create();
        var job = new FakeBatchJob("log_cleanup");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAppDbContext>(context);
        services.AddSingleton<IBatchJobLock>(new FakeBatchJobLock());
        services.AddSingleton<IBatchJob>(job);
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton(Options.Create(new BatchSettings()));
        services.AddScoped<BatchJobStateStore>();
        services.AddScoped<BatchJobRunner>();
        services.AddScoped<BatchJobScheduler>();

        await using var provider = services.BuildServiceProvider();

        Assert.True(BatchCommand.IsBatchCommand(new[] { "batch", "run", "all" }));
        Assert.False(BatchCommand.IsBatchCommand(new[] { "--urls", "http://*:5000" }));

        Assert.Equal(0, await BatchCommand.RunAsync(provider, new[] { "batch", "run", "log_cleanup" }));
        Assert.Equal(0, await BatchCommand.RunAsync(provider, new[] { "batch", "run", "all" }));
        Assert.Equal(2, job.RunCount);

        Assert.Equal(2, await BatchCommand.RunAsync(provider, new[] { "batch", "run", "unknown_job" }));
        Assert.Equal(2, await BatchCommand.RunAsync(provider, new[] { "batch" }));
    }
}
