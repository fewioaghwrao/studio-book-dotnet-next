using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Application.Services;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Host")]
    [Route("api/host")]
    public class HostSalesCsvController : ControllerBase
    {
        private readonly HostSalesService _hostSalesService;
        private readonly IHostSalesCsvExporter _csvExporter;

        public HostSalesCsvController(
            HostSalesService hostSalesService,
            IHostSalesCsvExporter csvExporter)
        {
            _hostSalesService = hostSalesService;
            _csvExporter = csvExporter;
        }

        [HttpGet("sales.csv")]
        public async Task<IActionResult> ExportSalesCsv(
            [FromQuery] int? roomId,
            [FromQuery] bool onlyWithItems = true)
        {
            var hostUserId = GetCurrentUserId();

            var rows = await _hostSalesService.GetCsvRowsAsync(
                hostUserId,
                roomId,
                onlyWithItems);

            var content = _csvExporter.CreateSalesCsv(rows);

            var fileName = $"host-sales-{DateTime.Now:yyyyMMddHHmmss}.csv";

            return File(
                content,
                "text/csv; charset=utf-8",
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