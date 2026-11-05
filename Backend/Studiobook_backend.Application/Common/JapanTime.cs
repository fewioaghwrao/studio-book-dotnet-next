namespace Studiobook_backend.Application.Common;

/// <summary>
/// 日時の基準（日本時間）をまとめたヘルパー。
///
/// このアプリの日時の扱い:
/// - 予約・休業日・営業時間などの業務日時（StartAt / EndAt など）は「日本時間の壁時計時刻」
///   （DateTimeKind.Unspecified、オフセットなし）として保存・送受信する。
/// - 作成・更新・有効期限などの記録用日時（*Utc、Ts、HostReplyAt）は UTC で保存する。
/// - 「現在」「今日」「今月」の判定は、サーバーのタイムゾーン（Heroku では UTC）に依存させず、
///   必ずこのクラスで日本時間に変換してから行う。
/// </summary>
public static class JapanTime
{
    /// <summary>日本時間（Asia/Tokyo）。日本は夏時間がないため、見つからない環境では UTC+9 固定で代用する。</summary>
    public static readonly TimeZoneInfo Zone = ResolveZone();

    /// <summary>UTC の日時を日本時間の壁時計時刻（Kind = Unspecified）へ変換する。</summary>
    public static DateTime FromUtc(DateTime utc)
    {
        var source = utc.Kind == DateTimeKind.Utc
            ? utc
            : DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        var japan = TimeZoneInfo.ConvertTimeFromUtc(source, Zone);
        return DateTime.SpecifyKind(japan, DateTimeKind.Unspecified);
    }

    /// <summary>日本時間の壁時計時刻を UTC（Kind = Utc）へ変換する。日付指定の検索条件を UTC 列と比較するときに使う。</summary>
    public static DateTime ToUtc(DateTime japanWallClock)
    {
        var source = DateTime.SpecifyKind(japanWallClock, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(source, Zone);
    }

    /// <summary>現在の日本時間（Kind = Unspecified）。予約の StartAt などと比較できる。</summary>
    public static DateTime Now(TimeProvider timeProvider)
    {
        return FromUtc(timeProvider.GetUtcNow().UtcDateTime);
    }

    /// <summary>日本時間の今日（0:00）。</summary>
    public static DateTime Today(TimeProvider timeProvider)
    {
        return Now(timeProvider).Date;
    }

    /// <summary>日本時間の今月 1 日（0:00）。</summary>
    public static DateTime FirstDayOfCurrentMonth(TimeProvider timeProvider)
    {
        var today = Today(timeProvider);
        return new DateTime(today.Year, today.Month, 1);
    }

    private static TimeZoneInfo ResolveZone()
    {
        // Linux / macOS は IANA ID、Windows は Windows ID（.NET 6 以降は相互変換もされる）
        foreach (var id in new[] { "Asia/Tokyo", "Tokyo Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "Asia/Tokyo",
            TimeSpan.FromHours(9),
            "Japan Standard Time",
            "Japan Standard Time");
    }
}
