using Studiobook_backend.Application.Dtos.Host;

namespace Studiobook_backend.Application.Interfaces;

/// <summary>
/// 予約1件分の売上明細（請求書）PDF の作成。
/// </summary>
public interface IHostSalesInvoicePdfGenerator
{
    byte[] CreateInvoicePdf(HostSalesExportDetailDto reservation);
}
