using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Services;

public class RoomDetailServiceTests
{
    [Fact]
    public async Task GetDetailAsync_ReturnsRoomDetail_WhenRoomExists()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", "ダンス練習向け", 3000);

        await SeedBusinessHourAsync(context, 1, dayOfWeek: 1, new TimeOnly(9, 0), new TimeOnly(18, 0), isHoliday: false);
        await SeedBusinessHourAsync(context, 1, dayOfWeek: 2, null, null, isHoliday: true);

        await SeedPriceRuleAsync(
            context,
            ruleId: 1,
            roomId: 1,
            weekday: null,
            startHour: new TimeOnly(18, 0),
            endHour: new TimeOnly(21, 0),
            multiplier: 1.5m,
            note: "夜間料金");

        var service = new RoomDetailService(context);

        // Act
        var result = await service.GetDetailAsync(1);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(1, result.Id);
        Assert.Equal("新宿スタジオ", result.Name);
        Assert.Equal("room01.jpg", result.ImageName);
        Assert.Equal("ダンス練習向け", result.Description);
        Assert.Equal(3000, result.Price);
        Assert.Equal(5, result.Capacity);
        Assert.Equal("100-0001", result.PostalCode);
        Assert.Equal("東京都テスト区サンプル1", result.Address);
        Assert.Equal("ホストA", result.HostName);

        Assert.Equal(2, result.BusinessHours.Count);

        Assert.Equal(1, result.BusinessHours[0].DayOfWeek);
        Assert.Equal("09:00", result.BusinessHours[0].StartTime);
        Assert.Equal("18:00", result.BusinessHours[0].EndTime);
        Assert.False(result.BusinessHours[0].IsHoliday);

        Assert.Equal(2, result.BusinessHours[1].DayOfWeek);
        Assert.Null(result.BusinessHours[1].StartTime);
        Assert.Null(result.BusinessHours[1].EndTime);
        Assert.True(result.BusinessHours[1].IsHoliday);

        var priceRule = Assert.Single(result.PriceRules);
        Assert.Equal(1, priceRule.Id);
        Assert.Equal("multiplier", priceRule.RuleType);
        Assert.Null(priceRule.Weekday);
        Assert.Equal("18:00", priceRule.StartHour);
        Assert.Equal("21:00", priceRule.EndHour);
        Assert.Equal(1.5m, priceRule.Multiplier);
        Assert.Equal("夜間料金", priceRule.Note);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsNull_WhenRoomDoesNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var service = new RoomDetailService(context);

