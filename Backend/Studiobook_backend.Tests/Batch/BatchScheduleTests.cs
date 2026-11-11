using Studiobook_backend.Application.Batch;

namespace Studiobook_backend.Tests.Batch;

public class BatchScheduleTests
{
    private static readonly TimeOnly FourAm = new(4, 0);

    [Fact]
    public void GetLatestScheduledUtc_日本時間4時より前なら前日4時_後なら当日4時()
    {
        // 日本時間 11/12 3:59（UTC 11/11 18:59）→ 直近の予定は 日本時間 11/11 4:00（UTC 11/10 19:00）
        Assert.Equal(
            new DateTime(2026, 11, 10, 19, 0, 0, DateTimeKind.Utc),
            BatchSchedule.GetLatestScheduledUtc(new DateTime(2026, 11, 11, 18, 59, 0, DateTimeKind.Utc), FourAm));

        // 日本時間 11/12 4:00（UTC 11/11 19:00）→ 当日 4:00
        Assert.Equal(
            new DateTime(2026, 11, 11, 19, 0, 0, DateTimeKind.Utc),
            BatchSchedule.GetLatestScheduledUtc(new DateTime(2026, 11, 11, 19, 0, 0, DateTimeKind.Utc), FourAm));
    }

    [Fact]
    public void IsDue_一度も成功していなければ実行する()
    {
        Assert.True(BatchSchedule.IsDue(null, new DateTime(2026, 11, 11, 20, 0, 0, DateTimeKind.Utc), FourAm));
    }

    [Fact]
    public void IsDue_直近の予定時刻より後に成功していれば実行しない()
    {
        var nowUtc = new DateTime(2026, 11, 11, 23, 0, 0, DateTimeKind.Utc);       // 日本時間 11/12 8:00
        var succeededUtc = new DateTime(2026, 11, 11, 19, 5, 0, DateTimeKind.Utc); // 日本時間 11/12 4:05

        Assert.False(BatchSchedule.IsDue(succeededUtc, nowUtc, FourAm));
    }

    [Fact]
    public void IsDue_再起動で予定時刻を逃していても次の確認で実行する()
    {
        var nowUtc = new DateTime(2026, 11, 11, 23, 0, 0, DateTimeKind.Utc);      // 日本時間 11/12 8:00
        var succeededUtc = new DateTime(2026, 11, 10, 19, 5, 0, DateTimeKind.Utc); // 前日 4:05 に成功したきり

        Assert.True(BatchSchedule.IsDue(succeededUtc, nowUtc, FourAm));
    }
}
