using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Reviews;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Services;

public class ReviewServiceTests
{
    [Fact]
    public async Task GetRoomReviewPageAsync_ReturnsRoomAndPublicReviews()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ログインユーザー");
        await SeedGuestUserAsync(context, userId: 21, name: "レビュー投稿者A");
        await SeedGuestUserAsync(context, userId: 22, name: "レビュー投稿者B");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");

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
            content: "とても良かったです。",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        await SeedReviewAsync(
            context,
            reviewId: 2,
            roomId: 1,
            userId: 22,
            score: 3,
            content: "普通でした。",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 2, 10, 0, 0));

        var service = new ReviewService(context);

        // Act
        var result = await service.GetRoomReviewPageAsync(
            roomId: 1,
            userId: 20,
            page: 1,
            pageSize: 10,
            reservationId: null);

        // Assert
        Assert.Equal(1, result.RoomId);
        Assert.Equal("新宿スタジオ", result.RoomName);
        Assert.Equal("room01.jpg", result.RoomImageName);
        Assert.Equal("東京都テスト区サンプル1", result.RoomAddress);

        Assert.Equal(4.0, result.AverageScore);
        Assert.Equal(2, result.ReviewCount);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.TotalPages);

        Assert.False(result.AlreadyReviewed);
        Assert.True(result.CanReview);

        Assert.Equal(2, result.Reviews.Count);

        // CreatedAtUtc desc
        Assert.Equal(2, result.Reviews[0].Id);
        Assert.Equal("普通でした。", result.Reviews[0].Content);
        Assert.Equal("レビュー投稿者B", result.Reviews[0].UserName);

        Assert.Equal(1, result.Reviews[1].Id);
        Assert.Equal("とても良かったです。", result.Reviews[1].Content);
        Assert.Equal("レビュー投稿者A", result.Reviews[1].UserName);
    }

    [Fact]
    public async Task GetRoomReviewPageAsync_ExcludesHiddenReviewsFromListAndAverage()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ログインユーザー");
        await SeedGuestUserAsync(context, 21, "公開ユーザー");
        await SeedGuestUserAsync(context, 22, "非公開ユーザー");

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
            content: "公開レビュー",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        await SeedReviewAsync(
            context,
            reviewId: 2,
            roomId: 1,
            userId: 22,
            score: 1,
            content: "非公開レビュー",
            publicVisible: false,
            createdAtUtc: new DateTime(2026, 5, 2, 10, 0, 0));

        var service = new ReviewService(context);

        // Act
        var result = await service.GetRoomReviewPageAsync(
            roomId: 1,
            userId: 20,
            page: 1,
            pageSize: 10,
            reservationId: null);

        // Assert
        Assert.Single(result.Reviews);
        Assert.Equal(1, result.Reviews[0].Id);
        Assert.Equal("公開レビュー", result.Reviews[0].Content);

        Assert.Equal(5.0, result.AverageScore);
        Assert.Equal(1, result.ReviewCount);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetRoomReviewPageAsync_ReturnsZeroAverage_WhenNoPublicReviews()
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

        var service = new ReviewService(context);

        // Act
        var result = await service.GetRoomReviewPageAsync(
            roomId: 1,
            userId: 20,
            page: 1,
            pageSize: 10,
            reservationId: null);

        // Assert
        Assert.Empty(result.Reviews);
        Assert.Equal(0.0, result.AverageScore);
        Assert.Equal(0, result.ReviewCount);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.True(result.CanReview);
    }

    [Fact]
    public async Task GetRoomReviewPageAsync_NormalizesPageAndPageSize()
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

        var service = new ReviewService(context);

        // Act
        var result = await service.GetRoomReviewPageAsync(
            roomId: 1,
            userId: 20,
            page: 0,
            pageSize: 0,
            reservationId: null);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetRoomReviewPageAsync_ClampsPageSizeTo50()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ログインユーザー");

        await SeedRoomAsync(context, 1, 10, "新宿スタジオ");

        for (var i = 1; i <= 60; i++)
        {
            await SeedGuestUserAsync(context, 100 + i, $"レビュー投稿者{i}");

            await SeedReviewAsync(
                context,
                reviewId: i,
                roomId: 1,
                userId: 100 + i,
                score: 5,
                content: $"レビュー{i}",
                publicVisible: true,
                createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0).AddDays(i));
        }

        var service = new ReviewService(context);

        // Act
        var result = await service.GetRoomReviewPageAsync(
            roomId: 1,
            userId: 20,
            page: 1,
            pageSize: 100,
            reservationId: null);

        // Assert
        Assert.Equal(50, result.PageSize);
        Assert.Equal(50, result.Reviews.Count);
        Assert.Equal(60, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetRoomReviewPageAsync_ReturnsAlreadyReviewedTrueAndCanReviewFalse_WhenUserAlreadyReviewed()
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
            score: 4,
            content: "自分のレビュー",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        var service = new ReviewService(context);

        // Act
        var result = await service.GetRoomReviewPageAsync(
            roomId: 1,
            userId: 20,
            page: 1,
            pageSize: 10,
            reservationId: null);

        // Assert
        Assert.True(result.AlreadyReviewed);
        Assert.False(result.CanReview);
    }

    [Fact]
    public async Task GetRoomReviewPageAsync_ReturnsCanReviewFalse_WhenUserHasNoPaidReservation()
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

        var service = new ReviewService(context);

        // Act
        var result = await service.GetRoomReviewPageAsync(
            roomId: 1,
            userId: 20,
            page: 1,
            pageSize: 10,
            reservationId: null);

        // Assert
        Assert.False(result.AlreadyReviewed);
        Assert.False(result.CanReview);
    }

    [Fact]
    public async Task GetRoomReviewPageAsync_UsesReservationId_WhenSpecified()
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

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            userId: 20,
            status: "paid");

        var service = new ReviewService(context);

        // Act
        var resultForBooked = await service.GetRoomReviewPageAsync(
            roomId: 1,
            userId: 20,
            page: 1,
            pageSize: 10,
            reservationId: 1);

        var resultForPaid = await service.GetRoomReviewPageAsync(
            roomId: 1,
            userId: 20,
            page: 1,
            pageSize: 10,
            reservationId: 2);

        // Assert
        Assert.False(resultForBooked.CanReview);
        Assert.True(resultForPaid.CanReview);
    }

    [Fact]
    public async Task GetRoomReviewPageAsync_ThrowsKeyNotFoundException_WhenRoomDoesNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var service = new ReviewService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetRoomReviewPageAsync(
                roomId: 999,
                userId: 20,
                page: 1,
                pageSize: 10,
                reservationId: null));

        Assert.Equal("スタジオが見つかりません。", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_CreatesReview_WhenUserHasPaidReservation()
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

        var service = new ReviewService(context);

        var request = new CreateReviewRequest
        {
            Score = 5,
            Content = "  とても良かったです。  ",
            ReservationId = null
        };

        // Act
        await service.CreateAsync(
            roomId: 1,
            userId: 20,
            request: request);

        // Assert
        var review = await context.Reviews.SingleAsync();

        Assert.Equal(1, review.RoomId);
        Assert.Equal(20, review.UserId);
        Assert.Equal(5, review.Score);
        Assert.Equal("とても良かったです。", review.Content);
        Assert.True(review.PublicVisible);
        Assert.Null(review.HiddenReason);
        Assert.Null(review.HostReply);
        Assert.Null(review.HostReplyAt);
    }

    [Fact]
    public async Task CreateAsync_CreatesReview_WhenSpecifiedReservationIsPaid()
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

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            userId: 20,
            status: "paid");

        var service = new ReviewService(context);

        var request = new CreateReviewRequest
        {
            Score = 4,
            Content = "良かったです。",
            ReservationId = 2
        };

        // Act
        await service.CreateAsync(
            roomId: 1,
            userId: 20,
            request: request);

        // Assert
        var review = await context.Reviews.SingleAsync();

        Assert.Equal(4, review.Score);
        Assert.Equal("良かったです。", review.Content);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task CreateAsync_ThrowsInvalidOperationException_WhenScoreIsInvalid(int score)
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var service = new ReviewService(context);

        var request = new CreateReviewRequest
        {
            Score = score,
            Content = "レビュー本文"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(
                roomId: 1,
                userId: 20,
                request: request));

        Assert.Equal("評価は1〜5で指定してください。", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsInvalidOperationException_WhenContentIsEmpty()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var service = new ReviewService(context);

        var request = new CreateReviewRequest
        {
            Score = 5,
            Content = "   "
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(
                roomId: 1,
                userId: 20,
                request: request));

        Assert.Equal("コメントを入力してください。", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsInvalidOperationException_WhenContentIsTooLong()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var service = new ReviewService(context);

        var request = new CreateReviewRequest
        {
            Score = 5,
            Content = new string('あ', 1001)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(
                roomId: 1,
                userId: 20,
                request: request));

        Assert.Equal("コメントは1000文字以内で入力してください。", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsKeyNotFoundException_WhenRoomDoesNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var service = new ReviewService(context);

        var request = new CreateReviewRequest
        {
            Score = 5,
            Content = "レビュー本文"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateAsync(
                roomId: 999,
                userId: 20,
                request: request));

        Assert.Equal("スタジオが見つかりません。", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsInvalidOperationException_WhenAlreadyReviewed()
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

        var service = new ReviewService(context);

        var request = new CreateReviewRequest
        {
            Score = 4,
            Content = "再投稿"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(
                roomId: 1,
                userId: 20,
                request: request));

        Assert.Equal("このスタジオへのレビューは投稿済みです。", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsInvalidOperationException_WhenNoPaidReservation()
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

        var service = new ReviewService(context);

        var request = new CreateReviewRequest
        {
            Score = 5,
            Content = "レビュー本文"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(
                roomId: 1,
                userId: 20,
                request: request));

        Assert.Equal("決済済みの予約がある場合のみレビューを投稿できます。", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsInvalidOperationException_WhenSpecifiedReservationIsNotPaid()
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

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            userId: 20,
            status: "paid");

        var service = new ReviewService(context);

        var request = new CreateReviewRequest
        {
            Score = 5,
            Content = "レビュー本文",
            ReservationId = 1
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(
                roomId: 1,
                userId: 20,
                request: request));

        Assert.Equal("決済済みの予約がある場合のみレビューを投稿できます。", ex.Message);
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