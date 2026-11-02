using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Application.Services;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Host")]
    [Route("api/host/sales")]
    public class HostSalesExportController : ControllerBase
    {
        private readonly HostSalesService _hostSalesService;
        private readonly IHostSalesCsvExporter _csvExporter;
        private readonly IHostSalesInvoicePdfGenerator _invoicePdfGenerator;

        public HostSalesExportController(
            HostSalesService hostSalesService,
            IHostSalesCsvExporter csvExporter,
            IHostSalesInvoicePdfGenerator invoicePdfGenerator)
        {
            _hostSalesService = hostSalesService;
            _csvExporter = csvExporter;
            _invoicePdfGenerator = invoicePdfGenerator;
        }

        [HttpGet("{reservationId:int}/items.csv")]
        public async Task<IActionResult> ExportItemsCsv(int reservationId)
        {
            var hostUserId = GetCurrentUserId();

            var reservation = await _hostSalesService.FindExportDetailAsync(
                hostUserId,
                reservationId);

            if (reservation == null)
            {
                return NotFound(new { message = "売上明細が見つかりません。" });
            }

            var content = _csvExporter.CreateItemsCsv(reservation);

            var fileName = $"reservation-{reservation.ReservationId}-items.csv";

            return File(
                content,
                "text/csv; charset=utf-8",
                fileName
            );
        }

        [HttpGet("{reservationId:int}/invoice.pdf")]
        public async Task<IActionResult> ExportInvoicePdf(int reservationId)
        {
            var hostUserId = GetCurrentUserId();

            var reservation = await _hostSalesService.FindExportDetailAsync(
                hostUserId,
                reservationId);

            if (reservation == null)
            {
                return NotFound(new { message = "売上明細が見つかりません。" });
            }

            var pdf = _invoicePdfGenerator.CreateInvoicePdf(reservation);

            var fileName = $"reservation-{reservation.ReservationId}-invoice.pdf";

            return File(
                pdf,
                "application/pdf",
                fileName
            );
        }

        private int GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, out var userId))
            {
                throw new UnauthorizedAccessException("ログインユーザーを取得できません。");
            }

            return userId;
        }
    }
}