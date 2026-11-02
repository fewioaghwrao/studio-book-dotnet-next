namespace Studiobook_backend.Application.Dtos.Host
{
    /// <summary>
    /// 売上一覧 CSV の1行。
    /// </summary>
    public class HostSalesCsvRowDto
    {
        public int ReservationId { get; set; }

        public string RoomName { get; set; } = string.Empty;

        public string GuestName { get; set; } = string.Empty;

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public int Amount { get; set; }

        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// 予約1件分の売上明細（明細 CSV / 請求書 PDF 出力用）。
    /// </summary>
    public class HostSalesExportDetailDto
    {
        public int ReservationId { get; set; }

        public string RoomName { get; set; } = string.Empty;

        public string GuestName { get; set; } = string.Empty;

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public int Amount { get; set; }

        public string Status { get; set; } = string.Empty;

        public List<HostSalesExportItemDto> Items { get; set; } = new();
    }

    public class HostSalesExportItemDto
    {
        public string Kind { get; set; } = string.Empty;

        // 出力時に null と空文字を区別するため null 許容のまま保持します
        public string? Description { get; set; }

        public DateTime? SliceStart { get; set; }

        public DateTime? SliceEnd { get; set; }

        public int? UnitRatePerHour { get; set; }

        public int SliceAmount { get; set; }
    }
}
