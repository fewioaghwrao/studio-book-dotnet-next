using Studiobook_backend.Domain.Entities;

namespace Studiobook_backend.Application.Common;

/// <summary>
/// 予約の「利用前／利用済み」の判定。
///
/// - 利用済み：決済済み（paid）かつ終了日時が現在（日本時間）以前
/// - 利用前　：予約済み（booked）、または決済済みで終了日時が現在より後
///
/// 統計では「利用前」を見込み売上、「利用済み」を確定売上として集計し、
/// レビューは利用済みの予約がある場合のみ投稿できる。
/// EF のクエリ内では <c>x.Status == ReservationStatuses.Paid &amp;&amp; x.EndAt &lt;= now</c> と同じ条件を書く。
/// </summary>
public static class ReservationUsage
{
    /// <summary>利用済みかどうか。<paramref name="nowJapan"/> は <see cref="JapanTime.Now"/> の値。</summary>
    public static bool IsUsed(string? status, DateTime endAt, DateTime nowJapan)
    {
        return status == ReservationStatuses.Paid && endAt <= nowJapan;
    }

    /// <summary>利用前（見込み）かどうか。キャンセルは含まない。</summary>
    public static bool IsUpcoming(string? status, DateTime endAt, DateTime nowJapan)
    {
        return status == ReservationStatuses.Booked ||
               (status == ReservationStatuses.Paid && endAt > nowJapan);
    }
}
