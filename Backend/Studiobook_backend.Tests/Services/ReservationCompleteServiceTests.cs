using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Services;

public class ReservationCompleteServiceTests
{
    [Fact]
    public async Task CompleteFromStripeAsync_CreatesPaidReservationAndChargeItems_WhenMetadataIsValid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var service = CreateService(context);

        var metadata = CreateValidMetadata(
            amount: "7200",
            startAt: "2026-06-01T10:00",
            endAt: "2026-06-01T12:00");

        // Act
        await service.CompleteFromStripeAsync(
            metadata,
            paymentIntentId: "pi_test_001",
            checkoutSessionId: "cs_test_001",
            paidAmount: 7200);

        // Assert
        var reservation = await context.Reservations
            .Include(x => x.ChargeItems)
            .SingleAsync();

        Assert.Equal(1, reservation.RoomId);
        Assert.Equal(20, reservation.UserId);
        Assert.Equal(new DateTime(2026, 6, 1, 10, 0, 0), reservation.StartAt);
        Assert.Equal(new DateTime(2026, 6, 1, 12, 0, 0), reservation.EndAt);
        Assert.Equal(7200, reservation.Amount);
        Assert.Equal("paid", reservation.Status);

        Assert.Equal(3, reservation.ChargeItems.Count);

        Assert.Contains(reservation.ChargeItems, x =>
            x.Kind == "base" &&
            x.Description == "通常料金" &&
            x.SliceAmount == 6000 &&
            x.UnitRatePerHour == 3000 &&
            x.SliceStart == new DateTime(2026, 6, 1, 10, 0, 0) &&
            x.SliceEnd == new DateTime(2026, 6, 1, 12, 0, 0));

        Assert.Contains(reservation.ChargeItems, x =>
            x.Kind == "tax" &&
            x.Description == "消費税（10%）" &&
            x.SliceAmount == 600);

