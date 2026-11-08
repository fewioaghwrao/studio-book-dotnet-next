namespace Studiobook_backend.Domain.Entities
{
    /// <summary>
    /// 予約ステータス（Reservations.Status に保存する値）。
    ///
    /// - booked   : 予約済み（未決済）。デモデータや Stripe を通さない予約で使う。ホストの「承認」で paid になる
    /// - paid     : 決済済み。Stripe の決済完了（Webhook）で登録される
    /// - canceled : キャンセル
    ///
    /// 「利用済み」は保存せず、paid かつ終了日時（日本時間）が現在以前かどうかで判定する
    /// （Application/Common/ReservationUsage）。
    /// </summary>
    public static class ReservationStatuses
    {
        public const string Booked = "booked";
        public const string Paid = "paid";
        public const string Canceled = "canceled";
    }
}
