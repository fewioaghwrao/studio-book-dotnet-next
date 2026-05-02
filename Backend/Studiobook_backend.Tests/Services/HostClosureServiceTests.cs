using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Services;

public class HostClosureServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task SeedAsync(AppDbContext context)
    {
        var host = new User
        {
            Id = 1,
            Name = "ホスト太郎",
            Kana = "ホストタロウ",
            Email = "host@example.com",
            PasswordHash = "hash",
            PostalCode = "100-0001",
            Address = "東京都千代田区",
            PhoneNumber = "090-0000-0001",
            UsageType = "Host",
            Enabled = true
        };

        var otherHost = new User
        {
            Id = 2,
            Name = "別ホスト",
            Kana = "ベツホスト",
            Email = "other@example.com",
            PasswordHash = "hash",
            PostalCode = "100-0002",
            Address = "東京都中央区",
            PhoneNumber = "090-0000-0002",
            UsageType = "Host",
            Enabled = true
        };

        context.Users.AddRange(host, otherHost);

        context.Rooms.AddRange(
            new Room
            {
                Id = 1,
                UserId = host.Id,
                User = host,
                Name = "A Studio",
                ImageName = "room01.jpg",
                Description = "テスト用",
                Price = 3000,
                Capacity = 5,
                PostalCode = "100-0001",
                Address = "東京都渋谷区",
                CreatedAtUtc = new DateTime(2026, 5, 1),
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            },
            new Room
            {
                Id = 2,
                UserId = otherHost.Id,
                User = otherHost,
                Name = "Other Studio",
                ImageName = "room02.jpg",
                Description = "他人のスタジオ",
                Price = 4000,
                Capacity = 8,
                PostalCode = "100-0002",
                Address = "東京都新宿区",
                CreatedAtUtc = new DateTime(2026, 5, 1),
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            }
        );

        context.Closures.AddRange(
            new Closure
            {
                Id = 1,
                RoomId = 1,
                StartAt = new DateTime(2026, 5, 10, 0, 0, 0),
                EndAt = new DateTime(2026, 5, 11, 0, 0, 0),
                Reason = "設備点検"
            },
            new Closure
            {
                Id = 2,
                RoomId = 1,
                StartAt = new DateTime(2026, 5, 5, 13, 0, 0),
                EndAt = new DateTime(2026, 5, 5, 18, 0, 0),
                Reason = null
            }
        );

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task ListAsync_自分のRoomの場合_StartAt昇順で休館日一覧を返す()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new HostClosureService(context);

        // Act
        var result = await service.ListAsync(roomId: 1, userId: 1);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(2, result[0].Id);
        Assert.Equal(new DateTime(2026, 5, 5, 13, 0, 0), result[0].StartAt);

        Assert.Equal(1, result[1].Id);
        Assert.Equal(new DateTime(2026, 5, 10, 0, 0, 0), result[1].StartAt);
    }

    [Fact]
    public async Task ListAsync_他人のRoomの場合_UnauthorizedAccessExceptionを投げる()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new HostClosureService(context);

        // Act
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ListAsync(roomId: 2, userId: 1));

        // Assert
        Assert.Equal("対象のスタジオが見つからないか、操作権限がありません。", ex.Message);
    }

    [Fact]
    public async Task EventsAsync_休館イベント形式で返す()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new HostClosureService(context);

        // Act
        var result = await service.EventsAsync(roomId: 1, userId: 1);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(2, result[0].Id);
        Assert.Equal("休館", result[0].Title);
        Assert.False(result[0].AllDay);

        Assert.Equal(1, result[1].Id);
        Assert.Equal("休館: 設備点検", result[1].Title);
        Assert.True(result[1].AllDay);
    }

    [Fact]
    public async Task CreateAsync_正常な期間の場合_休館日を作成する()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new HostClosureService(context);

        var request = new CreateClosureRequest
        {
            StartAt = new DateTime(2026, 6, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 6, 1, 18, 0, 0),
            Reason = "臨時休業"
        };

        // Act
        var result = await service.CreateAsync(
            roomId: 1,
            userId: 1,
            request: request);

        // Assert
        Assert.NotEqual(0, result.Id);
        Assert.Equal(1, result.RoomId);
        Assert.Equal(new DateTime(2026, 6, 1, 10, 0, 0), result.StartAt);
        Assert.Equal(new DateTime(2026, 6, 1, 18, 0, 0), result.EndAt);
        Assert.Equal("臨時休業", result.Reason);

        var created = await context.Closures
            .FirstOrDefaultAsync(x => x.Id == result.Id);

        Assert.NotNull(created);
    }

    [Fact]
    public async Task CreateAsync_EndAtがStartAt以下の場合_InvalidOperationExceptionを投げる()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new HostClosureService(context);

        var request = new CreateClosureRequest
        {
            StartAt = new DateTime(2026, 6, 1, 18, 0, 0),
            EndAt = new DateTime(2026, 6, 1, 18, 0, 0),
            Reason = "不正期間"
        };

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(
                roomId: 1,
                userId: 1,
                request: request));

        // Assert
        Assert.Equal("終了日時は開始日時より後にしてください。", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_存在する休館日の場合_削除する()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new HostClosureService(context);

        // Act
        await service.DeleteAsync(
            roomId: 1,
            closureId: 1,
            userId: 1);

        // Assert
        var exists = await context.Closures
            .AnyAsync(x => x.Id == 1);

        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteAsync_存在しない休館日の場合_KeyNotFoundExceptionを投げる()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new HostClosureService(context);

        // Act
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.DeleteAsync(
                roomId: 1,
                closureId: 999,
                userId: 1));

        // Assert
        Assert.Equal("休館日が見つかりません。", ex.Message);
    }
}