        Assert.Contains(reservation.ChargeItems, x =>
            x.Kind == "platform_fee" &&
            x.Description == "プラットフォーム使用料（10%）" &&
            x.SliceAmount == 600);
    }

    [Fact]
    public async Task CompleteFromStripeAsync_DoesNotCreateDuplicateReservation_WhenSameReservationAlreadyExists()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var service = CreateService(context);

        var metadata = CreateValidMetadata(
            amount: "7200",
            startAt: "2026-06-01T10:00",
            endAt: "2026-06-01T12:00");

        await service.CompleteFromStripeAsync(
            metadata,
            paymentIntentId: "pi_test_001",
            checkoutSessionId: "cs_test_001",
            paidAmount: 7200);

        // Act
        await service.CompleteFromStripeAsync(
            metadata,
            paymentIntentId: "pi_test_001",
            checkoutSessionId: "cs_test_001",
            paidAmount: 7200);

        // Assert
        Assert.Equal(1, await context.Reservations.CountAsync());
        Assert.Equal(3, await context.ReservationChargeItems.CountAsync());
    }

    [Fact]
    public async Task CompleteFromStripeAsync_ThrowsInvalidOperationException_WhenPaidAmountDoesNotMatchMetadataAmount()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var service = CreateService(context);

        var metadata = CreateValidMetadata(
            amount: "7200",
            startAt: "2026-06-01T10:00",
            endAt: "2026-06-01T12:00");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteFromStripeAsync(
                metadata,
                paymentIntentId: "pi_test_001",
                checkoutSessionId: "cs_test_001",
                paidAmount: 9999));

        Assert.Contains("Stripeの支払金額と予約金額が一致しません。", ex.Message);
        Assert.Empty(context.Reservations);
    }

    [Fact]
    public async Task CompleteFromStripeAsync_ThrowsInvalidOperationException_WhenMetadataRequiredKeyIsMissing()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var service = CreateService(context);

        var metadata = CreateValidMetadata(
            amount: "7200",
            startAt: "2026-06-01T10:00",
            endAt: "2026-06-01T12:00");

        metadata.Remove("roomId");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteFromStripeAsync(
                metadata,
                paymentIntentId: "pi_test_001",
                checkoutSessionId: "cs_test_001",
                paidAmount: 7200));

        Assert.Equal("Stripe metadata に roomId がありません。", ex.Message);
        Assert.Empty(context.Reservations);
    }

    [Fact]
    public async Task CompleteFromStripeAsync_ThrowsInvalidOperationException_WhenMetadataIntValueIsInvalid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var service = CreateService(context);

        var metadata = CreateValidMetadata(
            amount: "7200",
            startAt: "2026-06-01T10:00",
            endAt: "2026-06-01T12:00");

        metadata["roomId"] = "not-number";

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteFromStripeAsync(
                metadata,
                paymentIntentId: "pi_test_001",
                checkoutSessionId: "cs_test_001",
                paidAmount: 7200));

        Assert.Equal("Stripe metadata の roomId が数値ではありません。value=not-number", ex.Message);
        Assert.Empty(context.Reservations);
    }

    [Fact]
    public async Task CompleteFromStripeAsync_ThrowsInvalidOperationException_WhenMetadataDateTimeValueIsInvalid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var service = CreateService(context);

        var metadata = CreateValidMetadata(
            amount: "7200",
            startAt: "invalid-date",
            endAt: "2026-06-01T12:00");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteFromStripeAsync(
                metadata,
                paymentIntentId: "pi_test_001",
                checkoutSessionId: "cs_test_001",
                paidAmount: 7200));

        Assert.Equal("Stripe metadata の startAt が日時ではありません。value=invalid-date", ex.Message);
        Assert.Empty(context.Reservations);
    }

    [Fact]
    public async Task CompleteFromStripeAsync_ThrowsInvalidOperationException_WhenRecalculatedAmountDoesNotMatchMetadataAmount()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var service = CreateService(context);

        var metadata = CreateValidMetadata(
            amount: "9999",
            startAt: "2026-06-01T10:00",
            endAt: "2026-06-01T12:00");

        // paidAmount を null にして、先に paidAmount 不一致で落ちないようにする。
        // その後、ReservationConfirmService の再計算金額 7200 と metadata amount 9999 の不一致で落ちる。
        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteFromStripeAsync(
                metadata,
                paymentIntentId: "pi_test_001",
                checkoutSessionId: "cs_test_001",
                paidAmount: null));

        Assert.Contains("予約金額が再計算結果と一致しません。", ex.Message);
        Assert.Empty(context.Reservations);
    }

    [Fact]
    public async Task CompleteFromStripeAsync_AllowsExistingCanceledReservationAndCreatesNewReservation()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        context.Reservations.Add(new Reservation
        {
            Id = 1,
            RoomId = 1,
            UserId = 20,
            StartAt = new DateTime(2026, 6, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 6, 1, 12, 0, 0),
            Amount = 7200,
            Status = "canceled",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var metadata = CreateValidMetadata(
            amount: "7200",
            startAt: "2026-06-01T10:00",
            endAt: "2026-06-01T12:00");

        // Act
        await service.CompleteFromStripeAsync(
            metadata,
            paymentIntentId: "pi_test_001",
            checkoutSessionId: "cs_test_001",
            paidAmount: 7200);

        // Assert
        var reservations = await context.Reservations
            .OrderBy(x => x.Id)
            .ToListAsync();

        Assert.Equal(2, reservations.Count);
        Assert.Equal("canceled", reservations[0].Status);
        Assert.Equal("paid", reservations[1].Status);
    }

    private static ReservationCompleteService CreateService(AppDbContext context)
    {
        var adminSettingsService = new AdminSettingsService(context);

        var reservationConfirmService = new ReservationConfirmService(
            context,
            adminSettingsService);

        return new ReservationCompleteService(
            context,
            reservationConfirmService);
    }

    private static Dictionary<string, string> CreateValidMetadata(
        string amount,
        string startAt,
        string endAt)
    {
        return new Dictionary<string, string>
        {
            ["userId"] = "20",
            ["roomId"] = "1",
            ["startAt"] = startAt,
            ["endAt"] = endAt,
            ["subtotal"] = "6000",
            ["taxRatePercent"] = "10",
            ["tax"] = "600",
            ["platformFeeRatePercent"] = "10",
            ["platformFee"] = "600",
            ["amount"] = amount,
            ["hourlyPrice"] = "3000",
            ["hours"] = "2"
        };
    }

    private static async Task SeedBaseDataAsync(AppDbContext context)
    {
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

        // 2026/06/01 は月曜日
        await SeedBusinessHourAsync(
            context,
            roomId: 1,
            dayOfWeek: 1,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(20, 0),
            isHoliday: false);
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
}
