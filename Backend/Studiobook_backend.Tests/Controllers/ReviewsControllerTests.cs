using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Reviews;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Controllers;

public class ReviewsControllerTests
{
    [Fact]
    public async Task GetNewReviewPage_ReturnsOk_WhenRoomExists()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ログインユーザー");
        await SeedGuestUserAsync(context, 21, "レビュー投稿者");

        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            userId: 20,
            status: "paid");

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 21,
            score: 5,
            content: "良かったです。",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        var controller = CreateController(context, userId: 20);

        // Act
        var result = await controller.GetNewReviewPage(
            roomId: 1,
            reservationId: null,
            page: 1,
            pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RoomReviewPageResponse>(okResult.Value);

        Assert.Equal(1, response.RoomId);
        Assert.Equal("新宿スタジオ", response.RoomName);
        Assert.Single(response.Reviews);
        Assert.True(response.CanReview);
    }

    [Fact]
    public async Task GetNewReviewPage_ReturnsUnauthorized_WhenUserIdClaimIsMissing()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var controller = new ReviewsController(new ReviewService(context))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        // Act
        var result = await controller.GetNewReviewPage(
            roomId: 1,
            reservationId: null,
            page: 1,
            pageSize: 10);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task GetNewReviewPage_ReturnsNotFound_WhenRoomDoesNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var controller = CreateController(context, userId: 20);

        // Act
        var result = await controller.GetNewReviewPage(
            roomId: 999,
            reservationId: null,
            page: 1,
            pageSize: 10);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task Create_ReturnsOk_WhenReviewIsCreated()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ログインユーザー");

        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            userId: 20,
            status: "paid");

        var controller = CreateController(context, userId: 20);

        var request = new CreateReviewRequest
        {
            Score = 5,
            Content = "  とても良かったです。  ",
            ReservationId = 1
        };

        // Act
        var result = await controller.Create(
            roomId: 1,
            request: request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var review = await context.Reviews.SingleAsync();

        Assert.Equal(1, review.RoomId);
        Assert.Equal(20, review.UserId);
        Assert.Equal(5, review.Score);
        Assert.Equal("とても良かったです。", review.Content);
        Assert.True(review.PublicVisible);
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WhenUserIdClaimIsMissing()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var controller = new ReviewsController(new ReviewService(context))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        var request = new CreateReviewRequest
        {
            Score = 5,
            Content = "レビュー本文"
        };

        // Act
        var result = await controller.Create(
            roomId: 1,
            request: request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task Create_ReturnsNotFound_WhenRoomDoesNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var controller = CreateController(context, userId: 20);

        var request = new CreateReviewRequest
        {
            Score = 5,
            Content = "レビュー本文"
        };

        // Act
        var result = await controller.Create(
            roomId: 999,
            request: request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenScoreIsInvalid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var controller = CreateController(context, userId: 20);

        var request = new CreateReviewRequest
        {
            Score = 6,
            Content = "レビュー本文"
        };

        // Act
        var result = await controller.Create(
            roomId: 1,
            request: request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenUserHasNoPaidReservation()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ログインユーザー");

        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            userId: 20,
            status: "booked");

        var controller = CreateController(context, userId: 20);

        var request = new CreateReviewRequest
        {
            Score = 5,
            Content = "レビュー本文"
        };

        // Act
        var result = await controller.Create(
            roomId: 1,
            request: request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenAlreadyReviewed()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ログインユーザー");

        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            userId: 20,
            status: "paid");

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 5,
            content: "既存レビュー",
            publicVisible: true,
            createdAtUtc: DateTime.UtcNow);

        var controller = CreateController(context, userId: 20);

        var request = new CreateReviewRequest
        {
            Score = 4,
            Content = "再投稿"
        };

        // Act
        var result = await controller.Create(
            roomId: 1,
            request: request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    private static ReviewsController CreateController(
        AppDbContext context,
        int userId)
    {
        var controller = new ReviewsController(new ReviewService(context));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
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
        int userId,
        string status)
    {
        context.Reservations.Add(new Reservation
        {
            Id = reservationId,
            RoomId = roomId,
            UserId = userId,
            StartAt = new DateTime(2026, 5, 1, 10, 0, 0).AddDays(reservationId),
            EndAt = new DateTime(2026, 5, 1, 12, 0, 0).AddDays(reservationId),
            Amount = status == "paid" ? 6000 : 0,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedReviewAsync(
        AppDbContext context,
        int reviewId,
        int roomId,
        int userId,
        int score,
        string content,
        bool publicVisible,
        DateTime createdAtUtc)
    {
        context.Reviews.Add(new Review
        {
            Id = reviewId,
            RoomId = roomId,
            UserId = userId,
            Score = score,
            Content = content,
            PublicVisible = publicVisible,
            HiddenReason = publicVisible ? null : "非公開理由",
            HostReply = null,
            HostReplyAt = null,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc
        });

        await context.SaveChangesAsync();
    }
}