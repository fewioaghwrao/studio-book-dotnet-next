using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Studiobook_backend.Application.Dtos.Reservations;
using Studiobook_backend.Application.Common;
using Studiobook_backend.Infrastructure.Persistence;
using Studiobook_backend.Domain.Entities;
using Studiobook_backend.Application.Services;
using Studiobook_backend.Infrastructure.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Services;

public class ReservationCompleteServiceTests
{
    [Fact]
    public async Task CompleteFromStripeAsync_CreatesPaidReservationAndChargeItems_WhenMetadataIsValid()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        var service = CreateService(context);

        var metadata = CreateValidMetadata(
            amount: "7200",
            startAt: FormatMetadataDateTime(startAt),
            endAt: FormatMetadataDateTime(endAt));

        await service.CompleteFromStripeAsync(
            metadata,
            paymentIntentId: "pi_test_001",
            checkoutSessionId: "cs_test_001",
            paidAmount: 7200);

        var reservation = await context.Reservations
            .Include(x => x.ChargeItems)
            .SingleAsync();

        Assert.Equal(1, reservation.RoomId);
        Assert.Equal(20, reservation.UserId);
        Assert.Equal(startAt, reservation.StartAt);
        Assert.Equal(endAt, reservation.EndAt);
        Assert.Equal(7200, reservation.Amount);
        Assert.Equal("paid", reservation.Status);

        Assert.Equal(3, reservation.ChargeItems.Count);

        Assert.Contains(reservation.ChargeItems, x =>
            x.Kind == "base" &&
            x.Description == "通常料金" &&
            x.SliceAmount == 6000 &&
            x.UnitRatePerHour == 3000 &&
            x.SliceStart == startAt &&
            x.SliceEnd == endAt);

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
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        var service = CreateService(context);

        var metadata = CreateValidMetadata(
            amount: "7200",
            startAt: FormatMetadataDateTime(startAt),
            endAt: FormatMetadataDateTime(endAt));

        var first = await service.CompleteFromStripeAsync(
            metadata,
            paymentIntentId: "pi_test_001",
            checkoutSessionId: "cs_test_001",
            paidAmount: 7200);

        var second = await service.CompleteFromStripeAsync(
            metadata,
            paymentIntentId: "pi_test_001",
            checkoutSessionId: "cs_test_001",
            paidAmount: 7200);

        Assert.Equal(ReservationCompleteStatus.Created, first.Status);
        Assert.Equal(ReservationCompleteStatus.AlreadyProcessed, second.Status);
        Assert.Equal(first.ReservationId, second.ReservationId);

