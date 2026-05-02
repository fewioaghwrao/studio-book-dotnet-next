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
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedAdminSettingsAsync(
            context,
            taxRate: "0.10",
            adminFeeRate: "0.10");

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(
            context,
            roomId: 1,
            hostUserId: 10,
            name: "新宿スタジオ",
            price: 3000);

        await SeedBusinessHourAsync(
            context,
            roomId: 1,
            dayOfWeek: 1,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(20, 0),
            isHoliday: false);

        var service = CreateService(context);

        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = new DateTime(2026, 6, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 6, 1, 12, 0, 0)
        };

        // Act
        var result = await service.BuildConfirmAsync(
            userId: 20,
            request: request);

        // Assert
        Assert.Equal(1, result.RoomId);
        Assert.Equal("新宿スタジオ", result.RoomName);
        Assert.Equal(new DateTime(2026, 6, 1, 10, 0, 0), result.StartAt);
        Assert.Equal(new DateTime(2026, 6, 1, 12, 0, 0), result.EndAt);

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
        Assert.Equal(new DateTime(2026, 6, 1, 10, 0, 0), item.SliceStart);
        Assert.Equal(new DateTime(2026, 6, 1, 12, 0, 0), item.SliceEnd);
        Assert.Equal(3000, item.UnitRatePerHour);
    }

    [Fact]
    public async Task BuildConfirmAsync_AppliesMultiplierPriceRuleAndTaxFee()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedAdminSettingsAsync(
            context,
            taxRate: "0.10",
            adminFeeRate: "0.10");

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(
            context,
            roomId: 1,
            hostUserId: 10,
            name: "新宿スタジオ",
            price: 3000);

        await SeedBusinessHourAsync(
            context,
            roomId: 1,
            dayOfWeek: 1,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(21, 0),
            isHoliday: false);

        await SeedPriceRuleAsync(
            context,
            roomId: 1,
            ruleId: 1,
            weekday: 1,
            startHour: new TimeOnly(18, 0),
            endHour: new TimeOnly(21, 0),
            multiplier: 1.5m);

        var service = CreateService(context);

        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = new DateTime(2026, 6, 1, 17, 0, 0),
            EndAt = new DateTime(2026, 6, 1, 19, 0, 0)
        };

        // Act
        var result = await service.BuildConfirmAsync(
            userId: 20,
            request: request);

        // Assert
        Assert.Equal(7500, result.Subtotal);
        Assert.Equal(750, result.Tax);
        Assert.Equal(750, result.PlatformFee);
        Assert.Equal(9000, result.Amount);

        Assert.Equal(2, result.Items.Count);

        Assert.Equal("通常料金", result.Items[0].Label);
        Assert.Equal(3000, result.Items[0].Amount);
        Assert.Equal(3000, result.Items[0].UnitRatePerHour);
        Assert.Equal(new DateTime(2026, 6, 1, 17, 0, 0), result.Items[0].SliceStart);
        Assert.Equal(new DateTime(2026, 6, 1, 18, 0, 0), result.Items[0].SliceEnd);

        Assert.Equal("料金ルール適用", result.Items[1].Label);
        Assert.Equal(4500, result.Items[1].Amount);
        Assert.Equal(4500, result.Items[1].UnitRatePerHour);
        Assert.Equal(new DateTime(2026, 6, 1, 18, 0, 0), result.Items[1].SliceStart);
        Assert.Equal(new DateTime(2026, 6, 1, 19, 0, 0), result.Items[1].SliceEnd);
    }

    [Fact]
    public async Task BuildConfirmAsync_UsesDefaultAdminSettings_WhenSettingsDoNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(
            context,
            roomId: 1,
            hostUserId: 10,
            name: "新宿スタジオ",
            price: 3000);

        await SeedBusinessHourAsync(
            context,
            roomId: 1,
            dayOfWeek: 1,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(20, 0),
            isHoliday: false);

        var service = CreateService(context);

        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = new DateTime(2026, 6, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 6, 1, 12, 0, 0)
        };

        // Act
        var result = await service.BuildConfirmAsync(
            userId: 20,
            request: request);

        // Assert
        Assert.Equal(6000, result.Subtotal);

        // AdminSettingsService のデフォルト値
        // tax_rate = 0.10, admin_fee_rate = 0.15
        Assert.Equal(10m, result.TaxRatePercent);
        Assert.Equal(600, result.Tax);
        Assert.Equal(15m, result.PlatformFeeRatePercent);
        Assert.Equal(900, result.PlatformFee);
        Assert.Equal(7500, result.Amount);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsKeyNotFoundException_WhenRoomDoesNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var service = CreateService(context);

        var request = new ReservationConfirmRequest
        {
            RoomId = 999,
            StartAt = new DateTime(2026, 6, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 6, 1, 12, 0, 0)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.BuildConfirmAsync(
                userId: 20,
                request: request));

        Assert.Equal("スタジオが見つかりません。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenStartAtIsAfterEndAt()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var service = CreateService(context);

        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = new DateTime(2026, 6, 1, 12, 0, 0),
            EndAt = new DateTime(2026, 6, 1, 10, 0, 0)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(
                userId: 20,
                request: request));

        Assert.Equal("終了日時は開始日時より後にしてください。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenStartAtEqualsEndAt()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var service = CreateService(context);

        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = new DateTime(2026, 6, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 6, 1, 10, 0, 0)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(
                userId: 20,
                request: request));

        Assert.Equal("終了日時は開始日時より後にしてください。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenReservationCrossesDay()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedRoomAsync(
            context,
            roomId: 1,
            hostUserId: 10,
            name: "新宿スタジオ",
            price: 3000);

        var service = CreateService(context);

        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = new DateTime(2026, 6, 1, 23, 0, 0),
            EndAt = new DateTime(2026, 6, 2, 1, 0, 0)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(
                userId: 20,
                request: request));

        Assert.Equal("日をまたぐ予約はできません。1日ごとに予約してください。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenDateIsHoliday()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedRoomAsync(
            context,
            roomId: 1,
            hostUserId: 10,
            name: "新宿スタジオ",
            price: 3000);

        await SeedBusinessHourAsync(
            context,
            roomId: 1,
            dayOfWeek: 1,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(20, 0),
            isHoliday: true);

        var service = CreateService(context);

        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = new DateTime(2026, 6, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 6, 1, 12, 0, 0)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(
                userId: 20,
                request: request));

        Assert.Equal("指定日は営業時間外です。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenBusinessHourDoesNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedRoomAsync(
            context,
            roomId: 1,
            hostUserId: 10,
            name: "新宿スタジオ",
            price: 3000);

        var service = CreateService(context);

        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = new DateTime(2026, 6, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 6, 1, 12, 0, 0)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(
                userId: 20,
                request: request));

        Assert.Equal("指定日は営業時間外です。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenBusinessHourTimeIsNull()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedRoomAsync(
            context,
            roomId: 1,
            hostUserId: 10,
            name: "新宿スタジオ",
            price: 3000);

        await SeedBusinessHourAsync(
            context,
            roomId: 1,
            dayOfWeek: 1,
            startTime: null,
            endTime: null,
            isHoliday: false);

        var service = CreateService(context);

        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = new DateTime(2026, 6, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 6, 1, 12, 0, 0)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(
                userId: 20,
                request: request));

        Assert.Equal("指定日の営業時間が設定されていません。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenTimeIsOutsideBusinessHour()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedRoomAsync(
            context,
            roomId: 1,
            hostUserId: 10,
            name: "新宿スタジオ",
            price: 3000);

        await SeedBusinessHourAsync(
            context,
            roomId: 1,
            dayOfWeek: 1,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(18, 0),
            isHoliday: false);

        var service = CreateService(context);

        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = new DateTime(2026, 6, 1, 8, 0, 0),
            EndAt = new DateTime(2026, 6, 1, 10, 0, 0)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(
                userId: 20,
                request: request));

        Assert.Equal("営業時間外の時間が含まれています。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenOverlapsClosure()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedRoomAsync(
            context,
            roomId: 1,
            hostUserId: 10,
            name: "新宿スタジオ",
            price: 3000);

        await SeedBusinessHourAsync(
            context,
            roomId: 1,
            dayOfWeek: 1,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(20, 0),
            isHoliday: false);

        await SeedClosureAsync(
            context,
            closureId: 1,
            roomId: 1,
            startAt: new DateTime(2026, 6, 1, 11, 0, 0),
            endAt: new DateTime(2026, 6, 1, 13, 0, 0));

        var service = CreateService(context);

        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = new DateTime(2026, 6, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 6, 1, 12, 0, 0)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(
                userId: 20,
                request: request));

        Assert.Equal("指定した時間帯は休業期間と重複しています。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_ThrowsInvalidOperationException_WhenOverlapsExistingReservation()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");
        await SeedGuestUserAsync(context, userId: 21, name: "ゲストB");

        await SeedRoomAsync(
            context,
            roomId: 1,
            hostUserId: 10,
            name: "新宿スタジオ",
            price: 3000);

        await SeedBusinessHourAsync(
            context,
            roomId: 1,
            dayOfWeek: 1,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(20, 0),
            isHoliday: false);

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            userId: 21,
            startAt: new DateTime(2026, 6, 1, 11, 0, 0),
            endAt: new DateTime(2026, 6, 1, 13, 0, 0),
            status: "paid",
            amount: 6000);

        var service = CreateService(context);

        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = new DateTime(2026, 6, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 6, 1, 12, 0, 0)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuildConfirmAsync(
                userId: 20,
                request: request));

        Assert.Equal("指定した時間帯は既に予約されています。", ex.Message);
    }

    [Fact]
    public async Task BuildConfirmAsync_AllowsOverlapWithCanceledReservation()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedAdminSettingsAsync(
            context,
            taxRate: "0.10",
            adminFeeRate: "0.10");

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");
        await SeedGuestUserAsync(context, userId: 21, name: "ゲストB");

        await SeedRoomAsync(
            context,
            roomId: 1,
            hostUserId: 10,
            name: "新宿スタジオ",
            price: 3000);

        await SeedBusinessHourAsync(
            context,
            roomId: 1,
            dayOfWeek: 1,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(20, 0),
            isHoliday: false);

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            userId: 21,
            startAt: new DateTime(2026, 6, 1, 11, 0, 0),
            endAt: new DateTime(2026, 6, 1, 13, 0, 0),
            status: "canceled",
            amount: 6000);

        var service = CreateService(context);

        var request = new ReservationConfirmRequest
        {
            RoomId = 1,
            StartAt = new DateTime(2026, 6, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 6, 1, 12, 0, 0)
        };

        // Act
        var result = await service.BuildConfirmAsync(
            userId: 20,
            request: request);

        // Assert
        Assert.Equal(7200, result.Amount);
        Assert.Equal(6000, result.Subtotal);
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