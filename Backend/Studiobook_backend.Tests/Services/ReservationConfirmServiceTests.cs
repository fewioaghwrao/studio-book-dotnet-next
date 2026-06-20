using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Reservations;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Services;

public class ReservationConfirmServiceTests
{
    [Fact]
    public async Task BuildConfirmAsync_ReturnsConfirm_WhenReservationIsValid()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedAdminSettingsAsync(context, "0.10", "0.10");
        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", 3000);
        await SeedBusinessHourAsync(
            context, 1, (int)DayOfWeek.Monday,
            new TimeOnly(9, 0), new TimeOnly(20, 0), false);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        var service = CreateService(context);
        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = startAt,
            EndAt = endAt
        };

        var result = await service.BuildConfirmAsync(20, request);

        Assert.Equal(1, result.RoomId);
        Assert.Equal("新宿スタジオ", result.RoomName);
        Assert.Equal(startAt, result.StartAt);
        Assert.Equal(endAt, result.EndAt);

        Assert.Equal(3000, result.HourlyPrice);
        Assert.Equal(2m, result.Hours);

        Assert.Equal(6000, result.Subtotal);
        Assert.Equal(10m, result.TaxRatePercent);
        Assert.Equal(600, result.Tax);
        Assert.Equal(10m, result.PlatformFeeRatePercent);
        Assert.Equal(600, result.PlatformFee);
        Assert.Equal(7200, result.Amount);

        var item = Assert.Single(result.Items);
        Assert.Equal("通常料金", item.Label);
        Assert.Equal(6000, item.Amount);
        Assert.Equal(startAt, item.SliceStart);
        Assert.Equal(endAt, item.SliceEnd);
        Assert.Equal(3000, item.UnitRatePerHour);
    }

    [Fact]
    public async Task BuildConfirmAsync_AppliesMultiplierPriceRuleAndTaxFee()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedAdminSettingsAsync(context, "0.10", "0.10");
        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", 3000);
        await SeedBusinessHourAsync(
            context, 1, (int)DayOfWeek.Monday,
            new TimeOnly(9, 0), new TimeOnly(21, 0), false);
        await SeedPriceRuleAsync(
            context, 1, 1, (int)DayOfWeek.Monday,
            new TimeOnly(18, 0), new TimeOnly(21, 0), 1.5m);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 17);
        var endAt = startAt.AddHours(2);

        var service = CreateService(context);
        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = startAt,
            EndAt = endAt
        };

        var result = await service.BuildConfirmAsync(20, request);

        Assert.Equal(7500, result.Subtotal);
        Assert.Equal(750, result.Tax);
        Assert.Equal(750, result.PlatformFee);
        Assert.Equal(9000, result.Amount);

        Assert.Equal(2, result.Items.Count);

        Assert.Equal("通常料金", result.Items[0].Label);
        Assert.Equal(3000, result.Items[0].Amount);
        Assert.Equal(3000, result.Items[0].UnitRatePerHour);
        Assert.Equal(startAt, result.Items[0].SliceStart);
        Assert.Equal(startAt.AddHours(1), result.Items[0].SliceEnd);

        Assert.Equal("料金ルール適用", result.Items[1].Label);
        Assert.Equal(4500, result.Items[1].Amount);
        Assert.Equal(4500, result.Items[1].UnitRatePerHour);
        Assert.Equal(startAt.AddHours(1), result.Items[1].SliceStart);
        Assert.Equal(endAt, result.Items[1].SliceEnd);
    }

    [Fact]
    public async Task BuildConfirmAsync_UsesDefaultAdminSettings_WhenSettingsDoNotExist()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", 3000);
        await SeedBusinessHourAsync(
            context, 1, (int)DayOfWeek.Monday,
            new TimeOnly(9, 0), new TimeOnly(20, 0), false);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        var service = CreateService(context);
        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = startAt,
            EndAt = endAt
        };

        var result = await service.BuildConfirmAsync(20, request);

        Assert.Equal(6000, result.Subtotal);
        Assert.Equal(10m, result.TaxRatePercent);
        Assert.Equal(600, result.Tax);
        Assert.Equal(15m, result.PlatformFeeRatePercent);
        Assert.Equal(900, result.PlatformFee);
        Assert.Equal(7500, result.Amount);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsKeyNotFoundException_WhenRoomDoesNotExist()
    {
        await using var context = TestDbContextFactory.Create();

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        var service = CreateService(context);
        var request = new ReservationConfirmRequest
        {
            RoomId = 999,
            StartAt = startAt,
            EndAt = endAt
        };

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.BuildConfirmAsync(20, request));

        Assert.Equal("スタジオが見つかりません。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenStartAtIsAfterEndAt()
    {
        await using var context = TestDbContextFactory.Create();

        var baseAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);

        var service = CreateService(context);
        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = baseAt.AddHours(2),
            EndAt = baseAt
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(20, request));

        Assert.Equal("終了日時は開始日時より後にしてください。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenStartAtEqualsEndAt()
    {
        await using var context = TestDbContextFactory.Create();

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);

        var service = CreateService(context);
        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = startAt,
            EndAt = startAt
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(20, request));

        Assert.Equal("終了日時は開始日時より後にしてください。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenReservationCrossesDay()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", 3000);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 23);
        var endAt = startAt.Date.AddDays(1).AddHours(1);

        var service = CreateService(context);
        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = startAt,
            EndAt = endAt
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(20, request));

        Assert.Equal("日をまたぐ予約はできません。1日ごとに予約してください。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenDateIsHoliday()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", 3000);
        await SeedBusinessHourAsync(
            context, 1, (int)DayOfWeek.Monday,
            new TimeOnly(9, 0), new TimeOnly(20, 0), true);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        var service = CreateService(context);
        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = startAt,
            EndAt = endAt
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(20, request));

        Assert.Equal("指定日は営業時間外です。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenBusinessHourDoesNotExist()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", 3000);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        var service = CreateService(context);
        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = startAt,
            EndAt = endAt
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(20, request));

        Assert.Equal("指定日は営業時間外です。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenBusinessHourTimeIsNull()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", 3000);
        await SeedBusinessHourAsync(
            context, 1, (int)DayOfWeek.Monday,
            null, null, false);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        var service = CreateService(context);
        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = startAt,
            EndAt = endAt
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(20, request));

        Assert.Equal("指定日の営業時間が設定されていません。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenTimeIsOutsideBusinessHour()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", 3000);
        await SeedBusinessHourAsync(
            context, 1, (int)DayOfWeek.Monday,
            new TimeOnly(9, 0), new TimeOnly(18, 0), false);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 8);
        var endAt = startAt.AddHours(2);

        var service = CreateService(context);
        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = startAt,
            EndAt = endAt
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(20, request));

        Assert.Equal("営業時間外の時間が含まれています。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenOverlapsClosure()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", 3000);
        await SeedBusinessHourAsync(
            context, 1, (int)DayOfWeek.Monday,
            new TimeOnly(9, 0), new TimeOnly(20, 0), false);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        await SeedClosureAsync(
            context, 1, 1,
            startAt.AddHours(1),
            startAt.AddHours(3));

        var service = CreateService(context);
        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = startAt,
            EndAt = endAt
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(20, request));

        Assert.Equal("指定した時間帯は休業期間と重複しています。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenOverlapsExistingReservation()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedGuestUserAsync(context, 21, "ゲストB");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", 3000);
        await SeedBusinessHourAsync(
            context, 1, (int)DayOfWeek.Monday,
            new TimeOnly(9, 0), new TimeOnly(20, 0), false);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        await SeedReservationAsync(
            context, 1, 1, 21,
            startAt.AddHours(1),
            startAt.AddHours(3),
            "paid", 6000);

        var service = CreateService(context);
        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = startAt,
            EndAt = endAt
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(20, request));

        Assert.Equal("指定した時間帯は既に予約されています。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_AllowsOverlapWithCanceledReservation()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedAdminSettingsAsync(context, "0.10", "0.10");
        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ゲストA");
        await SeedGuestUserAsync(context, 21, "ゲストB");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", 3000);
        await SeedBusinessHourAsync(
            context, 1, (int)DayOfWeek.Monday,
            new TimeOnly(9, 0), new TimeOnly(20, 0), false);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        await SeedReservationAsync(
            context, 1, 1, 21,
            startAt.AddHours(1),
            startAt.AddHours(3),
            "canceled", 6000);

        var service = CreateService(context);
        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = startAt,
            EndAt = endAt
        };

        var result = await service.BuildConfirmAsync(20, request);

        Assert.Equal(7200, result.Amount);
        Assert.Equal(6000, result.Subtotal);
    }

    private static DateTime GetNextDayOfWeek(
        DayOfWeek targetDay,
        int hour,
        int minute = 0)
    {
        var date = DateTime.UtcNow.Date.AddDays(1);

        while (date.DayOfWeek != targetDay)
        {
            date = date.AddDays(1);
        }

        return date.AddHours(hour).AddMinutes(minute);
    }

    private static ReservationConfirmService CreateService(AppDbContext context)
    {
        var adminSettingsService = new AdminSettingsService(context);
        return new ReservationConfirmService(context, adminSettingsService);
    }

    private static async Task SeedAdminSettingsAsync(
        AppDbContext context,
        string taxRate,
        string adminFeeRate)
    {
        context.AppSettings.AddRange(
            new AppSetting
            {
                Key = "tax_rate",
                Value = taxRate,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new AppSetting
            {
                Key = "admin_fee_rate",
                Value = adminFeeRate,
                UpdatedAtUtc = DateTime.UtcNow
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
        int price)
    {
        context.Rooms.Add(new Room
        {
            Id = roomId,
            UserId = hostUserId,
            Name = name,
            ImageName = $"room{roomId:00}.jpg",
            Description = "テスト用スタジオ",
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
        int roomId,
        int ruleId,
        int? weekday,
        TimeOnly? startHour,
        TimeOnly? endHour,
        decimal multiplier)
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
            Note = "テスト料金ルール",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedClosureAsync(
        AppDbContext context,
        int closureId,
        int roomId,
        DateTime startAt,
        DateTime endAt)
    {
        context.Closures.Add(new Closure
        {
            Id = closureId,
            RoomId = roomId,
            StartAt = startAt,
            EndAt = endAt,
            Reason = "テスト休業"
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
