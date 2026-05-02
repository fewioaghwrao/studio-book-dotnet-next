using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Reservations;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Settings;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Controllers;

public class ReservationsControllerTests
{
    [Fact]
    public async Task GetMyReservations_ReturnsOk_WhenUserIsGeneralUser()
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

        var controller = CreateController(
            context,
            userId: 10,
            roles: new[] { "GeneralUser" });

        // Act
        var result = await controller.GetMyReservations(
            page: 1,
            pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserReservationListResponse>(okResult.Value);

        Assert.Single(response.Items);
        Assert.Equal(1, response.Items[0].ReservationId);
        Assert.Equal("新宿スタジオ", response.Items[0].RoomName);
        Assert.Equal(6000, response.Items[0].Amount);

        Assert.Equal(1, response.Page);
        Assert.Equal(10, response.PageSize);
        Assert.Equal(1, response.TotalCount);
        Assert.Equal(1, response.TotalPages);
    }

    [Fact]
    public async Task GetMyReservations_ReturnsOnlyLoginUserReservations()
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

        var controller = CreateController(
            context,
            userId: 10,
            roles: new[] { "GeneralUser" });

        // Act
        var result = await controller.GetMyReservations(
            page: 1,
            pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserReservationListResponse>(okResult.Value);

        Assert.Single(response.Items);
        Assert.Equal(1, response.Items[0].ReservationId);
    }

    [Fact]
    public async Task GetMyReservations_ReturnsUnauthorized_WhenUserIdClaimIsMissing()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var controller = CreateControllerWithoutUserId(
            context,
            roles: new[] { "GeneralUser" });

        // Act
        var result = await controller.GetMyReservations(
            page: 1,
            pageSize: 10);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task GetMyReservations_ReturnsForbid_WhenUserIsNotGeneralUser()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var controller = CreateController(
            context,
            userId: 10,
            roles: new[] { "Host" });

        // Act
        var result = await controller.GetMyReservations(
            page: 1,
            pageSize: 10);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetMyReservations_ReturnsForbid_WhenUserHasNoRole()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var controller = CreateController(
            context,
            userId: 10,
            roles: Array.Empty<string>());

        // Act
        var result = await controller.GetMyReservations(
            page: 1,
            pageSize: 10);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetMyReservations_AppliesPaging()
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

        var controller = CreateController(
            context,
            userId: 10,
            roles: new[] { "GeneralUser" });

        // Act
        var result = await controller.GetMyReservations(
            page: 2,
            pageSize: 2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserReservationListResponse>(okResult.Value);

        Assert.Single(response.Items);
        Assert.Equal(1, response.Items[0].ReservationId);

        Assert.Equal(2, response.Page);
        Assert.Equal(2, response.PageSize);
        Assert.Equal(3, response.TotalCount);
        Assert.Equal(2, response.TotalPages);
    }

    private static ReservationsController CreateController(
        AppDbContext context,
        int userId,
        IReadOnlyCollection<string> roles)
    {
        var controller = CreateControllerCore(context);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };

        return controller;
    }

    private static ReservationsController CreateControllerWithoutUserId(
        AppDbContext context,
        IReadOnlyCollection<string> roles)
    {
        var controller = CreateControllerCore(context);

        var claims = roles
            .Select(role => new Claim(ClaimTypes.Role, role))
            .ToList();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };

        return controller;
    }

    private static ReservationsController CreateControllerCore(AppDbContext context)
    {
        var adminSettingsService = new AdminSettingsService(context);
        var reservationConfirmService = new ReservationConfirmService(
            context,
            adminSettingsService);

        var stripeCheckoutService = new StripeCheckoutService(
            Options.Create(new StripeSettings
            {
                PublishableKey = "pk_test_dummy",
                SecretKey = "sk_test_dummy",
                SuccessUrl = "https://frontend.example.com/reservations/complete",
                CancelUrl = "https://frontend.example.com/rooms/{ROOM_ID}"
            }));

        var userReservationService = new UserReservationService(context);

        return new ReservationsController(
            reservationConfirmService,
            stripeCheckoutService,
            userReservationService);
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