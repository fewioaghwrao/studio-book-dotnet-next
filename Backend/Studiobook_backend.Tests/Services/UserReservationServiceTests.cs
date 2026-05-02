using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Services;

public class UserReservationServiceTests
{
    [Fact]
    public async Task GetMyReservationsAsync_ReturnsOnlyLoginUserReservations()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedGuestUserAsync(context, userId: 10, name: "ログインユーザー");
        await SeedGuestUserAsync(context, userId: 99, name: "別ユーザー");
        await SeedHostUserAsync(context, userId: 20, name: "ホストA");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 20, name: "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            userId: 10,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "paid",
            amount: 6000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            userId: 99,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "paid",
            amount: 7000);

        var service = new UserReservationService(context);

        // Act
        var result = await service.GetMyReservationsAsync(
            userId: 10,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);

        Assert.Equal(1, result.Items[0].ReservationId);
        Assert.Equal(1, result.Items[0].RoomId);
        Assert.Equal("新宿スタジオ", result.Items[0].RoomName);
        Assert.Equal("room01.jpg", result.Items[0].RoomImageName);
        Assert.Equal("東京都テスト区サンプル1", result.Items[0].RoomAddress);
        Assert.Equal(6000, result.Items[0].Amount);
        Assert.Equal("paid", result.Items[0].Status);

        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetMyReservationsAsync_OrdersByStartAtDescThenIdDesc()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedGuestUserAsync(context, userId: 10, name: "ログインユーザー");
        await SeedHostUserAsync(context, userId: 20, name: "ホストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 20, name: "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            userId: 10,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "paid",
            amount: 1000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            userId: 10,
            startAt: new DateTime(2026, 5, 3, 10, 0, 0),
            endAt: new DateTime(2026, 5, 3, 12, 0, 0),
            status: "paid",
            amount: 2000);

        await SeedReservationAsync(
            context,
            reservationId: 3,
            roomId: 1,
            userId: 10,
            startAt: new DateTime(2026, 5, 3, 10, 0, 0),
            endAt: new DateTime(2026, 5, 3, 13, 0, 0),
            status: "paid",
            amount: 3000);

        var service = new UserReservationService(context);

        // Act
        var result = await service.GetMyReservationsAsync(
            userId: 10,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Equal(3, result.Items.Count);

        Assert.Equal(3, result.Items[0].ReservationId);
        Assert.Equal(2, result.Items[1].ReservationId);
        Assert.Equal(1, result.Items[2].ReservationId);
    }

    [Fact]
    public async Task GetMyReservationsAsync_AppliesPaging()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedGuestUserAsync(context, userId: 10, name: "ログインユーザー");
        await SeedHostUserAsync(context, userId: 20, name: "ホストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 20, name: "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            userId: 10,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "paid",
            amount: 1000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            userId: 10,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "paid",
            amount: 2000);

        await SeedReservationAsync(
            context,
            reservationId: 3,
            roomId: 1,
            userId: 10,
            startAt: new DateTime(2026, 5, 3, 10, 0, 0),
            endAt: new DateTime(2026, 5, 3, 12, 0, 0),
            status: "paid",
            amount: 3000);

        var service = new UserReservationService(context);

        // Act
        var result = await service.GetMyReservationsAsync(
            userId: 10,
            page: 2,
            pageSize: 2);

        // Assert
        Assert.Single(result.Items);

        // StartAt desc:
        // page1: ID 3, 2
        // page2: ID 1
        Assert.Equal(1, result.Items[0].ReservationId);

        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetMyReservationsAsync_NormalizesInvalidPageAndPageSize()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedGuestUserAsync(context, userId: 10, name: "ログインユーザー");
        await SeedHostUserAsync(context, userId: 20, name: "ホストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 20, name: "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            userId: 10,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "paid",
            amount: 6000);

        var service = new UserReservationService(context);

        // Act
        var result = await service.GetMyReservationsAsync(
            userId: 10,
            page: 0,
            pageSize: 0);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetMyReservationsAsync_ClampsPageSizeTo50()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedGuestUserAsync(context, userId: 10, name: "ログインユーザー");
        await SeedHostUserAsync(context, userId: 20, name: "ホストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 20, name: "新宿スタジオ");

        for (var i = 1; i <= 60; i++)
        {
            await SeedReservationAsync(
                context,
                reservationId: i,
                roomId: 1,
                userId: 10,
                startAt: new DateTime(2026, 5, 1, 10, 0, 0).AddDays(i),
                endAt: new DateTime(2026, 5, 1, 12, 0, 0).AddDays(i),
                status: "paid",
                amount: 6000 + i);
        }

        var service = new UserReservationService(context);

        // Act
        var result = await service.GetMyReservationsAsync(
            userId: 10,
            page: 1,
            pageSize: 100);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.Equal(50, result.Items.Count);
        Assert.Equal(60, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetMyReservationsAsync_ReturnsEmptyItems_WhenNoReservations()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedGuestUserAsync(context, userId: 10, name: "ログインユーザー");

        var service = new UserReservationService(context);

        // Act
        var result = await service.GetMyReservationsAsync(
            userId: 10,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    private static async Task SeedGuestUserAsync(
        AppDbContext context,
        int userId,
        string name)
    {
        context.Users.Add(new User
        {
            Id = userId,
            Name = name,
            Kana = "テストゲスト",
            Email = $"guest{userId}@example.com",
            PasswordHash = "dummy_hash",
            PostalCode = "100-0001",
            Address = "東京都千代田区テスト",
            PhoneNumber = "090-0000-0000",
            UsageType = "general",
            Enabled = true
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedHostUserAsync(
        AppDbContext context,
        int userId,
        string name)
    {
        context.Users.Add(new User
        {
            Id = userId,
            Name = name,
            Kana = "テストホスト",
            Email = $"host{userId}@example.com",
            PasswordHash = "dummy_hash",
            PostalCode = "100-0001",
            Address = "東京都千代田区テスト",
            PhoneNumber = "090-0000-0000",
            UsageType = "host",
            Enabled = true
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedRoomAsync(
        AppDbContext context,
        int roomId,
        int hostUserId,
        string name)
    {
        context.Rooms.Add(new Room
        {
            Id = roomId,
            UserId = hostUserId,
            Name = name,
            ImageName = $"room{roomId:00}.jpg",
            Description = "テスト用スタジオ",
            Price = 3000,
            Capacity = 5,
            PostalCode = "100-0001",
            Address = $"東京都テスト区サンプル{roomId}",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedReservationAsync(
        AppDbContext context,
        int reservationId,
        int roomId,
        int userId,
        DateTime startAt,
        DateTime endAt,
        string status,
        int amount)
    {
        context.Reservations.Add(new Reservation
        {
            Id = reservationId,
            RoomId = roomId,
            UserId = userId,
            StartAt = startAt,
            EndAt = endAt,
            Status = status,
            Amount = amount,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }
}