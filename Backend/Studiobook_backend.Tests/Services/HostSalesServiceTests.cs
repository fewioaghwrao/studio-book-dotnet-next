using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Services;

public class HostSalesServiceTests
{
    [Fact]
    public async Task GetListAsync_ReturnsOnlySalesForHostRooms()
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
            status: "paid",
            amount: 6000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 2,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "paid",
            amount: 6000);

        var service = new HostSalesService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            roomId: null,
            onlyWithItems: true,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].ReservationId);
        Assert.Equal("ホストAのスタジオ", result.Items[0].RoomName);
        Assert.Equal("ゲストA", result.Items[0].GuestName);
        Assert.True(result.Items[0].HasItems);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);

        Assert.Single(result.RoomOptions);
        Assert.Equal(1, result.RoomOptions[0].Id);
        Assert.Equal("ホストAのスタジオ", result.RoomOptions[0].Name);
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
            status: "paid",
            amount: 6000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 2,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "paid",
            amount: 7000);

        var service = new HostSalesService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            roomId: 2,
            onlyWithItems: true,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].ReservationId);
        Assert.Equal(2, result.Items[0].RoomId);
        Assert.Equal("渋谷スタジオ", result.Items[0].RoomName);
        Assert.Equal(7000, result.Items[0].Amount);
    }

    [Fact]
    public async Task GetListAsync_ExcludesZeroAmount_WhenOnlyWithItemsIsTrue()
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
            amount: 0);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "paid",
            amount: 6000);

        var service = new HostSalesService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            roomId: null,
            onlyWithItems: true,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].ReservationId);
        Assert.Equal(6000, result.Items[0].Amount);
        Assert.True(result.Items[0].HasItems);
    }

    [Fact]
    public async Task GetListAsync_IncludesZeroAmount_WhenOnlyWithItemsIsFalse()
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
            amount: 0);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "paid",
            amount: 6000);

        var service = new HostSalesService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            roomId: null,
            onlyWithItems: false,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Items[0].ReservationId); // StartAt desc
        Assert.Equal(1, result.Items[1].ReservationId);
        Assert.False(result.Items[1].HasItems);
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
            status: "paid",
            amount: 1000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "paid",
            amount: 2000);

        await SeedReservationAsync(
            context,
            reservationId: 3,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 3, 10, 0, 0),
            endAt: new DateTime(2026, 5, 3, 12, 0, 0),
            status: "paid",
            amount: 3000);

        var service = new HostSalesService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            roomId: null,
            onlyWithItems: true,
            page: 2,
            pageSize: 2);

        // Assert
        Assert.Single(result.Items);

        // OrderByDescending(StartAt)
        // 1ページ目: ID 3, 2
        // 2ページ目: ID 1
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
            status: "paid",
            amount: 6000);

        var service = new HostSalesService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            roomId: null,
            onlyWithItems: true,
            page: 0,
            pageSize: 0);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsDetailWithChargeItems_WhenReservationBelongsToHost()
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
            startAt: new DateTime(2026, 5, 1, 9, 0, 0),
            endAt: new DateTime(2026, 5, 1, 20, 0, 0),
            status: "paid",
            amount: 36300);

        await SeedChargeItemAsync(
            context,
            id: 1,
            reservationId: 1,
            kind: "base",
            description: "通常料金",
            sliceStart: new DateTime(2026, 5, 1, 9, 0, 0),
            sliceEnd: new DateTime(2026, 5, 1, 19, 0, 0),
            unitRatePerHour: 3000,
            sliceAmount: 30000);

        await SeedChargeItemAsync(
            context,
            id: 2,
            reservationId: 1,
            kind: "late",
            description: "夜間料金",
            sliceStart: new DateTime(2026, 5, 1, 19, 0, 0),
            sliceEnd: new DateTime(2026, 5, 1, 20, 0, 0),
            unitRatePerHour: 4500,
            sliceAmount: 4500);

        await SeedChargeItemAsync(
            context,
            id: 3,
            reservationId: 1,
            kind: "tax",
            description: "消費税",
            sliceStart: null,
            sliceEnd: null,
            unitRatePerHour: null,
            sliceAmount: 1800);

        var service = new HostSalesService(context);

        // Act
        var result = await service.GetDetailAsync(
            hostUserId: 10,
            reservationId: 1);

        // Assert
        Assert.Equal(1, result.ReservationId);
        Assert.Equal(1, result.RoomId);
        Assert.Equal("新宿スタジオ", result.RoomName);
        Assert.Equal("ゲストA", result.GuestName);
        Assert.Equal(36300, result.Amount);
        Assert.Equal("paid", result.Status);

        Assert.Equal(3, result.Items.Count);

        Assert.Equal("base", result.Items[0].Kind);
        Assert.Equal("通常料金", result.Items[0].Description);
        Assert.Equal(30000, result.Items[0].SliceAmount);

        Assert.Equal("late", result.Items[1].Kind);
        Assert.Equal("夜間料金", result.Items[1].Description);
        Assert.Equal(4500, result.Items[1].SliceAmount);

        // SliceStart が null の明細は最後
        Assert.Equal("tax", result.Items[2].Kind);
        Assert.Equal("消費税", result.Items[2].Description);
        Assert.Equal(1800, result.Items[2].SliceAmount);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsEmptyItems_WhenNoChargeItemsExist()
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
            startAt: new DateTime(2026, 5, 1, 9, 0, 0),
            endAt: new DateTime(2026, 5, 1, 20, 0, 0),
            status: "paid",
            amount: 10000);

        var service = new HostSalesService(context);

        // Act
        var result = await service.GetDetailAsync(
            hostUserId: 10,
            reservationId: 1);

        // Assert
        Assert.Equal(1, result.ReservationId);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetDetailAsync_ThrowsKeyNotFoundException_WhenReservationDoesNotBelongToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedHostUserAsync(context, 99, "ホストB");
        await SeedGuestUserAsync(context, 20, "ゲストA");

        await SeedRoomAsync(context, 1, 99, "他ホストのスタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 9, 0, 0),
            endAt: new DateTime(2026, 5, 1, 20, 0, 0),
            status: "paid",
            amount: 10000);

        var service = new HostSalesService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetDetailAsync(hostUserId: 10, reservationId: 1));
    }

    [Fact]
    public async Task GetDetailAsync_ThrowsKeyNotFoundException_WhenReservationDoesNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var service = new HostSalesService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetDetailAsync(hostUserId: 10, reservationId: 999));
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

    private static async Task SeedChargeItemAsync(
        AppDbContext context,
        int id,
        int reservationId,
        string kind,
        string? description,
        DateTime? sliceStart,
        DateTime? sliceEnd,
        int? unitRatePerHour,
        int sliceAmount)
    {
        context.ReservationChargeItems.Add(new ReservationChargeItem
        {
            Id = id,
            ReservationId = reservationId,
            Kind = kind,
            Description = description,
            SliceStart = sliceStart,
            SliceEnd = sliceEnd,
            UnitRatePerHour = unitRatePerHour,
            SliceAmount = sliceAmount
        });

        await context.SaveChangesAsync();
    }
}
