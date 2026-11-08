using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Application.Common;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Application.Dtos.Reservations;
using Studiobook_backend.Domain.Entities;

namespace Studiobook_backend.Application.Services;

public class ReservationConfirmService
{
    private readonly IAppDbContext _db;
    private readonly AdminSettingsService _adminSettingsService;
    private readonly TimeProvider _timeProvider;

    public ReservationConfirmService(
        IAppDbContext db,
        AdminSettingsService adminSettingsService,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _adminSettingsService = adminSettingsService;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <param name="allowPastStart">
    /// 決済完了（Webhook）時の再検証では true。Checkout 時点で開始前だった予約が、
    /// Webhook の遅延・再送で開始時刻を過ぎても、決済済みの予約を失わないようにする。
    /// </param>
    public async Task<ReservationConfirmResponse> BuildConfirmAsync(
        int userId,
        ReservationConfirmRequest request,
        bool allowPastStart = false)
    {
        if (request.StartAt >= request.EndAt)
        {
            throw new InvalidOperationException("終了日時は開始日時より後にしてください。");
        }

        // StartAt は日本時間の壁時計時刻のため、現在時刻も日本時間で比較する
        // （DateTime.Now はサーバーのタイムゾーン依存で、Heroku では UTC になり 9 時間ずれる）
        if (!allowPastStart && request.StartAt <= JapanTime.Now(_timeProvider))
        {
            throw new InvalidOperationException("開始日時は現在時刻より後にしてください。");
        }

        var room = await _db.Rooms
            .Include(x => x.BusinessHours)
            .Include(x => x.Closures)
            .Include(x => x.PriceRules)
            .FirstOrDefaultAsync(x => x.Id == request.RoomId);

        if (room == null)
        {
            throw new KeyNotFoundException("スタジオが見つかりません。");
        }

        ValidateSameDay(request.StartAt, request.EndAt);
        ValidateBusinessHour(room, request.StartAt, request.EndAt);
        ValidateClosure(room, request.StartAt, request.EndAt);
        await ValidateReservationOverlapAsync(room.Id, request.StartAt, request.EndAt);

        var items = BuildChargeItems(room, request.StartAt, request.EndAt);

        var settings = await _adminSettingsService.GetAsync();

        var taxRate = settings.TaxRatePercent / 100m;
        var platformFeeRate = settings.AdminFeeRatePercent / 100m;

        var subtotal = items.Sum(x => x.Amount);

        var tax = CalculateRateAmount(subtotal, taxRate);
        var platformFee = CalculateRateAmount(subtotal, platformFeeRate);

        var amount = subtotal + tax + platformFee;

        var hours = CalculateHours(request.StartAt, request.EndAt);

        return new ReservationConfirmResponse
        {
            RoomId = room.Id,
            RoomName = room.Name,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            HourlyPrice = room.Price,
            Hours = hours,
            Subtotal = subtotal,

            TaxRatePercent = settings.TaxRatePercent,
            Tax = tax,

            PlatformFeeRatePercent = settings.AdminFeeRatePercent,
            PlatformFee = platformFee,

            Amount = amount,
            Items = items
        };
    }

    private static int CalculateRateAmount(int baseAmount, decimal rate)
    {
        return (int)Math.Round(baseAmount * rate, MidpointRounding.AwayFromZero);
    }

    private static void ValidateSameDay(DateTime startAt, DateTime endAt)
    {
        if (startAt.Date != endAt.Date)
        {
            throw new InvalidOperationException("日をまたぐ予約はできません。1日ごとに予約してください。");
        }
    }

    private static void ValidateBusinessHour(Room room, DateTime startAt, DateTime endAt)
    {
        var dayOfWeek = ToBusinessDayOfWeek(startAt.DayOfWeek);

        var businessHour = room.BusinessHours
            .FirstOrDefault(x => x.DayOfWeek == dayOfWeek);

        if (businessHour == null || businessHour.IsHoliday)
        {
            throw new InvalidOperationException("指定日は営業時間外です。");
        }

        if (businessHour.StartTime == null || businessHour.EndTime == null)
        {
            throw new InvalidOperationException("指定日の営業時間が設定されていません。");
        }

        var startTime = TimeOnly.FromDateTime(startAt);
        var endTime = TimeOnly.FromDateTime(endAt);

        if (startTime < businessHour.StartTime.Value ||
            endTime > businessHour.EndTime.Value)
        {
            throw new InvalidOperationException("営業時間外の時間が含まれています。");
        }
    }

    private static void ValidateClosure(Room room, DateTime startAt, DateTime endAt)
    {
        var hasClosure = room.Closures.Any(x =>
            x.StartAt < endAt &&
            startAt < x.EndAt);

        if (hasClosure)
        {
            throw new InvalidOperationException("指定した時間帯は休業期間と重複しています。");
        }
    }

    private async Task ValidateReservationOverlapAsync(
        int roomId,
        DateTime startAt,
        DateTime endAt)
    {
        var exists = await _db.Reservations.AnyAsync(x =>
            x.RoomId == roomId &&
            x.Status != ReservationStatuses.Canceled &&
            x.StartAt < endAt &&
            startAt < x.EndAt);

        if (exists)
        {
            throw new InvalidOperationException("指定した時間帯は既に予約されています。");
        }
    }

    private static List<ReservationConfirmItemDto> BuildChargeItems(
        Room room,
        DateTime startAt,
        DateTime endAt)
    {
        var items = new List<ReservationConfirmItemDto>();

        var cursor = startAt;

        while (cursor < endAt)
        {
            var next = cursor.AddHours(1);

            if (next > endAt)
            {
                next = endAt;
            }

            var hours = CalculateHours(cursor, next);
            var unitRate = ResolveUnitRate(room, cursor);
            var amount = (int)Math.Round(unitRate * hours, MidpointRounding.AwayFromZero);

            items.Add(new ReservationConfirmItemDto
            {
                Label = unitRate == room.Price ? "通常料金" : "料金ルール適用",
                Amount = amount,
                SliceStart = cursor,
                SliceEnd = next,
                UnitRatePerHour = unitRate
            });

            cursor = next;
        }

        return MergeSameRateItems(items);
    }

    private static int ResolveUnitRate(Room room, DateTime targetAt)
    {
        var basePrice = room.Price;
        var time = TimeOnly.FromDateTime(targetAt);
        var dotnetWeekday = (int)targetAt.DayOfWeek;

        var matchedRule = room.PriceRules
            .Where(x =>
                x.RuleType == "multiplier" &&
                x.Multiplier != null &&
                (x.Weekday == null || x.Weekday == dotnetWeekday) &&
                (x.StartHour == null || time >= x.StartHour.Value) &&
                (x.EndHour == null || time < x.EndHour.Value))
            .OrderByDescending(x => x.Weekday.HasValue)
            .ThenByDescending(x => x.StartHour.HasValue)
            .FirstOrDefault();

        if (matchedRule?.Multiplier != null)
        {
            return (int)Math.Round(basePrice * matchedRule.Multiplier.Value, MidpointRounding.AwayFromZero);
        }

        return basePrice;
    }

    private static List<ReservationConfirmItemDto> MergeSameRateItems(
        List<ReservationConfirmItemDto> items)
    {
        var result = new List<ReservationConfirmItemDto>();

        foreach (var item in items)
        {
            var last = result.LastOrDefault();

            if (last != null &&
                last.UnitRatePerHour == item.UnitRatePerHour &&
                last.Label == item.Label &&
                last.SliceEnd == item.SliceStart)
            {
                last.Amount += item.Amount;
                last.SliceEnd = item.SliceEnd;
            }
            else
            {
                result.Add(item);
            }
        }

        return result;
    }

    private static decimal CalculateHours(DateTime startAt, DateTime endAt)
    {
        return (decimal)(endAt - startAt).TotalHours;
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
            _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek))
        };
    }
}