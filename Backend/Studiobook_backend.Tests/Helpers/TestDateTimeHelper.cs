namespace Studiobook_backend.Tests.Helpers;

public static class TestDateTimeHelper
{
    public static DateTime GetNextDayOfWeek(
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
}