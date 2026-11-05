namespace Studiobook_backend.Tests.Helpers;

/// <summary>
/// 現在時刻を固定する TimeProvider（テスト用）。
/// サーバーが UTC で動いていても日本時間で判定できているかを、日付の境目で検証するために使う。
/// </summary>
public sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow.ToUniversalTime();
    }

    /// <summary>UTC の日時で現在時刻を指定する。</summary>
    public static FixedTimeProvider AtUtc(int year, int month, int day, int hour, int minute = 0)
    {
        return new FixedTimeProvider(new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero));
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;
}
