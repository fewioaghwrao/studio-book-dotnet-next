using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Admin;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/status")]
    public class AdminStatusController : ControllerBase
    {
        private readonly AdminStatusService _service;

        public AdminStatusController(AdminStatusService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<AdminStatusResponseDto>> Get(
            [FromQuery] int? roomId,
            [FromQuery] int? year,
            [FromQuery] int? month)
        {
            var normalizedRoomId = roomId is null or 0
                ? null
                : roomId;

            var baseDate = ResolveBaseMonth(year, month);

            var result = await _service.GetAsync(
                normalizedRoomId,
                baseDate
            );

            return Ok(result);
        }

        private static DateTime ResolveBaseMonth(int? year, int? month)
        {
            if (year.HasValue && month.HasValue && month.Value is >= 1 and <= 12)
            {
                return new DateTime(year.Value, month.Value, 1);
            }

            return new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        }
    }
}