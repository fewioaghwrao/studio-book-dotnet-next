using System.IdentityModel.Tokens.Jwt;
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
using Microsoft.Extensions.Logging.Abstractions;

namespace Studiobook_backend.Tests.Controllers;

public class HostReservationsControllerTests
{
    [Fact]
    public async Task GetList_ReturnsOk_WhenReservationsExist()
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
            status: "booked",
            amount: 6000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.GetList(
            keyword: null,
            status: null,
            reservationId: null,
            roomId: null,
            startFrom: null,
            startTo: null,
            page: 1,
            pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HostReservationListResponseDto>(okResult.Value);

        Assert.Single(response.Items);
        Assert.Equal(1, response.Items[0].ReservationId);
        Assert.Equal("新宿スタジオ", response.Items[0].RoomName);
        Assert.Equal("ゲストA", response.Items[0].GuestName);
        Assert.Equal(1, response.TotalCount);
    }

    [Fact]
    public async Task GetList_ReturnsOnlyLoginHostReservations()
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

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.GetList(
            keyword: null,
            status: null,
            reservationId: null,
            roomId: null,
            startFrom: null,
            startTo: null,
            page: 1,
            pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HostReservationListResponseDto>(okResult.Value);

        Assert.Single(response.Items);
        Assert.Equal(1, response.Items[0].ReservationId);
        Assert.Equal("ホストAのスタジオ", response.Items[0].RoomName);
    }

    [Fact]
    public async Task Approve_ReturnsNoContent_AndWritesAuditLog_WhenReservationIsBooked()
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
            status: "booked",
            amount: 6000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.Approve(reservationId: 1);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var reservation = await context.Reservations.SingleAsync(x => x.Id == 1);
        Assert.Equal("paid", reservation.Status);

        var auditLog = Assert.Single(context.AuditLogs);
        Assert.Equal(10, auditLog.ActorId);
        Assert.Equal("APPROVE", auditLog.Action);
        Assert.Equal("Reservation", auditLog.Entity);
        Assert.Equal(1, auditLog.EntityId);
        Assert.Contains("承認", auditLog.Note);
    }

    [Fact]
    public async Task Approve_ReturnsNotFound_WhenReservationDoesNotBelongToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedHostUserAsync(context, userId: 99, name: "ホストB");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 99, name: "他人のスタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "booked",
            amount: 6000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.Approve(reservationId: 1);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);

        var reservation = await context.Reservations.SingleAsync(x => x.Id == 1);
        Assert.Equal("booked", reservation.Status);

        Assert.Empty(context.AuditLogs);
    }

    [Fact]
    public async Task Cancel_ReturnsNoContent_AndWritesAuditLog_WhenReservationIsBooked()
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
            status: "booked",
            amount: 6000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.Cancel(reservationId: 1);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var reservation = await context.Reservations.SingleAsync(x => x.Id == 1);
        Assert.Equal("canceled", reservation.Status);

        var auditLog = Assert.Single(context.AuditLogs);
        Assert.Equal(10, auditLog.ActorId);
        Assert.Equal("CANCEL", auditLog.Action);
        Assert.Equal("Reservation", auditLog.Entity);
        Assert.Equal(1, auditLog.EntityId);
        Assert.Contains("キャンセル", auditLog.Note);
    }

    [Fact]
    public async Task Cancel_ReturnsNotFound_WhenReservationDoesNotBelongToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedHostUserAsync(context, userId: 99, name: "ホストB");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 99, name: "他人のスタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "booked",
            amount: 6000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.Cancel(reservationId: 1);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);

        var reservation = await context.Reservations.SingleAsync(x => x.Id == 1);
        Assert.Equal("booked", reservation.Status);

        Assert.Empty(context.AuditLogs);
    }

    [Fact]
    public async Task GetList_ThrowsUnauthorizedAccessException_WhenUserIdClaimIsMissing()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var reservationService = new HostReservationService(context);
        var auditLogService = new AuditLogService(
            context,
            NullLogger<AuditLogService>.Instance);

        var controller = new HostReservationsController(
            reservationService,
            auditLogService);

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
                keyword: null,
                status: null,
                reservationId: null,
                roomId: null,
                startFrom: null,
                startTo: null,
                page: 1,
                pageSize: 10));
    }

    private static HostReservationsController CreateController(
        AppDbContext context,
        int userId)
    {
        var reservationService = new HostReservationService(context);
        var auditLogService = new AuditLogService(
     context,
     NullLogger<AuditLogService>.Instance);

        var controller = new HostReservationsController(
            reservationService,
            auditLogService);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
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