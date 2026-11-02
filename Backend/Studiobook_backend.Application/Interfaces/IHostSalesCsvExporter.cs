using Studiobook_backend.Application.Dtos.Host;

namespace Studiobook_backend.Application.Interfaces;

/// <summary>
/// ホスト向け売上データの CSV 出力（UTF-8 BOM 付き）。
/// </summary>
public interface IHostSalesCsvExporter
{
    /// <summary>売上一覧 CSV を作成します。</summary>
    byte[] CreateSalesCsv(IEnumerable<HostSalesCsvRowDto> rows);

    /// <summary>予約1件分の売上明細 CSV を作成します。</summary>
    byte[] CreateItemsCsv(HostSalesExportDetailDto reservation);
}