        // Act
        var result = await service.GetDetailAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsPublicReviewsOnlyAndAverageScore()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "公開ユーザーA");
        await SeedGuestUserAsync(context, 21, "公開ユーザーB");
        await SeedGuestUserAsync(context, 22, "非公開ユーザー");

        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", "説明", 3000);

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 5,
            content: "良かったです。",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0));

        await SeedReviewAsync(
            context,
            reviewId: 2,
            roomId: 1,
            userId: 21,
            score: 3,
            content: "普通でした。",
            publicVisible: true,
            createdAtUtc: new DateTime(2026, 5, 2, 10, 0, 0));

        await SeedReviewAsync(
            context,
            reviewId: 3,
            roomId: 1,
            userId: 22,
            score: 1,
            content: "非公開レビュー",
            publicVisible: false,
            createdAtUtc: new DateTime(2026, 5, 3, 10, 0, 0));

        var service = new RoomDetailService(context);

        // Act
        var result = await service.GetDetailAsync(1);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(4.0, result.AverageScore);
        Assert.Equal(2, result.ReviewCount);

        Assert.Equal(2, result.Reviews.Count);

        // CreatedAtUtc desc
        Assert.Equal(2, result.Reviews[0].Id);
        Assert.Equal(3, result.Reviews[0].Score);
        Assert.Equal("普通でした。", result.Reviews[0].Content);
        Assert.Equal("公開ユーザーB", result.Reviews[0].UserName);

        Assert.Equal(1, result.Reviews[1].Id);
        Assert.Equal(5, result.Reviews[1].Score);
        Assert.Equal("良かったです。", result.Reviews[1].Content);
        Assert.Equal("公開ユーザーA", result.Reviews[1].UserName);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsOnlyLatestFivePublicReviews()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", "説明", 3000);

        for (var i = 1; i <= 6; i++)
        {
            await SeedGuestUserAsync(context, 100 + i, $"ユーザー{i}");

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

        var service = new RoomDetailService(context);

        // Act
        var result = await service.GetDetailAsync(1);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(5, result.Reviews.Count);

        Assert.Equal(6, result.Reviews[0].Id);
        Assert.Equal(5, result.Reviews[1].Id);
        Assert.Equal(4, result.Reviews[2].Id);
        Assert.Equal(3, result.Reviews[3].Id);
        Assert.Equal(2, result.Reviews[4].Id);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsHiddenHostReplies_WhenHiddenReviewHasHostReply()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ユーザーA");
        await SeedGuestUserAsync(context, 21, "ユーザーB");

        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", "説明", 3000);

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 1,
            content: "非公開レビュー",
            publicVisible: false,
            createdAtUtc: new DateTime(2026, 5, 1, 10, 0, 0),
            hostReply: "ご意見ありがとうございます。",
            hostReplyAt: new DateTime(2026, 5, 2, 10, 0, 0));

        await SeedReviewAsync(
            context,
            reviewId: 2,
            roomId: 1,
            userId: 21,
            score: 1,
            content: "非公開返信なし",
            publicVisible: false,
            createdAtUtc: new DateTime(2026, 5, 3, 10, 0, 0),
            hostReply: null,
            hostReplyAt: null);

        var service = new RoomDetailService(context);

        // Act
        var result = await service.GetDetailAsync(1);

        // Assert
        Assert.NotNull(result);

        var reply = Assert.Single(result.HiddenHostReplies);

        Assert.Equal(1, reply.Id);
        Assert.Null(reply.Score);
        Assert.Equal("", reply.Content);
        Assert.Equal("", reply.UserName);
        Assert.Equal("ご意見ありがとうございます。", reply.HostReply);
        Assert.Equal(new DateTime(2026, 5, 2, 10, 0, 0), reply.HostReplyAt);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsCalendarEvents_ForOpenClosureAndReservation()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "予約ユーザー");

        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", "説明", 3000);

        var targetDate = DateTime.Today.AddDays(10).Date;
        var businessDayOfWeek = ToBusinessDayOfWeek(targetDate.DayOfWeek);

        await SeedBusinessHourAsync(
            context,
            roomId: 1,
            dayOfWeek: businessDayOfWeek,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(18, 0),
            isHoliday: false);

        await SeedClosureAsync(
            context,
            closureId: 1,
            roomId: 1,
            startAt: targetDate.AddHours(13),
            endAt: targetDate.AddHours(14),
            reason: "臨時休館");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            userId: 20,
            startAt: targetDate.AddHours(10),
            endAt: targetDate.AddHours(12),
            status: "paid");

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            userId: 20,
            startAt: targetDate.AddHours(15),
            endAt: targetDate.AddHours(16),
            status: "canceled");

        var service = new RoomDetailService(context);

        // Act
        var result = await service.GetDetailAsync(1);

        // Assert
        Assert.NotNull(result);

        Assert.Contains(result.CalendarEvents, x =>
            x.Id == $"open-{targetDate:yyyyMMdd}" &&
            x.Title == "営業時間" &&
            x.Start == targetDate.AddHours(9) &&
            x.End == targetDate.AddHours(18) &&
            x.Type == "open" &&
            x.AllDay == false);

        Assert.Contains(result.CalendarEvents, x =>
            x.Id == "closure-1" &&
            x.Title == "臨時休館" &&
            x.Start == targetDate.AddHours(13) &&
            x.End == targetDate.AddHours(14) &&
            x.Type == "closure");

        Assert.Contains(result.CalendarEvents, x =>
            x.Id == "reservation-1" &&
            x.Title == "予約済み" &&
            x.Start == targetDate.AddHours(10) &&
            x.End == targetDate.AddHours(12) &&
            x.Type == "reservation");

        Assert.DoesNotContain(result.CalendarEvents, x => x.Id == "reservation-2");
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsHolidayCalendarEvent_WhenBusinessHourIsHoliday()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", "説明", 3000);

        var targetDate = DateTime.Today.AddDays(10).Date;
        var businessDayOfWeek = ToBusinessDayOfWeek(targetDate.DayOfWeek);

        await SeedBusinessHourAsync(
            context,
            roomId: 1,
            dayOfWeek: businessDayOfWeek,
            startTime: null,
            endTime: null,
            isHoliday: true);

        var service = new RoomDetailService(context);

        // Act
        var result = await service.GetDetailAsync(1);

        // Assert
        Assert.NotNull(result);

        Assert.Contains(result.CalendarEvents, x =>
            x.Id == $"holiday-{targetDate:yyyyMMdd}" &&
            x.Title == "休業" &&
            x.Start == targetDate &&
            x.End == targetDate.AddDays(1) &&
            x.AllDay &&
            x.Type == "closure");
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
        string name,
        string description,
        int price)
    {
        context.Rooms.Add(new Room
        {
            Id = roomId,
            UserId = hostUserId,
            Name = name,
            ImageName = $"room{roomId:00}.jpg",
            Description = description,
            Price = price,
            Capacity = 5,
            PostalCode = "100-0001",
            Address = $"東京都テスト区サンプル{roomId}",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedBusinessHourAsync(
        AppDbContext context,
        int roomId,
        int dayOfWeek,
        TimeOnly? startTime,
        TimeOnly? endTime,
        bool isHoliday)
    {
        context.BusinessHours.Add(new BusinessHour
        {
            RoomId = roomId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            IsHoliday = isHoliday,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedPriceRuleAsync(
        AppDbContext context,
        int ruleId,
        int roomId,
        int? weekday,
        TimeOnly? startHour,
        TimeOnly? endHour,
        decimal? multiplier,
        string? note)
    {
        context.PriceRules.Add(new PriceRule
        {
            Id = ruleId,
            RoomId = roomId,
            RuleType = "multiplier",
            Weekday = weekday,
            StartHour = startHour,
            EndHour = endHour,
            Multiplier = multiplier,
            FlatFee = null,
            Note = note,
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
            HiddenReason = publicVisible ? null : "非公開理由",
            HostReply = hostReply,
            HostReplyAt = hostReplyAt,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedClosureAsync(
        AppDbContext context,
        int closureId,
        int roomId,
        DateTime startAt,
        DateTime endAt,
        string reason)
    {
        context.Closures.Add(new Closure
        {
            Id = closureId,
            RoomId = roomId,
            StartAt = startAt,
            EndAt = endAt,
            Reason = reason
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
        string status)
    {
        context.Reservations.Add(new Reservation
        {
            Id = reservationId,
            RoomId = roomId,
            UserId = userId,
            StartAt = startAt,
            EndAt = endAt,
            Amount = status == "paid" ? 6000 : 0,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private static int ToBusinessDayOfWeek(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => 1,
            DayOfWeek.Tuesday => 2,
            DayOfWeek.Wednesday => 3,
            DayOfWeek.Thursday => 4,
            DayOfWeek.Friday => 5,
            DayOfWeek.Saturday => 6,
            DayOfWeek.Sunday => 7,
            _ => 0
        };
    }
}