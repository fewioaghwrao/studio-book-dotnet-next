using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Controllers;

public class HostSalesControllerTests
{
    [Fact]
    public async Task GetList_ReturnsOk_WhenSalesExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "paid",
            amount: 6000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.GetList(
            roomId: null,
            onlyWithItems: true,
            page: 1,
            pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HostSalesListResponseDto>(okResult.Value);

        Assert.Single(response.Items);
        Assert.Equal(1, response.Items[0].ReservationId);
        Assert.Equal("新宿スタジオ", response.Items[0].RoomName);
        Assert.Equal("ゲストA", response.Items[0].GuestName);
        Assert.Equal(6000, response.Items[0].Amount);
        Assert.True(response.Items[0].HasItems);
    }

    [Fact]
    public async Task GetList_ReturnsOnlyLoginHostSales()
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
            amount: 7000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.GetList(
            roomId: null,
            onlyWithItems: true,
            page: 1,
            pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HostSalesListResponseDto>(okResult.Value);

        Assert.Single(response.Items);
        Assert.Equal(1, response.Items[0].ReservationId);
        Assert.Equal("ホストAのスタジオ", response.Items[0].RoomName);
    }

    [Fact]
    public async Task GetDetail_ReturnsOk_WhenReservationBelongsToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");

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
            kind: "tax",
            description: "消費税",
            sliceStart: null,
            sliceEnd: null,
            unitRatePerHour: null,
            sliceAmount: 3300);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.GetDetail(reservationId: 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HostSalesDetailResponseDto>(okResult.Value);

        Assert.Equal(1, response.ReservationId);
        Assert.Equal("新宿スタジオ", response.RoomName);
        Assert.Equal("ゲストA", response.GuestName);
        Assert.Equal(36300, response.Amount);
        Assert.Equal("paid", response.Status);

        Assert.Equal(2, response.Items.Count);
        Assert.Equal("base", response.Items[0].Kind);
        Assert.Equal("tax", response.Items[1].Kind);
    }

    [Fact]
    public async Task GetDetail_ReturnsNotFound_WhenReservationDoesNotBelongToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedHostUserAsync(context, userId: 99, name: "ホストB");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 99, name: "他ホストのスタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 9, 0, 0),
            endAt: new DateTime(2026, 5, 1, 20, 0, 0),
            status: "paid",
            amount: 10000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.GetDetail(reservationId: 1);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetDetail_ReturnsNotFound_WhenReservationDoesNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.GetDetail(reservationId: 999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetList_ThrowsUnauthorizedAccessException_WhenUserIdClaimIsMissing()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var service = new HostSalesService(context);
        var controller = new HostSalesController(service);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            controller.GetList(
                roomId: null,
                onlyWithItems: true,
                page: 1,
                pageSize: 10));
    }

    [Fact]
    public async Task GetDetail_ThrowsUnauthorizedAccessException_WhenUserIdClaimIsMissing()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var service = new HostSalesService(context);
        var controller = new HostSalesController(service);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            controller.GetDetail(reservationId: 1));
    }

    private static HostSalesController CreateController(
        AppDbContext context,
        int userId)
    {
        var service = new HostSalesService(context);
        var controller = new HostSalesController(service);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, "Host")
                }, "TestAuth"))
            }
        };

        return controller;
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