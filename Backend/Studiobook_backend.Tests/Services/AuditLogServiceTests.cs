using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Studiobook_backend.Data;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Services;

public class AuditLogServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task WriteAsync_正常時_AuditLogを保存する()
    {
        // Arrange
        await using var context = CreateDbContext();

        var service = new AuditLogService(
            context,
            NullLogger<AuditLogService>.Instance);

        // Act
        await service.WriteAsync(
            actorId: 1,
            action: "CREATE",
            entity: "Room",
            entityId: 10,
            note: "スタジオを登録しました。");

        // Assert
        var log = await context.AuditLogs.SingleAsync();

        Assert.Equal(1, log.ActorId);
        Assert.Equal("CREATE", log.Action);
        Assert.Equal("Room", log.Entity);
        Assert.Equal(10, log.EntityId);
        Assert.Equal("スタジオを登録しました。", log.Note);
        Assert.True(log.Ts <= DateTime.UtcNow);
    }

    [Fact]
    public async Task WriteAsync_actorIdとentityIdがnullでも保存する()
    {
        // Arrange
        await using var context = CreateDbContext();

        var service = new AuditLogService(
            context,
            NullLogger<AuditLogService>.Instance);

        // Act
        await service.WriteAsync(
            actorId: null,
            action: "SETTING_UPDATE",
            entity: "AppSetting",
            entityId: null,
            note: "管理設定を更新しました。");

        // Assert
        var log = await context.AuditLogs.SingleAsync();

        Assert.Null(log.ActorId);
        Assert.Equal("SETTING_UPDATE", log.Action);
        Assert.Equal("AppSetting", log.Entity);
        Assert.Null(log.EntityId);
        Assert.Equal("管理設定を更新しました。", log.Note);
    }

    [Fact]
    public async Task WriteAsync_複数回呼び出した場合_複数件保存する()
    {
        // Arrange
        await using var context = CreateDbContext();

        var service = new AuditLogService(
            context,
            NullLogger<AuditLogService>.Instance);

        // Act
        await service.WriteAsync(
            actorId: 1,
            action: "LOGIN",
            entity: "User",
            entityId: 1,
            note: "ログインしました。");

        await service.WriteAsync(
            actorId: 1,
            action: "UPDATE",
            entity: "Room",
            entityId: 2,
            note: "スタジオを更新しました。");

        // Assert
        var logs = await context.AuditLogs
            .OrderBy(x => x.Id)
            .ToListAsync();

        Assert.Equal(2, logs.Count);

        Assert.Equal("LOGIN", logs[0].Action);
        Assert.Equal("User", logs[0].Entity);

        Assert.Equal("UPDATE", logs[1].Action);
        Assert.Equal("Room", logs[1].Entity);
    }
}