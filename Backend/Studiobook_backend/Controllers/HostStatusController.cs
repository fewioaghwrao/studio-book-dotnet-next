using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Application.Common;
using Studiobook_backend.Application.Dtos.Host;
using Studiobook_backend.Application.Services;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Host")]
    [Route("api/host/status")]
    public class HostStatusController : ControllerBase
    {
        private readonly HostStatusService _service;
        private readonly TimeProvider _timeProvider;

        public HostStatusController(HostStatusService service, TimeProvider? timeProvider = null)
        {
            _service = service;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        [HttpGet]
        public async Task<ActionResult<HostStatusResponseDto>> Get(
            [FromQuery] int? roomId,
            [FromQuery] int? year,
            [FromQuery] int? month)
        {
            var hostUserId = GetCurrentUserId();

            var normalizedRoomId = roomId is null or 0
                ? null
                : roomId;

            var baseDate = ResolveBaseMonth(year, month);

            var result = await _service.GetAsync(
                hostUserId,
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