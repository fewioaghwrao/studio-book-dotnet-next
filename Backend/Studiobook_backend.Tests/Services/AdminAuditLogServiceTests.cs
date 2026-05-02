using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Services;

public class AdminAuditLogServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetListAsync_条件なしの場合_日時降順で一覧を返す()
    {
        // Arrange
        await using var context = CreateDbContext();

        context.AuditLogs.AddRange(
            new AuditLog
            {
                Id = 1,
                Ts = new DateTime(2026, 5, 1, 10, 0, 0),
                ActorId = 1,
                Action = "LOGIN",
                Entity = "User",
                EntityId = 1,
                Note = "ログインしました"
            },
            new AuditLog
            {
                Id = 2,
                Ts = new DateTime(2026, 5, 2, 10, 0, 0),
                ActorId = 2,
                Action = "UPDATE",
                Entity = "Room",
                EntityId = 10,
                Note = "部屋情報を更新しました"
            }
        );

        await context.SaveChangesAsync();

        var service = new AdminAuditLogService(context);

        // Act
        var result = await service.GetListAsync(
            q: null,
            actorId: null,
            action: null,
            entity: null,
            entityId: null,
            from: null,
            to: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(2, result.Items.Count);

        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal(1, result.Items[1].Id);
    }

    [Fact]
    public async Task GetListAsync_q指定の場合_ActionEntityNoteに部分一致するログを返す()
    {
        // Arrange
        await using var context = CreateDbContext();

        context.AuditLogs.AddRange(
            new AuditLog
            {
                Id = 1,
                Ts = new DateTime(2026, 5, 1),
                ActorId = 1,
                Action = "LOGIN",
                Entity = "User",
                EntityId = 1,
                Note = "ログインしました"
            },
            new AuditLog
            {
                Id = 2,
                Ts = new DateTime(2026, 5, 2),
                ActorId = 2,
                Action = "UPDATE",
                Entity = "Room",
                EntityId = 10,
                Note = "部屋情報を更新しました"
            }
        );

        await context.SaveChangesAsync();

        var service = new AdminAuditLogService(context);

        // Act
        var result = await service.GetListAsync(
            q: "部屋",
            actorId: null,
            action: null,
            entity: null,
            entityId: null,
            from: null,
            to: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal("UPDATE", result.Items[0].Action);
        Assert.Equal("Room", result.Items[0].Entity);
    }

    [Fact]
    public async Task GetListAsync_actorId指定の場合_操作ユーザーで絞り込む()
    {
        // Arrange
        await using var context = CreateDbContext();

        context.AuditLogs.AddRange(
            new AuditLog
            {
                Id = 1,
                Ts = new DateTime(2026, 5, 1),
                ActorId = 1,
                Action = "LOGIN",
                Entity = "User",
                EntityId = 1,
                Note = "user1"
            },
            new AuditLog
            {
                Id = 2,
                Ts = new DateTime(2026, 5, 2),
                ActorId = 2,
                Action = "LOGIN",
                Entity = "User",
                EntityId = 2,
                Note = "user2"
            }
        );

        await context.SaveChangesAsync();

        var service = new AdminAuditLogService(context);

        // Act
        var result = await service.GetListAsync(
            q: null,
            actorId: 2,
            action: null,
            entity: null,
            entityId: null,
            from: null,
            to: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal(2, result.Items[0].ActorId);
    }

    [Fact]
    public async Task GetListAsync_action指定の場合_Actionで部分一致検索する()
    {
        // Arrange
        await using var context = CreateDbContext();

        context.AuditLogs.AddRange(
            new AuditLog
            {
                Id = 1,
                Ts = new DateTime(2026, 5, 1),
                ActorId = 1,
                Action = "LOGIN",
                Entity = "User",
                EntityId = 1,
                Note = "ログイン"
            },
            new AuditLog
            {
                Id = 2,
                Ts = new DateTime(2026, 5, 2),
                ActorId = 1,
                Action = "UPDATE_ROOM",
                Entity = "Room",
                EntityId = 10,
                Note = "更新"
            }
        );

        await context.SaveChangesAsync();

        var service = new AdminAuditLogService(context);

        // Act
        var result = await service.GetListAsync(
            q: null,
            actorId: null,
            action: "UPDATE",
            entity: null,
            entityId: null,
            from: null,
            to: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal("UPDATE_ROOM", result.Items[0].Action);
    }

    [Fact]
    public async Task GetListAsync_fromとto指定の場合_指定日範囲のログのみ返す()
    {
        // Arrange
        await using var context = CreateDbContext();

        context.AuditLogs.AddRange(
            new AuditLog
            {
                Id = 1,
                Ts = new DateTime(2026, 5, 1, 23, 59, 59),
                ActorId = 1,
                Action = "BEFORE",
                Entity = "User",
                EntityId = 1,
                Note = "before"
            },
            new AuditLog
            {
                Id = 2,
                Ts = new DateTime(2026, 5, 2, 12, 0, 0),
                ActorId = 1,
                Action = "TARGET",
                Entity = "User",
                EntityId = 1,
                Note = "target"
            },
            new AuditLog
            {
                Id = 3,
                Ts = new DateTime(2026, 5, 3, 0, 0, 0),
                ActorId = 1,
                Action = "AFTER",
                Entity = "User",
                EntityId = 1,
                Note = "after"
            }
        );

        await context.SaveChangesAsync();

        var service = new AdminAuditLogService(context);

        // Act
        var result = await service.GetListAsync(
            q: null,
            actorId: null,
            action: null,
            entity: null,
            entityId: null,
            from: new DateTime(2026, 5, 2),
            to: new DateTime(2026, 5, 2),
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal("TARGET", result.Items[0].Action);
    }

    [Fact]
    public async Task GetListAsync_pageSizeが100を超える場合_100に丸める()
    {
        // Arrange
        await using var context = CreateDbContext();

        for (var i = 1; i <= 120; i++)
        {
            context.AuditLogs.Add(new AuditLog
            {
                Id = i,
                Ts = new DateTime(2026, 5, 1).AddMinutes(i),
                ActorId = i % 3 + 1,
                Action = "LOGIN",
                Entity = "User",
                EntityId = i,
                Note = $"log-{i}"
            });
        }

        await context.SaveChangesAsync();

        var service = new AdminAuditLogService(context);

        // Act
        var result = await service.GetListAsync(
            q: null,
            actorId: null,
            action: null,
            entity: null,
            entityId: null,
            from: null,
            to: null,
            page: 1,
            pageSize: 999);

        // Assert
        Assert.Equal(120, result.TotalCount);
        Assert.Equal(100, result.PageSize);
        Assert.Equal(100, result.Items.Count);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetListAsync_pageとpageSizeが0以下の場合_既定値に丸める()
    {
        // Arrange
        await using var context = CreateDbContext();

        context.AuditLogs.Add(new AuditLog
        {
            Id = 1,
            Ts = new DateTime(2026, 5, 1),
            ActorId = 1,
            Action = "LOGIN",
            Entity = "User",
            EntityId = 1,
            Note = "ログイン"
        });

        await context.SaveChangesAsync();

        var service = new AdminAuditLogService(context);

        // Act
        var result = await service.GetListAsync(
            q: null,
            actorId: null,
            action: null,
            entity: null,
            entityId: null,
            from: null,
            to: null,
            page: 0,
            pageSize: 0);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Single(result.Items);
    }
}
