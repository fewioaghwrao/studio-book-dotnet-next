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

public class HostReviewsControllerTests
{
    [Fact]
    public async Task GetList_ReturnsOk_WhenReviewsExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 5,
            content: "とても良かったです。",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.GetList(
            roomId: null,
            stars: null,
            onlyHidden: null,
            page: 1,
            pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HostReviewListResponseDto>(okResult.Value);

        Assert.Single(response.Items);
        Assert.Equal(1, response.Items[0].Id);
        Assert.Equal("新宿スタジオ", response.Items[0].RoomName);
        Assert.Equal("ゲストA", response.Items[0].UserName);
        Assert.Equal(5, response.Items[0].Score);
    }

    [Fact]
    public async Task GetList_ReturnsOnlyLoginHostReviews()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedHostUserAsync(context, userId: 99, name: "ホストB");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "ホストAのスタジオ");
        await SeedRoomAsync(context, roomId: 2, hostUserId: 99, name: "ホストBのスタジオ");

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 5,
            content: "ホストAのレビュー",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        await SeedReviewAsync(
            context,
            reviewId: 2,
            roomId: 2,
            userId: 20,
            score: 1,
            content: "ホストBのレビュー",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 2, 10, 0, 0));

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.GetList(
            roomId: null,
            stars: null,
            onlyHidden: null,
            page: 1,
            pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HostReviewListResponseDto>(okResult.Value);

        Assert.Single(response.Items);
        Assert.Equal(1, response.Items[0].Id);
        Assert.Equal("ホストAのスタジオ", response.Items[0].RoomName);
    }

    [Fact]
    public async Task Reply_ReturnsNoContent_WhenReviewBelongsToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 5,
            content: "良かったです。",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        var controller = CreateController(context, userId: 10);

        var request = new HostReviewReplyRequestDto
        {
            HostReply = "  ご利用ありがとうございました。  "
        };

        // Act
        var result = await controller.Reply(reviewId: 1, request);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var review = await context.Reviews.SingleAsync(x => x.Id == 1);
        Assert.Equal("ご利用ありがとうございました。", review.HostReply);
        Assert.NotNull(review.HostReplyAt);
    }

    [Fact]
    public async Task Reply_ReturnsNotFound_WhenReviewDoesNotBelongToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedHostUserAsync(context, userId: 99, name: "ホストB");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 99, name: "他人のスタジオ");

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 5,
            content: "他ホストのレビュー",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        var controller = CreateController(context, userId: 10);

        var request = new HostReviewReplyRequestDto
        {
            HostReply = "返信できないはず"
        };

        // Act
        var result = await controller.Reply(reviewId: 1, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);

        var review = await context.Reviews.SingleAsync(x => x.Id == 1);
        Assert.Null(review.HostReply);
        Assert.Null(review.HostReplyAt);
    }

    [Fact]
    public async Task ChangeVisibility_ReturnsNoContent_WhenReviewBelongsToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 1,
            content: "非公開にするレビュー",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        var controller = CreateController(context, userId: 10);

        var request = new HostReviewVisibilityRequestDto
        {
            IsPublic = false,
            Reason = "  不適切な内容  "
        };

        // Act
        var result = await controller.ChangeVisibility(reviewId: 1, request);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var review = await context.Reviews.SingleAsync(x => x.Id == 1);
        Assert.False(review.PublicVisible);
        Assert.Equal("不適切な内容", review.HiddenReason);
    }

    [Fact]
    public async Task ChangeVisibility_ReturnsNotFound_WhenReviewDoesNotBelongToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedHostUserAsync(context, userId: 99, name: "ホストB");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 99, name: "他人のスタジオ");

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 1,
            content: "他ホストのレビュー",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        var controller = CreateController(context, userId: 10);

        var request = new HostReviewVisibilityRequestDto
        {
            IsPublic = false,
            Reason = "非公開にしたい"
        };

        // Act
        var result = await controller.ChangeVisibility(reviewId: 1, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);

        var review = await context.Reviews.SingleAsync(x => x.Id == 1);
        Assert.True(review.PublicVisible);
        Assert.Null(review.HiddenReason);
    }

    [Fact]
    public async Task GetList_ThrowsUnauthorizedAccessException_WhenUserIdClaimIsMissing()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var service = new HostReviewService(context);
        var controller = new HostReviewsController(service);

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
                stars: null,
                onlyHidden: null,
                page: 1,
                pageSize: 10));
    }

    private static HostReviewsController CreateController(
        AppDbContext context,
        int userId)
    {
        var service = new HostReviewService(context);
        var controller = new HostReviewsController(service);

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

    private static async Task SeedReviewAsync(
        AppDbContext context,
        int reviewId,
        int roomId,
        int userId,
        int score,
        string content,
        bool publicVisible,
        DateTime createdAtUtc,
        string? hiddenReason = null,
        string? hostReply = null,
        DateTime? hostReplyAt = null)
    {
        context.Reviews.Add(new Review
        {
            Id = reviewId,
            RoomId = roomId,
            UserId = userId,
            Score = score,
            Content = content,
            PublicVisible = publicVisible,
            HiddenReason = hiddenReason,
            HostReply = hostReply,
            HostReplyAt = hostReplyAt,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc
        });

        await context.SaveChangesAsync();
    }
}