using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Studiobook_backend.Application.Batch;
using Studiobook_backend.Application.Settings;
using Studiobook_backend.Domain.Entities;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Batch;

public class LogCleanupJobTests
{
    // 現在：UTC 2026/11/12 0:00
    private static readonly DateTime NowUtc = new(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RunAsync_保持期間を過ぎたAI検索ログと監査ログだけを小分けに削除する()
    {
        await using var context = TestDbContextFactory.Create();

        // AI 検索ログ：91 日前 × 3（削除）、89 日前 × 1（残す）
        for (var id = 1; id <= 3; id++)
        {
            context.AiSearchLogs.Add(AiLog(id, NowUtc.AddDays(-91)));
        }

        context.AiSearchLogs.Add(AiLog(4, NowUtc.AddDays(-89)));

        // 監査ログ：366 日前（削除）、364 日前（残す）
        context.AuditLogs.Add(Audit(1, NowUtc.AddDays(-366)));
        context.AuditLogs.Add(Audit(2, NowUtc.AddDays(-364)));

        await context.SaveChangesAsync();

        var job = new LogCleanupJob(
            context,
            Options.Create(new BatchSettings { DeleteChunkSize = 2 }),
            new FixedTimeProvider(new DateTimeOffset(NowUtc)));

        var result = await job.RunAsync(CancellationToken.None);

        Assert.Equal(4, result.AffectedRows);
        Assert.Equal("ai_search_logs=3,audit_logs=1", result.Summary);

        Assert.Equal(new[] { 4 }, await context.AiSearchLogs.Select(x => x.Id).ToListAsync());
        Assert.Equal(new[] { 2 }, await context.AuditLogs.Select(x => x.Id).ToListAsync());
    }

    [Fact]
    public async Task RunAsync_何度実行しても結果は同じ()
    {
        await using var context = TestDbContextFactory.Create();
        context.AiSearchLogs.Add(AiLog(1, NowUtc.AddDays(-100)));
        await context.SaveChangesAsync();

        var job = new LogCleanupJob(
            context,
            Options.Create(new BatchSettings()),
            new FixedTimeProvider(new DateTimeOffset(NowUtc)));

        Assert.Equal(1, (await job.RunAsync(CancellationToken.None)).AffectedRows);
        Assert.Equal(0, (await job.RunAsync(CancellationToken.None)).AffectedRows);
    }

    private static AiSearchLog AiLog(int id, DateTime createdAtUtc) => new()
    {
        Id = id,
        Query = "渋谷で撮影",
        Succeeded = true,
        ResultCount = 1,
        CreatedAtUtc = createdAtUtc
    };

    private static AuditLog Audit(int id, DateTime ts) => new()
    {
        Id = id,
        Ts = ts,
        ActorId = 1,
        Action = "LOGIN",
        Entity = "User",
        EntityId = 1,
        Note = "ログイン"
    };
}
