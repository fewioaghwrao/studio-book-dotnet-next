using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Services;

public class HostReservationServiceTests
{
    [Fact]
    public async Task GetListAsync_ReturnsOnlyReservationsForHostRooms()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedHostUserAsync(context, userId: 99, name: "ホストB");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "ホストAのスタジオ");
        await SeedRoomAsync(context, roomId: 2, hostUserId: 99, name: "ホストBのスタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "booked",
            amount: 6000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 2,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "booked",
            amount: 6000);

        var service = new HostReservationService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            keyword: null,
            status: null,
            reservationId: null,
            roomId: null,
            startFrom: null,
            startTo: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].ReservationId);
        Assert.Equal("ホストAのスタジオ", result.Items[0].RoomName);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);

        Assert.Single(result.RoomOptions);
        Assert.Equal(1, result.RoomOptions[0].Id);
        Assert.Equal("ホストAのスタジオ", result.RoomOptions[0].Name);
    }

    [Fact]
    public async Task GetListAsync_FiltersByKeyword_WhenKeywordMatchesRoomName()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "山田太郎");

        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");
        await SeedRoomAsync(context, 2, 10, "渋谷スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "booked",
            amount: 6000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 2,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "booked",
            amount: 6000);

        var service = new HostReservationService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            keyword: "新宿",
            status: null,
            reservationId: null,
            roomId: null,
            startFrom: null,
            startTo: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].ReservationId);
        Assert.Equal("新宿スタジオ", result.Items[0].RoomName);
    }

    [Fact]
    public async Task GetListAsync_FiltersByKeyword_WhenKeywordMatchesGuestName()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "山田太郎");
        await SeedGuestUserAsync(context, 21, "佐藤花子");

        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "booked",
            amount: 6000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            guestUserId: 21,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "booked",
            amount: 6000);

        var service = new HostReservationService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            keyword: "佐藤",
            status: null,
            reservationId: null,
            roomId: null,
            startFrom: null,
            startTo: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].ReservationId);
        Assert.Equal("佐藤花子", result.Items[0].GuestName);
    }

    [Fact]
    public async Task GetListAsync_FiltersByStatus()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "booked",
            amount: 6000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "paid",
            amount: 6000);

        var service = new HostReservationService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            keyword: null,
            status: "paid",
            reservationId: null,
            roomId: null,
            startFrom: null,
            startTo: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].ReservationId);
        Assert.Equal("paid", result.Items[0].Status);
    }

    [Fact]
    public async Task GetListAsync_FiltersByReservationId()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "booked",
            amount: 6000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "paid",
            amount: 6000);

        var service = new HostReservationService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            keyword: null,
            status: null,
            reservationId: 2,
            roomId: null,
            startFrom: null,
            startTo: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].ReservationId);
    }

    [Fact]
    public async Task GetListAsync_FiltersByRoomId()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");

        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");
        await SeedRoomAsync(context, 2, 10, "渋谷スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "booked",
            amount: 6000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 2,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "booked",
            amount: 6000);

        var service = new HostReservationService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            keyword: null,
            status: null,
            reservationId: null,
            roomId: 2,
            startFrom: null,
            startTo: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].RoomId);
        Assert.Equal("渋谷スタジオ", result.Items[0].RoomName);
    }

    [Fact]
    public async Task GetListAsync_FiltersByStartDateRange()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "booked",
            amount: 6000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 10, 10, 0, 0),
            endAt: new DateTime(2026, 5, 10, 12, 0, 0),
            status: "booked",
            amount: 6000);

        var service = new HostReservationService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            keyword: null,
            status: null,
            reservationId: null,
            roomId: null,
            startFrom: new DateTime(2026, 5, 10),
            startTo: new DateTime(2026, 5, 10),
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].ReservationId);
        Assert.Equal(new DateTime(2026, 5, 10, 10, 0, 0), result.Items[0].StartAt);
    }

    [Fact]
    public async Task GetListAsync_AppliesPaging()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "booked",
            amount: 6000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "booked",
            amount: 6000);

        await SeedReservationAsync(
            context,
            reservationId: 3,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 3, 10, 0, 0),
            endAt: new DateTime(2026, 5, 3, 12, 0, 0),
            status: "booked",
            amount: 6000);

        var service = new HostReservationService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            keyword: null,
            status: null,
            reservationId: null,
            roomId: null,
            startFrom: null,
            startTo: null,
            page: 2,
            pageSize: 2);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].ReservationId);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task GetListAsync_NormalizesInvalidPageAndPageSize()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "booked",
            amount: 6000);

        var service = new HostReservationService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            keyword: null,
            status: null,
            reservationId: null,
            roomId: null,
            startFrom: null,
            startTo: null,
            page: 0,
            pageSize: 0);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task ApproveAsync_ChangesStatusToPaid_WhenReservationIsBooked()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "booked",
            amount: 6000);

        var service = new HostReservationService(context);

        // Act
        await service.ApproveAsync(hostUserId: 10, reservationId: 1);

        // Assert
        var reservation = await context.Reservations.SingleAsync(x => x.Id == 1);
        Assert.Equal("paid", reservation.Status);
    }

    [Fact]
    public async Task ApproveAsync_DoesNotChangeStatus_WhenReservationIsNotBooked()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "canceled",
            amount: 6000);

        var service = new HostReservationService(context);

        // Act
        await service.ApproveAsync(hostUserId: 10, reservationId: 1);

        // Assert
        var reservation = await context.Reservations.SingleAsync(x => x.Id == 1);
        Assert.Equal("canceled", reservation.Status);
    }

    [Fact]
    public async Task ApproveAsync_ThrowsKeyNotFoundException_WhenReservationDoesNotBelongToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedHostUserAsync(context, 99, "ホストB");
        await SeedGuestUserAsync(context, 20, "ゲストA");

        await SeedRoomAsync(context, 1, 99, "他人のスタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "booked",
            amount: 6000);

        var service = new HostReservationService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ApproveAsync(hostUserId: 10, reservationId: 1));

        var reservation = await context.Reservations.SingleAsync(x => x.Id == 1);
        Assert.Equal("booked", reservation.Status);
    }

    [Fact]
    public async Task CancelAsync_ChangesStatusToCanceled_WhenReservationIsBooked()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "booked",
            amount: 6000);

        var service = new HostReservationService(context);

        // Act
        await service.CancelAsync(hostUserId: 10, reservationId: 1);

        // Assert
        var reservation = await context.Reservations.SingleAsync(x => x.Id == 1);
        Assert.Equal("canceled", reservation.Status);
    }

    [Fact]
    public async Task CancelAsync_DoesNotChangeStatus_WhenReservationIsNotBooked()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "paid",
            amount: 6000);

        var service = new HostReservationService(context);

        // Act
        await service.CancelAsync(hostUserId: 10, reservationId: 1);

        // Assert
        var reservation = await context.Reservations.SingleAsync(x => x.Id == 1);
        Assert.Equal("paid", reservation.Status);
    }

    [Fact]
    public async Task CancelAsync_ThrowsKeyNotFoundException_WhenReservationDoesNotBelongToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedHostUserAsync(context, 99, "ホストB");
        await SeedGuestUserAsync(context, 20, "ゲストA");

        await SeedRoomAsync(context, 1, 99, "他人のスタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "booked",
            amount: 6000);

        var service = new HostReservationService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CancelAsync(hostUserId: 10, reservationId: 1));

        var reservation = await context.Reservations.SingleAsync(x => x.Id == 1);
        Assert.Equal("booked", reservation.Status);
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
            Enabled = true,
        });

        await context.SaveChangesAsync();
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
            Enabled = true,
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
        int guestUserId,
        DateTime startAt,
        DateTime endAt,
        string status,
        int amount)
    {
        context.Reservations.Add(new Reservation
        {
            Id = reservationId,
            RoomId = roomId,
            UserId = guestUserId,
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