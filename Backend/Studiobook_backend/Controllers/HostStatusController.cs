using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Host")]
    [Route("api/host/status")]
    public class HostStatusController : ControllerBase
    {
        private readonly HostStatusService _service;

        public HostStatusController(HostStatusService service)
        {
            _service = service;
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

        private static DateTime ResolveBaseMonth(int? year, int? month)
        {
            if (year.HasValue && month.HasValue && month.Value is >= 1 and <= 12)
            {
                return new DateTime(year.Value, month.Value, 1);
            }

            return new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
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