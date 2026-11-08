namespace Studiobook_backend.Infrastructure.Reports
{
    /// <summary>
    /// 売上帳票で使う表示ラベル。
    /// </summary>
    internal static class HostSalesLabels
    {
        /// <summary>
        /// 予約ステータスの表示名。決済済み（paid）は利用済みかどうかで「決済済み」「利用済み」を分ける。
        /// </summary>
        public static string ToStatusLabel(string? status, bool isUsed)
        {
            return status switch
            {
                "booked" => "予約済み",
                "paid" => isUsed ? "利用済み" : "決済済み",
                "canceled" => "キャンセル済み",
                _ => status ?? ""
            };
        }

        public static string ToKindLabel(string? kind)
        {
            return kind switch
            {
                "base" => "基本料金",
                "multiplier" => "加算料金",
                "tax" => "消費税",
                "platform_fee" => "手数料",
                "基本料金" => "基本料金",
                "加算料金" => "加算料金",
                _ => kind ?? ""
            };
        }
    }
}
