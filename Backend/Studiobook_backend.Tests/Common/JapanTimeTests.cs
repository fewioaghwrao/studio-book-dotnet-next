using Studiobook_backend.Application.Common;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Common;

public class JapanTimeTests
{
    [Fact]
    public void Now_UTCの現在時刻を日本時間の壁時計時刻に変換する()
    {
        // UTC 10/31 15:30 ＝ 日本時間 11/1 0:30
        var clock = FixedTimeProvider.AtUtc(2026, 10, 31, 15, 30);

        var now = JapanTime.Now(clock);

        Assert.Equal(new DateTime(2026, 11, 1, 0, 30, 0), now);
        Assert.Equal(DateTimeKind.Unspecified, now.Kind);
    }

    [Fact]
    public void Today_UTCではまだ前日でも日本時間の今日を返す()
    {
        var clock = FixedTimeProvider.AtUtc(2026, 10, 31, 15, 30);

        Assert.Equal(new DateTime(2026, 11, 1), JapanTime.Today(clock));
    }

    [Fact]
    public void FirstDayOfCurrentMonth_日本時間の月初0時台は当月を返す()
    {
        var clock = FixedTimeProvider.AtUtc(2026, 10, 31, 15, 30);

        Assert.Equal(new DateTime(2026, 11, 1), JapanTime.FirstDayOfCurrentMonth(clock));
    }

    [Fact]
    public void FirstDayOfCurrentMonth_UTCで月が変わっても日本時間ではすでに翌月()
    {
        // UTC 11/30 23:00 ＝ 日本時間 12/1 8:00
        var clock = FixedTimeProvider.AtUtc(2026, 11, 30, 23, 0);

        Assert.Equal(new DateTime(2026, 12, 1), JapanTime.FirstDayOfCurrentMonth(clock));
    }

    [Fact]
    public void ToUtc_日本時間の0時はUTCの前日15時()
    {
        var utc = JapanTime.ToUtc(new DateTime(2026, 5, 2));

        Assert.Equal(new DateTime(2026, 5, 1, 15, 0, 0), utc);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
    }

    [Fact]
    public void FromUtc_とToUtc_は往復で同じ値になる()
    {
        var japan = new DateTime(2026, 11, 6, 7, 0, 0);

        Assert.Equal(japan, JapanTime.FromUtc(JapanTime.ToUtc(japan)));
    }
}
