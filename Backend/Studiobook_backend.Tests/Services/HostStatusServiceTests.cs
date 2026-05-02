using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Services;

public class HostStatusServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsMonthlyAmountsAndReviewAverages()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");

        await SeedBusinessHoursForWeekdaysAsync(
            context,
            roomId: 1,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(18, 0));

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 3, 10, 10, 0, 0),
            endAt: new DateTime(2026, 3, 10, 12, 0, 0),
            status: "booked",
            amount: 5000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 4, 10, 10, 0, 0),
            endAt: new DateTime(2026, 4, 10, 12, 0, 0),
            status: "paid",
            amount: 6000);

        await SeedReservationAsync(
            context,
            reservationId: 3,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 10, 10, 0, 0),
            endAt: new DateTime(2026, 5, 10, 12, 0, 0),
            status: "booked",
            amount: 7000);

        await SeedReservationAsync(
            context,
            reservationId: 4,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 11, 10, 0, 0),
            endAt: new DateTime(2026, 5, 11, 13, 0, 0),
            status: "paid",
            amount: 8000);

        await SeedReviewAsync(
            context,
            reviewId: 1,
            roomId: 1,
            userId: 20,
            score: 5,
            publicVisible: true);

        await SeedReviewAsync(
            context,
            reviewId: 2,
            roomId: 1,
            userId: 20,
            score: 3,
            publicVisible: false);

        var service = new HostStatusService(context);

        // Act
        var result = await service.GetAsync(
            hostUserId: 10,
            roomId: null,
            baseMonth: new DateTime(2026, 5, 1));

        // Assert
        Assert.Equal(new List<string> { "2026-03", "2026-04", "2026-05" }, result.Labels);

        Assert.Equal(new List<int> { 5000, 0, 7000 }, result.Booked);
        Assert.Equal(new List<int> { 0, 6000, 8000 }, result.Paid);

        Assert.Equal(3, result.UtilizationPercents.Count);
        Assert.All(result.UtilizationPercents, x => Assert.NotNull(x));

        Assert.Equal(4.0, result.ReviewAvgAny);
        Assert.Equal(5.0, result.ReviewAvgPublic);

        Assert.Single(result.RoomOptions);
        Assert.Equal(1, result.RoomOptions[0].Id);
        Assert.Equal("新宿スタジオ", result.RoomOptions[0].Name);
    }

    [Fact]
    public async Task GetAsync_UsesOnlyLoginHostRooms()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedHostUserAsync(context, userId: 99, name: "ホストB");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "ホストAのスタジオ");
        await SeedRoomAsync(context, roomId: 2, hostUserId: 99, name: "ホストBのスタジオ");

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
            startAt: new DateTime(2026, 5, 10, 10, 0, 0),
            endAt: new DateTime(2026, 5, 10, 12, 0, 0),
            status: "paid",
            amount: 9999);

        await SeedReviewAsync(context, reviewId: 1, roomId: 1, userId: 20, score: 4, publicVisible: true);
        await SeedReviewAsync(context, reviewId: 2, roomId: 2, userId: 20, score: 1, publicVisible: true);

        var service = new HostStatusService(context);

        // Act
        var result = await service.GetAsync(
            hostUserId: 10,
            roomId: null,
            baseMonth: new DateTime(2026, 5, 1));

        // Assert
        Assert.Equal(new List<int> { 0, 0, 0 }, result.Booked);
        Assert.Equal(new List<int> { 0, 0, 6000 }, result.Paid);

        Assert.Equal(4.0, result.ReviewAvgAny);
        Assert.Equal(4.0, result.ReviewAvgPublic);

        Assert.Single(result.RoomOptions);
        Assert.Equal("ホストAのスタジオ", result.RoomOptions[0].Name);
    }

    [Fact]
    public async Task GetAsync_FiltersByRoomId()
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
            startAt: new DateTime(2026, 5, 10, 10, 0, 0),
            endAt: new DateTime(2026, 5, 10, 12, 0, 0),
            status: "paid",
            amount: 9000);

        await SeedReviewAsync(context, reviewId: 1, roomId: 1, userId: 20, score: 5, publicVisible: true);
        await SeedReviewAsync(context, reviewId: 2, roomId: 2, userId: 20, score: 2, publicVisible: true);

        var service = new HostStatusService(context);

        // Act
        var result = await service.GetAsync(
            hostUserId: 10,
            roomId: 2,
            baseMonth: new DateTime(2026, 5, 1));

        // Assert
        Assert.Equal(new List<int> { 0, 0, 0 }, result.Booked);
        Assert.Equal(new List<int> { 0, 0, 9000 }, result.Paid);

        Assert.Equal(2.0, result.ReviewAvgAny);
        Assert.Equal(2.0, result.ReviewAvgPublic);

        Assert.Equal(2, result.RoomOptions.Count);
        Assert.Contains(result.RoomOptions, x => x.Id == 1 && x.Name == "新宿スタジオ");
        Assert.Contains(result.RoomOptions, x => x.Id == 2 && x.Name == "渋谷スタジオ");
    }

    [Fact]
    public async Task GetAsync_ReturnsZeroValues_WhenTargetRoomDoesNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");

        var service = new HostStatusService(context);

        // Act
        var result = await service.GetAsync(
            hostUserId: 10,
            roomId: 999,
            baseMonth: new DateTime(2026, 5, 1));

        // Assert
        Assert.Equal(new List<string> { "2026-03", "2026-04", "2026-05" }, result.Labels);
        Assert.Equal(new List<int> { 0, 0, 0 }, result.Booked);
        Assert.Equal(new List<int> { 0, 0, 0 }, result.Paid);

        Assert.Equal(3, result.UtilizationPercents.Count);
        Assert.All(result.UtilizationPercents, Assert.Null);

        Assert.Null(result.ReviewAvgAny);
        Assert.Null(result.ReviewAvgPublic);

        Assert.Single(result.RoomOptions);
        Assert.Equal(1, result.RoomOptions[0].Id);
    }

    [Fact]
    public async Task GetAsync_ReturnsNullUtilization_WhenNoBusinessHours()
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
            startAt: new DateTime(2026, 5, 10, 10, 0, 0),
            endAt: new DateTime(2026, 5, 10, 12, 0, 0),
            status: "paid",
            amount: 6000);

        var service = new HostStatusService(context);

        // Act
        var result = await service.GetAsync(
            hostUserId: 10,
            roomId: null,
            baseMonth: new DateTime(2026, 5, 1));

        // Assert
        Assert.Equal(new List<int> { 0, 0, 0 }, result.Booked);
        Assert.Equal(new List<int> { 0, 0, 6000 }, result.Paid);

        Assert.Equal(3, result.UtilizationPercents.Count);
        Assert.All(result.UtilizationPercents, Assert.Null);
    }

    [Fact]
    public async Task GetAsync_CapsUtilizationAt100Percent()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");

        // 2026/05/01 は金曜。金曜だけ1時間営業にする。
        await SeedBusinessHourAsync(
            context,
            roomId: 1,
            dayOfWeek: 5,
            startTime: new TimeOnly(10, 0),
            endTime: new TimeOnly(11, 0),
            isHoliday: false);

        // 24時間予約にして、利用率が100%を超える状態にする。
        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 0, 0, 0),
            endAt: new DateTime(2026, 5, 2, 0, 0, 0),
            status: "paid",
            amount: 10000);

        var service = new HostStatusService(context);

        // Act
        var result = await service.GetAsync(
            hostUserId: 10,
            roomId: 1,
            baseMonth: new DateTime(2026, 5, 1));

        // Assert
        Assert.Equal(new List<string> { "2026-03", "2026-04", "2026-05" }, result.Labels);

        Assert.Equal(0m, result.UtilizationPercents[0]);
        Assert.Equal(0m, result.UtilizationPercents[1]);
        Assert.Equal(100m, result.UtilizationPercents[2]);
    }

    [Fact]
    public async Task GetAsync_CalculatesUtilizationPercent()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");

        // 2026/05/01 は金曜。金曜だけ10時間営業にする。
        await SeedBusinessHourAsync(
            context,
            roomId: 1,
            dayOfWeek: 5,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(19, 0),
            isHoliday: false);

        // 2時間予約 => 20%
        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "paid",
            amount: 6000);

        var service = new HostStatusService(context);

        // Act
        var result = await service.GetAsync(
            hostUserId: 10,
            roomId: 1,
            baseMonth: new DateTime(2026, 5, 1));

        // Assert
        Assert.Equal(0m, result.UtilizationPercents[0]);
        Assert.Equal(0m, result.UtilizationPercents[1]);
        Assert.Equal(4.0m, result.UtilizationPercents[2]);
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

    private static async Task SeedReviewAsync(
        AppDbContext context,
        int reviewId,
        int roomId,
        int userId,
        int score,
        bool publicVisible)
    {
        context.Reviews.Add(new Review
        {
            Id = reviewId,
            RoomId = roomId,
            UserId = userId,
            Score = score,
            Content = "テストレビュー",
            PublicVisible = publicVisible,
            HiddenReason = publicVisible ? null : "非公開理由",
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
            await SeedBusinessHourAsync(
                context,
                roomId,
                dayOfWeek,
                start,
                end,
                isHoliday: false);
        }
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
}