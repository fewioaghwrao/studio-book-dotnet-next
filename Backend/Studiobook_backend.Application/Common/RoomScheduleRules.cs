using Studiobook_backend.Application.Dtos.Reservations;
using Studiobook_backend.Domain.Entities;

namespace Studiobook_backend.Application.Common;

/// <summary>
/// スタジオの営業時間・休業期間による予約可否の判定（予約確認と AI 検索の空き判定で共通）。
/// 予約の重複と現在時刻の判定は DB・時計が必要なため、ここでは扱わない。
/// </summary>
public static class RoomScheduleRules
{
    /// <summary>
    /// 営業時間・休業期間から見て予約できるかを判定する。
    /// <paramref name="room"/> は BusinessHours と Closures を読み込んでおくこと。
    /// </summary>
    public static RoomAvailabilityStatus Evaluate(Room room, DateTime startAt, DateTime endAt)
    {
        if (startAt.Date != endAt.Date)
        {
            return RoomAvailabilityStatus.CrossDay;
        }

        var dayOfWeek = ToBusinessDayOfWeek(startAt.DayOfWeek);

        var businessHour = room.BusinessHours
            .FirstOrDefault(x => x.DayOfWeek == dayOfWeek);

        if (businessHour == null || businessHour.IsHoliday)
        {
            return RoomAvailabilityStatus.Holiday;
        }

        if (businessHour.StartTime == null || businessHour.EndTime == null)
        {
            return RoomAvailabilityStatus.BusinessHoursNotSet;
        }

        var startTime = TimeOnly.FromDateTime(startAt);
        var endTime = TimeOnly.FromDateTime(endAt);

        if (startTime < businessHour.StartTime.Value ||
            endTime > businessHour.EndTime.Value)
        {
            return RoomAvailabilityStatus.OutsideBusinessHours;
        }

        var hasClosure = room.Closures.Any(x =>
            x.StartAt < endAt &&
            startAt < x.EndAt);

        if (hasClosure)
        {
            return RoomAvailabilityStatus.Closure;
        }

        return RoomAvailabilityStatus.Available;
    }

    /// <summary>BusinessHours.DayOfWeek の値（月曜 = 1 〜 日曜 = 7）。</summary>
    public static int ToBusinessDayOfWeek(DayOfWeek dayOfWeek)
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
