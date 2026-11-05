using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Application.Common;
using Studiobook_backend.Application.Dtos.Admin;
using Studiobook_backend.Application.Services;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/status")]
    public class AdminStatusController : ControllerBase
    {
        private readonly AdminStatusService _service;
        private readonly TimeProvider _timeProvider;

        public AdminStatusController(AdminStatusService service, TimeProvider? timeProvider = null)
        {
            _service = service;
            _timeProvider = timeProvider ?? TimeProvider.System;
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

        private DateTime ResolveBaseMonth(int? year, int? month)
        {
            if (year.HasValue && month.HasValue && month.Value is >= 1 and <= 12)
            {
                return new DateTime(year.Value, month.Value, 1);
            }

            // 未指定時は日本時間の今月（サーバーが UTC でも月初 0〜9 時に前月にならないように）
            return JapanTime.FirstDayOfCurrentMonth(_timeProvider);
        }
    }
}