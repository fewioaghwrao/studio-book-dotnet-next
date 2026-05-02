using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Controllers;

public class HostStatusControllerTests
{
    [Fact]
    public async Task Get_ReturnsOk_WithSpecifiedYearMonth()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");

        await SeedBusinessHoursForWeekdaysAsync(context, roomId: 1);

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 10, 10, 0, 0),
            endAt: new DateTime(2026, 5, 10, 12, 0, 0),
            status: "paid",
            amount: 6000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.Get(
            roomId: null,
            year: 2026,
            month: 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HostStatusResponseDto>(okResult.Value);

        Assert.Equal(new List<string> { "2026-03", "2026-04", "2026-05" }, response.Labels);
        Assert.Equal(new List<int> { 0, 0, 6000 }, response.Paid);
        Assert.Single(response.RoomOptions);
    }

    [Fact]
    public async Task Get_TreatsRoomIdZeroAsNull()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");
        await SeedRoomAsync(context, roomId: 2, hostUserId: 10, name: "渋谷スタジオ");

        await SeedBusinessHoursForWeekdaysAsync(context, roomId: 1);
        await SeedBusinessHoursForWeekdaysAsync(context, roomId: 2);

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 10, 10, 0, 0),
            endAt: new DateTime(2026, 5, 10, 12, 0, 0),
            status: "paid",
            amount: 6000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 2,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 11, 10, 0, 0),
            endAt: new DateTime(2026, 5, 11, 12, 0, 0),
            status: "paid",
            amount: 9000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.Get(
            roomId: 0,
            year: 2026,
            month: 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HostStatusResponseDto>(okResult.Value);

        Assert.Equal(new List<int> { 0, 0, 15000 }, response.Paid);
        Assert.Equal(2, response.RoomOptions.Count);
    }

    [Fact]
    public async Task Get_FiltersByRoomId_WhenRoomIdIsSpecified()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");
        await SeedRoomAsync(context, roomId: 2, hostUserId: 10, name: "渋谷スタジオ");

        await SeedBusinessHoursForWeekdaysAsync(context, roomId: 1);
        await SeedBusinessHoursForWeekdaysAsync(context, roomId: 2);

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 10, 10, 0, 0),
            endAt: new DateTime(2026, 5, 10, 12, 0, 0),
            status: "paid",
            amount: 6000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 2,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 11, 10, 0, 0),
            endAt: new DateTime(2026, 5, 11, 12, 0, 0),
            status: "paid",
            amount: 9000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.Get(
            roomId: 2,
            year: 2026,
            month: 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HostStatusResponseDto>(okResult.Value);

        Assert.Equal(new List<int> { 0, 0, 9000 }, response.Paid);
        Assert.Equal(2, response.RoomOptions.Count);
    }

    [Fact]
    public async Task Get_UsesCurrentMonth_WhenMonthIsInvalid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");

        var controller = CreateController(context, userId: 10);

        var expectedBaseMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var expectedLabels = new List<string>
        {
            expectedBaseMonth.AddMonths(-2).ToString("yyyy-MM"),
            expectedBaseMonth.AddMonths(-1).ToString("yyyy-MM"),
            expectedBaseMonth.ToString("yyyy-MM")
        };

        // Act
        var result = await controller.Get(
            roomId: null,
            year: 2026,
            month: 13);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HostStatusResponseDto>(okResult.Value);

        Assert.Equal(expectedLabels, response.Labels);
        Assert.Equal(new List<int> { 0, 0, 0 }, response.Booked);
        Assert.Equal(new List<int> { 0, 0, 0 }, response.Paid);
    }

    [Fact]
    public async Task Get_ThrowsUnauthorizedAccessException_WhenUserIdClaimIsMissing()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var service = new HostStatusService(context);
        var controller = new HostStatusController(service);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            controller.Get(
                roomId: null,
                year: 2026,
                month: 5));
    }

    private static HostStatusController CreateController(
        AppDbContext context,
        int userId)
    {
        var service = new HostStatusService(context);
        var controller = new HostStatusController(service);

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

    private static async Task SeedBusinessHoursForWeekdaysAsync(
        AppDbContext context,
        int roomId,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null)
    {
        var start = startTime ?? new TimeOnly(9, 0);
        var end = endTime ?? new TimeOnly(18, 0);

        for (var dayOfWeek = 1; dayOfWeek <= 5; dayOfWeek++)
        {
            context.BusinessHours.Add(new BusinessHour
            {
                RoomId = roomId,
                DayOfWeek = dayOfWeek,
                StartTime = start,
                EndTime = end,
                IsHoliday = false,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }
}