        Assert.Equal(1, await context.Reservations.CountAsync());
        Assert.Equal(3, await context.ReservationChargeItems.CountAsync());
        Assert.Equal("pi_test_001", (await context.Reservations.SingleAsync()).PaymentIntentId);
    }

    [Fact]
    public async Task CompleteFromStripeAsync_RefundsPayment_WhenPaidAmountDoesNotMatchMetadataAmount()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        var refunds = new FakePaymentRefundService();
        var service = CreateService(context, refunds);

        var metadata = CreateValidMetadata(
            amount: "7200",
            startAt: FormatMetadataDateTime(startAt),
            endAt: FormatMetadataDateTime(endAt));

        var result = await service.CompleteFromStripeAsync(
            metadata,
            paymentIntentId: "pi_test_001",
            checkoutSessionId: "cs_test_001",
            paidAmount: 9999);

        Assert.Contains("Stripeの支払金額と予約金額が一致しません。", result.RefundReason);
        Assert.Equal(ReservationCompleteStatus.Refunded, result.Status);
        Assert.Equal("pi_test_001", Assert.Single(refunds.Refunds).PaymentIntentId);
        Assert.Empty(context.Reservations);
    }

    [Fact]
    public async Task CompleteFromStripeAsync_RefundsPayment_WhenMetadataRequiredKeyIsMissing()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        var refunds = new FakePaymentRefundService();
        var service = CreateService(context, refunds);

        var metadata = CreateValidMetadata(
            amount: "7200",
            startAt: FormatMetadataDateTime(startAt),
            endAt: FormatMetadataDateTime(endAt));

        metadata.Remove("roomId");

        var result = await service.CompleteFromStripeAsync(
            metadata,
            paymentIntentId: "pi_test_001",
            checkoutSessionId: "cs_test_001",
            paidAmount: 7200);

        Assert.Equal("Stripe metadata に roomId がありません。", result.RefundReason);
        Assert.Equal(ReservationCompleteStatus.Refunded, result.Status);
        Assert.Equal("pi_test_001", Assert.Single(refunds.Refunds).PaymentIntentId);
        Assert.Empty(context.Reservations);
    }

    [Fact]
    public async Task CompleteFromStripeAsync_RefundsPayment_WhenMetadataIntValueIsInvalid()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        var refunds = new FakePaymentRefundService();
        var service = CreateService(context, refunds);

        var metadata = CreateValidMetadata(
            amount: "7200",
            startAt: FormatMetadataDateTime(startAt),
            endAt: FormatMetadataDateTime(endAt));

        metadata["roomId"] = "not-number";

        var result = await service.CompleteFromStripeAsync(
            metadata,
            paymentIntentId: "pi_test_001",
            checkoutSessionId: "cs_test_001",
            paidAmount: 7200);

        Assert.Equal(
            "Stripe metadata の roomId が数値ではありません。value=not-number",
            result.RefundReason);

        Assert.Equal(ReservationCompleteStatus.Refunded, result.Status);
        Assert.Equal("pi_test_001", Assert.Single(refunds.Refunds).PaymentIntentId);
        Assert.Empty(context.Reservations);
    }

    [Fact]
    public async Task CompleteFromStripeAsync_RefundsPayment_WhenMetadataDateTimeValueIsInvalid()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var validEndAt = GetNextDayOfWeek(DayOfWeek.Monday, 12);

        var refunds = new FakePaymentRefundService();
        var service = CreateService(context, refunds);

        var metadata = CreateValidMetadata(
            amount: "7200",
            startAt: "invalid-date",
            endAt: FormatMetadataDateTime(validEndAt));

        var result = await service.CompleteFromStripeAsync(
            metadata,
            paymentIntentId: "pi_test_001",
            checkoutSessionId: "cs_test_001",
            paidAmount: 7200);

        Assert.Equal(
            "Stripe metadata の startAt が日時ではありません。value=invalid-date",
            result.RefundReason);

        Assert.Equal(ReservationCompleteStatus.Refunded, result.Status);
        Assert.Equal("pi_test_001", Assert.Single(refunds.Refunds).PaymentIntentId);
        Assert.Empty(context.Reservations);
    }

    [Fact]
    public async Task CompleteFromStripeAsync_RefundsPayment_WhenRecalculatedAmountDoesNotMatchMetadataAmount()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        var refunds = new FakePaymentRefundService();
        var service = CreateService(context, refunds);

        var metadata = CreateValidMetadata(
            amount: "9999",
            startAt: FormatMetadataDateTime(startAt),
            endAt: FormatMetadataDateTime(endAt));

        var result = await service.CompleteFromStripeAsync(
            metadata,
            paymentIntentId: "pi_test_001",
            checkoutSessionId: "cs_test_001",
            paidAmount: null);

        Assert.Contains("予約金額が再計算結果と一致しません。", result.RefundReason);
        Assert.Equal(ReservationCompleteStatus.Refunded, result.Status);
        Assert.Equal("pi_test_001", Assert.Single(refunds.Refunds).PaymentIntentId);
        Assert.Empty(context.Reservations);
    }

    [Fact]
    public async Task CompleteFromStripeAsync_AllowsExistingCanceledReservationAndCreatesNewReservation()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        context.Reservations.Add(new Reservation
        {
            Id = 1,
            RoomId = 1,
            UserId = 20,
            StartAt = startAt,
            EndAt = endAt,
            Amount = 7200,
            Status = "canceled",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var metadata = CreateValidMetadata(
            amount: "7200",
            startAt: FormatMetadataDateTime(startAt),
            endAt: FormatMetadataDateTime(endAt));

        await service.CompleteFromStripeAsync(
            metadata,
            paymentIntentId: "pi_test_001",
            checkoutSessionId: "cs_test_001",
            paidAmount: 7200);

        var reservations = await context.Reservations
            .OrderBy(x => x.Id)
            .ToListAsync();

        Assert.Equal(2, reservations.Count);
        Assert.Equal("canceled", reservations[0].Status);
        Assert.Equal("paid", reservations[1].Status);
        Assert.Equal(startAt, reservations[1].StartAt);
        Assert.Equal(endAt, reservations[1].EndAt);
    }

    [Fact]
    public async Task CompleteFromStripeAsync_同じ時間帯が別の決済で埋まっていたら全額返金し_予約を作らない()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        var refunds = new FakePaymentRefundService();
        var service = CreateService(context, refunds);

        var metadata = CreateValidMetadata(
            amount: "7200",
            startAt: FormatMetadataDateTime(startAt),
            endAt: FormatMetadataDateTime(endAt));

        // 先に届いた別の決済で同じ時間帯が予約された
        var first = await service.CompleteFromStripeAsync(
            metadata,
            paymentIntentId: "pi_first",
            checkoutSessionId: "cs_first",
            paidAmount: 7200);

        // Act：後から届いた決済
        var second = await service.CompleteFromStripeAsync(
            metadata,
            paymentIntentId: "pi_second",
            checkoutSessionId: "cs_second",
            paidAmount: 7200);

        // Assert
        Assert.Equal(ReservationCompleteStatus.Created, first.Status);
        Assert.Equal(ReservationCompleteStatus.Refunded, second.Status);
        Assert.Equal("指定した時間帯は既に予約されています。", second.RefundReason);

        var refund = Assert.Single(refunds.Refunds);
        Assert.Equal("pi_second", refund.PaymentIntentId);

        var reservation = await context.Reservations.SingleAsync();
        Assert.Equal("pi_first", reservation.PaymentIntentId);

        var audit = await context.AuditLogs.SingleAsync(x => x.Action == "STRIPE_REFUND");
        Assert.Contains("pi_second", audit.Note);
    }

    [Fact]
    public async Task CompleteFromStripeAsync_登録済みの決済はmetadataを読む前に処理済みとして返す()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        context.Reservations.Add(new Reservation
        {
            Id = 5,
            RoomId = 1,
            UserId = 20,
            StartAt = GetNextDayOfWeek(DayOfWeek.Monday, 10),
            EndAt = GetNextDayOfWeek(DayOfWeek.Monday, 12),
            Amount = 7200,
            Status = "paid",
            PaymentIntentId = "pi_done",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var refunds = new FakePaymentRefundService();
        var roomLock = new FakeRoomReservationLock();
        var service = CreateService(context, refunds, roomLock);

        // Act：metadata が空でも、同じ決済の予約があれば返金しない
        var result = await service.CompleteFromStripeAsync(
            new Dictionary<string, string>(),
            paymentIntentId: "pi_done",
            checkoutSessionId: "cs_done",
            paidAmount: 7200);

        // Assert
        Assert.Equal(ReservationCompleteStatus.AlreadyProcessed, result.Status);
        Assert.Equal(5, result.ReservationId);
        Assert.Empty(refunds.Refunds);
        Assert.Empty(roomLock.AcquiredRoomIds);
    }

    [Fact]
    public async Task CompleteFromStripeAsync_スタジオ単位のロック内で登録し_最後に解放する()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);

        var roomLock = new FakeRoomReservationLock();
        var service = CreateService(context, roomLock: roomLock);

        var result = await service.CompleteFromStripeAsync(
            CreateValidMetadata(
                amount: "7200",
                startAt: FormatMetadataDateTime(startAt),
                endAt: FormatMetadataDateTime(startAt.AddHours(2))),
            paymentIntentId: "pi_test_001",
            checkoutSessionId: "cs_test_001",
            paidAmount: 7200);

        Assert.Equal(ReservationCompleteStatus.Created, result.Status);
        Assert.Equal(new[] { 1 }, roomLock.AcquiredRoomIds);
        Assert.Equal(1, roomLock.ReleasedCount);
    }

    [Fact]
    public async Task CompleteFromStripeAsync_Webhookが開始時刻を過ぎて届いても決済済みの予約を登録する()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);
        var endAt = startAt.AddHours(2);

        // 現在：開始 30 分後（日本時間）。Webhook の再送が遅れた想定
        var clock = new FixedTimeProvider(
            new DateTimeOffset(JapanTime.ToUtc(startAt.AddMinutes(30)), TimeSpan.Zero));

        var refunds = new FakePaymentRefundService();
        var service = CreateService(context, refunds, timeProvider: clock);

        var result = await service.CompleteFromStripeAsync(
            CreateValidMetadata(
                amount: "7200",
                startAt: FormatMetadataDateTime(startAt),
                endAt: FormatMetadataDateTime(endAt)),
            paymentIntentId: "pi_late",
            checkoutSessionId: "cs_late",
            paidAmount: 7200);

        Assert.Equal(ReservationCompleteStatus.Created, result.Status);
        Assert.Empty(refunds.Refunds);
        Assert.Equal(1, await context.Reservations.CountAsync());
    }

    [Fact]
    public async Task CompleteFromStripeAsync_返金APIが失敗したら例外を返してStripeの再送に任せる()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedBaseDataAsync(context);

        var startAt = GetNextDayOfWeek(DayOfWeek.Monday, 10);

        var refunds = new FakePaymentRefundService
        {
            ExceptionToThrow = new HttpRequestException("Stripe unavailable")
        };
        var service = CreateService(context, refunds);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.CompleteFromStripeAsync(
                CreateValidMetadata(
                    amount: "9999",
                    startAt: FormatMetadataDateTime(startAt),
                    endAt: FormatMetadataDateTime(startAt.AddHours(2))),
                paymentIntentId: "pi_test_001",
                checkoutSessionId: "cs_test_001",
                paidAmount: null));

        Assert.Empty(context.Reservations);
        Assert.False(await context.AuditLogs.AnyAsync(x => x.Action == "STRIPE_REFUND"));
    }

    private static DateTime GetNextDayOfWeek(
        DayOfWeek targetDay,
        int hour,
        int minute = 0)
    {
        var date = JapanTime.Today(TimeProvider.System).AddDays(1);

        while (date.DayOfWeek != targetDay)
        {
            date = date.AddDays(1);
        }

        return DateTime.SpecifyKind(
            date.AddHours(hour).AddMinutes(minute),
            DateTimeKind.Unspecified);
    }

    private static string FormatMetadataDateTime(DateTime value)
    {
        return value.ToString("yyyy-MM-ddTHH:mm:ss");
    }

    private static ReservationCompleteService CreateService(
        AppDbContext context,
        FakePaymentRefundService? refunds = null,
        FakeRoomReservationLock? roomLock = null,
        TimeProvider? timeProvider = null)
    {
        var adminSettingsService = new AdminSettingsService(context);

        var reservationConfirmService = new ReservationConfirmService(
            context,
            adminSettingsService,
            timeProvider);

        return new ReservationCompleteService(
            context,
            reservationConfirmService,
            roomLock ?? new FakeRoomReservationLock(),
            refunds ?? new FakePaymentRefundService(),
            new AuditLogService(context, NullLogger<AuditLogService>.Instance),
            NullLogger<ReservationCompleteService>.Instance);
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

        await SeedBusinessHourAsync(
            context,
            roomId: 1,
            dayOfWeek: (int)DayOfWeek.Monday,
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
