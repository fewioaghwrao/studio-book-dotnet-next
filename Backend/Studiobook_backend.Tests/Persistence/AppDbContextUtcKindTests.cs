using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Domain.Entities;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Persistence;

/// <summary>
/// DB から読んだ記録用日時（UTC 保存）が Kind = Utc になり、
/// JSON で "Z" 付きとして返る（＝ブラウザで 9 時間ずれない）ことを確認する。
/// </summary>
public class AppDbContextUtcKindTests
{
    [Fact]
    public async Task Utc列は読み込み時にKindがUtcになる()
    {
        await using var context = TestDbContextFactory.Create();

        context.AuditLogs.Add(new AuditLog
        {
            Id = 1,
            Ts = new DateTime(2026, 5, 1, 10, 0, 0),
            ActorId = 1,
            Action = "LOGIN",
            Entity = "User",
            EntityId = 1,
            Note = "ログインしました"
        });

        context.AiSearchLogs.Add(new AiSearchLog
        {
            Id = 1,
            CreatedAtUtc = new DateTime(2026, 5, 1, 10, 0, 0),
            Query = "q",
            Succeeded = true,
            ResultCount = 1
        });

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var log = await context.AuditLogs.SingleAsync();
        var aiLog = await context.AiSearchLogs.SingleAsync();

        Assert.Equal(DateTimeKind.Utc, log.Ts.Kind);
        Assert.Equal(new DateTime(2026, 5, 1, 10, 0, 0), log.Ts);
        Assert.Equal(DateTimeKind.Utc, aiLog.CreatedAtUtc.Kind);
    }

    [Fact]
    public async Task 休業日の開始終了は日本時間のままで作成日時だけUtcになる()
    {
        await using var context = TestDbContextFactory.Create();

        context.Closures.Add(new Closure
        {
            Id = 1,
            RoomId = 1,
            StartAt = new DateTime(2026, 5, 1, 9, 0, 0),
            EndAt = new DateTime(2026, 5, 1, 18, 0, 0),
            CreatedAt = new DateTime(2026, 4, 30, 1, 0, 0)
        });

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var closure = await context.Closures.SingleAsync();

        Assert.Equal(DateTimeKind.Unspecified, closure.StartAt.Kind);
        Assert.Equal(DateTimeKind.Unspecified, closure.EndAt.Kind);
        Assert.Equal(DateTimeKind.Utc, closure.CreatedAt.Kind);
    }

    [Fact]
    public void 予約の開始終了には変換を設定しない()
    {
        using var context = TestDbContextFactory.Create();

        var entity = context.Model.FindEntityType(typeof(Reservation))!;

        Assert.Null(entity.FindProperty(nameof(Reservation.StartAt))!.GetValueConverter());
        Assert.Null(entity.FindProperty(nameof(Reservation.EndAt))!.GetValueConverter());
        Assert.NotNull(entity.FindProperty(nameof(Reservation.CreatedAtUtc))!.GetValueConverter());
    }
}
