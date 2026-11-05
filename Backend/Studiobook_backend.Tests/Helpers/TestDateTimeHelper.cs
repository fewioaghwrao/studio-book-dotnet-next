using Studiobook_backend.Application.Common;

namespace Studiobook_backend.Tests.Helpers;

public static class TestDateTimeHelper
{
    public static DateTime GetNextDayOfWeek(
        DayOfWeek targetDay,
        int hour,
        int minute = 0)
    {
        // 予約日時は日本時間の壁時計時刻のため、日本時間の明日以降で探す
        var date = JapanTime.Today(TimeProvider.System).AddDays(1);

        while (date.DayOfWeek != targetDay)
        {
            date = date.AddDays(1);
        }

        return date.AddHours(hour).AddMinutes(minute);
    }
}