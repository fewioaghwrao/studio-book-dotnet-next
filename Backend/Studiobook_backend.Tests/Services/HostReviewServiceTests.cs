using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Services;

public class HostReviewServiceTests
{
    [Fact]
    public async Task GetListAsync_ReturnsOnlyReviewsForHostRooms()
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
            content: "とても良かったです。",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        await SeedReviewAsync(
            context,
            reviewId: 2,
            roomId: 2,
            userId: 20,
            score: 1,
            content: "他ホストのレビューです。",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 2, 10, 0, 0));

        var service = new HostReviewService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            roomId: null,
            stars: null,
            onlyHidden: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].Id);
        Assert.Equal("ホストAのスタジオ", result.Items[0].RoomName);
        Assert.Equal("ゲストA", result.Items[0].UserName);
        Assert.Equal(5, result.Items[0].Score);

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

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 5,
            content: "新宿のレビュー",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        await SeedReviewAsync(
            context,
            reviewId: 2,
            roomId: 2,
            userId: 20,
            score: 4,
            content: "渋谷のレビュー",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 2, 10, 0, 0));

        var service = new HostReviewService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            roomId: 2,
            stars: null,
            onlyHidden: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal(2, result.Items[0].RoomId);
        Assert.Equal("渋谷スタジオ", result.Items[0].RoomName);
    }

    [Fact]
    public async Task GetListAsync_FiltersByStars()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 5,
            content: "星5レビュー",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        await SeedReviewAsync(
            context,
            reviewId: 2,
            roomId: 1,
            userId: 20,
            score: 3,
            content: "星3レビュー",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 2, 10, 0, 0));

        var service = new HostReviewService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            roomId: null,
            stars: 3,
            onlyHidden: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal(3, result.Items[0].Score);
    }

    [Fact]
    public async Task GetListAsync_FiltersOnlyHidden()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 5,
            content: "公開レビュー",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        await SeedReviewAsync(
            context,
            reviewId: 2,
            roomId: 1,
            userId: 20,
            score: 1,
            content: "非公開レビュー",
            publicVisible: false,
            hiddenReason: "不適切な内容",
            createdAtUtc: new DateTime(2026, 5, 2, 10, 0, 0));

        var service = new HostReviewService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            roomId: null,
            stars: null,
            onlyHidden: true,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
        Assert.False(result.Items[0].PublicVisible);
        Assert.Equal("不適切な内容", result.Items[0].HiddenReason);
    }

    [Fact]
    public async Task GetListAsync_AppliesPaging()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 5,
            content: "レビュー1",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        await SeedReviewAsync(
            context,
            reviewId: 2,
            roomId: 1,
            userId: 20,
            score: 4,
            content: "レビュー2",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 2, 10, 0, 0));

        await SeedReviewAsync(
            context,
            reviewId: 3,
            roomId: 1,
            userId: 20,
            score: 3,
            content: "レビュー3",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 3, 10, 0, 0));

        var service = new HostReviewService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            roomId: null,
            stars: null,
            onlyHidden: null,
            page: 2,
            pageSize: 2);

        // Assert
        Assert.Single(result.Items);

        // OrderByDescending(CreatedAtUtc)
        // 1ページ目: ID 3, 2
        // 2ページ目: ID 1
        Assert.Equal(1, result.Items[0].Id);

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

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 5,
            content: "レビュー",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        var service = new HostReviewService(context);

        // Act
        var result = await service.GetListAsync(
            hostUserId: 10,
            roomId: null,
            stars: null,
            onlyHidden: null,
            page: 0,
            pageSize: 0);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task SaveReplyAsync_SavesTrimmedReply_WhenReviewBelongsToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 5,
            content: "良かったです。",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        var service = new HostReviewService(context);

        var request = new HostReviewReplyRequestDto
        {
            HostReply = "  ご利用ありがとうございました。  "
        };

        // Act
        await service.SaveReplyAsync(hostUserId: 10, reviewId: 1, request);

        // Assert
        var review = await context.Reviews.SingleAsync(x => x.Id == 1);

        Assert.Equal("ご利用ありがとうございました。", review.HostReply);
        Assert.NotNull(review.HostReplyAt);
    }

    [Fact]
    public async Task SaveReplyAsync_SetsHostReplyNull_WhenReplyIsWhitespace()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 5,
            content: "良かったです。",
            publicVisible: true,
            hostReply: "既存返信",
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        var service = new HostReviewService(context);

        var request = new HostReviewReplyRequestDto
        {
            HostReply = "   "
        };

        // Act
        await service.SaveReplyAsync(hostUserId: 10, reviewId: 1, request);

        // Assert
        var review = await context.Reviews.SingleAsync(x => x.Id == 1);

        Assert.Null(review.HostReply);
        Assert.NotNull(review.HostReplyAt);
    }

    [Fact]
    public async Task SaveReplyAsync_ThrowsKeyNotFoundException_WhenReviewDoesNotBelongToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedHostUserAsync(context, 99, "ホストB");
        await SeedGuestUserAsync(context, 20, "ゲストA");

        await SeedRoomAsync(context, 1, 99, "他人のスタジオ");

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 5,
            content: "他人の部屋へのレビュー",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        var service = new HostReviewService(context);

        var request = new HostReviewReplyRequestDto
        {
            HostReply = "返信できないはず"
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.SaveReplyAsync(hostUserId: 10, reviewId: 1, request));

        var review = await context.Reviews.SingleAsync(x => x.Id == 1);
        Assert.Null(review.HostReply);
        Assert.Null(review.HostReplyAt);
    }

    [Fact]
    public async Task ChangeVisibilityAsync_ChangesToHiddenWithTrimmedReason()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 1,
            content: "非公開にしたいレビュー",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        var service = new HostReviewService(context);

        var request = new HostReviewVisibilityRequestDto
        {
            IsPublic = false,
            Reason = "  不適切な内容  "
        };

        // Act
        await service.ChangeVisibilityAsync(hostUserId: 10, reviewId: 1, request);

        // Assert
        var review = await context.Reviews.SingleAsync(x => x.Id == 1);

        Assert.False(review.PublicVisible);
        Assert.Equal("不適切な内容", review.HiddenReason);
    }

    [Fact]
    public async Task ChangeVisibilityAsync_ClearsHiddenReason_WhenChangedToPublic()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 1,
            content: "公開に戻すレビュー",
            publicVisible: false,
            hiddenReason: "不適切な内容",
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        var service = new HostReviewService(context);

        var request = new HostReviewVisibilityRequestDto
        {
            IsPublic = true,
            Reason = "理由は無視される"
        };

        // Act
        await service.ChangeVisibilityAsync(hostUserId: 10, reviewId: 1, request);

        // Assert
        var review = await context.Reviews.SingleAsync(x => x.Id == 1);

        Assert.True(review.PublicVisible);
        Assert.Null(review.HiddenReason);
    }

    [Fact]
    public async Task ChangeVisibilityAsync_ThrowsKeyNotFoundException_WhenReviewDoesNotBelongToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedHostUserAsync(context, 99, "ホストB");
        await SeedGuestUserAsync(context, 20, "ゲストA");

        await SeedRoomAsync(context, 1, 99, "他人のスタジオ");

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 1,
            content: "他ホストのレビュー",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        var service = new HostReviewService(context);

        var request = new HostReviewVisibilityRequestDto
        {
            IsPublic = false,
            Reason = "非公開にしたい"
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ChangeVisibilityAsync(hostUserId: 10, reviewId: 1, request));

        var review = await context.Reviews.SingleAsync(x => x.Id == 1);

        Assert.True(review.PublicVisible);
        Assert.Null(review.HiddenReason);
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