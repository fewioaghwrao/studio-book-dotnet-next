using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Services;

public class AdminAiSearchLogServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetListAsync_条件なしの場合_作成日時の降順で一覧を返す()
    {
        // Arrange
        await using var context = CreateDbContext();

        context.AiSearchLogs.AddRange(
            new AiSearchLog
            {
                Id = 1,
                CreatedAtUtc = new DateTime(2026, 5, 1, 10, 0, 0),
                Query = "quiet studio",
                IpAddress = "127.0.0.1",
                UserId = 1,
                Model = "gpt-test",
                Succeeded = true,
                ResultCount = 3
            },
            new AiSearchLog
            {
                Id = 2,
                CreatedAtUtc = new DateTime(2026, 5, 2, 10, 0, 0),
                Query = "night studio",
                IpAddress = "127.0.0.2",
                UserId = 2,
                Model = "gpt-test",
                Succeeded = false,
                ResultCount = 0,
                ErrorMessage = "error"
            }
        );

        await context.SaveChangesAsync();

        var service = new AdminAiSearchLogService(context);

        // Act
        var result = await service.GetListAsync(
            q: null,
            userId: null,
            ipAddress: null,
            succeeded: null,
            model: null,
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
    public async Task GetListAsync_q指定の場合_QueryまたはErrorMessageに部分一致するログを返す()
    {
        // Arrange
        await using var context = CreateDbContext();

        context.AiSearchLogs.AddRange(
            new AiSearchLog
            {
                Id = 1,
                CreatedAtUtc = new DateTime(2026, 5, 1),
                Query = "落ち着いた雰囲気のスタジオ",
                IpAddress = "127.0.0.1",
                UserId = 1,
                Model = "gpt-test",
                Succeeded = true,
                ResultCount = 5
            },
            new AiSearchLog
            {
                Id = 2,
                CreatedAtUtc = new DateTime(2026, 5, 2),
                Query = "夜に使いやすい場所",
                IpAddress = "127.0.0.2",
                UserId = 2,
                Model = "gpt-test",
                Succeeded = false,
                ResultCount = 0,
                ErrorMessage = "AI検索エラー"
            }
        );

        await context.SaveChangesAsync();

        var service = new AdminAiSearchLogService(context);

        // Act
        var result = await service.GetListAsync(
            q: "エラー",
            userId: null,
            ipAddress: null,
            succeeded: null,
            model: null,
            from: null,
            to: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal("AI検索エラー", result.Items[0].ErrorMessage);
    }

    [Fact]
    public async Task GetListAsync_succeeded指定の場合_成功状態で絞り込む()
    {
        // Arrange
        await using var context = CreateDbContext();

        context.AiSearchLogs.AddRange(
            new AiSearchLog
            {
                Id = 1,
                CreatedAtUtc = new DateTime(2026, 5, 1),
                Query = "success query",
                Succeeded = true,
                ResultCount = 2
            },
            new AiSearchLog
            {
                Id = 2,
                CreatedAtUtc = new DateTime(2026, 5, 2),
                Query = "failed query",
                Succeeded = false,
                ResultCount = 0,
                ErrorMessage = "failed"
            }
        );

        await context.SaveChangesAsync();

        var service = new AdminAiSearchLogService(context);

        // Act
        var result = await service.GetListAsync(
            q: null,
            userId: null,
            ipAddress: null,
            succeeded: false,
            model: null,
            from: null,
            to: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
        Assert.False(result.Items[0].Succeeded);
    }

    [Fact]
    public async Task GetListAsync_pageSizeが50を超える場合_50に丸める()
    {
        // Arrange
        await using var context = CreateDbContext();

        for (var i = 1; i <= 60; i++)
        {
            context.AiSearchLogs.Add(new AiSearchLog
            {
                Id = i,
                CreatedAtUtc = new DateTime(2026, 5, 1).AddMinutes(i),
                Query = $"query-{i}",
                Succeeded = true,
                ResultCount = i
            });
        }

        await context.SaveChangesAsync();

        var service = new AdminAiSearchLogService(context);

        // Act
        var result = await service.GetListAsync(
            q: null,
            userId: null,
            ipAddress: null,
            succeeded: null,
            model: null,
            from: null,
            to: null,
            page: 1,
            pageSize: 999);

        // Assert
        Assert.Equal(60, result.TotalCount);
        Assert.Equal(50, result.PageSize);
        Assert.Equal(50, result.Items.Count);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetListAsync_fromとto指定の場合_指定日の範囲内のみ返す()
    {
        // Arrange
        await using var context = CreateDbContext();

        context.AiSearchLogs.AddRange(
            new AiSearchLog
            {
                Id = 1,
                CreatedAtUtc = new DateTime(2026, 5, 1, 23, 59, 59),
                Query = "before",
                Succeeded = true,
                ResultCount = 1
            },
            new AiSearchLog
            {
                Id = 2,
                CreatedAtUtc = new DateTime(2026, 5, 2, 12, 0, 0),
                Query = "target",
                Succeeded = true,
                ResultCount = 1
            },
            new AiSearchLog
            {
                Id = 3,
                CreatedAtUtc = new DateTime(2026, 5, 3, 0, 0, 0),
                Query = "after",
                Succeeded = true,
                ResultCount = 1
            }
        );

        await context.SaveChangesAsync();

        var service = new AdminAiSearchLogService(context);

        // Act
        var result = await service.GetListAsync(
            q: null,
            userId: null,
            ipAddress: null,
            succeeded: null,
            model: null,
            from: new DateTime(2026, 5, 2),
            to: new DateTime(2026, 5, 2),
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
    }
}