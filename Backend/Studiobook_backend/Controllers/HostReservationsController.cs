using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Host")]
    [Route("api/host/reservations")]
    public class HostReservationsController : ControllerBase
    {
        private readonly HostReservationService _service;
        private readonly AuditLogService _auditLogService;

        public HostReservationsController(
            HostReservationService service,
            AuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<ActionResult<HostReservationListResponseDto>> GetList(
            [FromQuery] string? keyword,
            [FromQuery] string? status,
            [FromQuery] int? reservationId,
            [FromQuery] int? roomId,
            [FromQuery] DateTime? startFrom,
            [FromQuery] DateTime? startTo,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var hostUserId = GetCurrentUserId();

            var result = await _service.GetListAsync(
                hostUserId,
                keyword,
                status,
                reservationId,
                roomId,
                startFrom,
                startTo,
                page,
                pageSize
            );

            return Ok(result);
        }

        [HttpPost("{reservationId:int}/approve")]
        public async Task<IActionResult> Approve(int reservationId)
        {
            var hostUserId = GetCurrentUserId();

            try
            {
                await _service.ApproveAsync(hostUserId, reservationId);

                await _auditLogService.WriteAsync(
                    actorId: hostUserId,
                    action: "APPROVE",
                    entity: "Reservation",
                    entityId: reservationId,
                    note: $"ホストユーザーが予約ID「{reservationId}」を承認しました。"
                );

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{reservationId:int}/cancel")]
        public async Task<IActionResult> Cancel(int reservationId)
        {
            var hostUserId = GetCurrentUserId();

            try
            {
                await _service.CancelAsync(hostUserId, reservationId);

                await _auditLogService.WriteAsync(
                    actorId: hostUserId,
                    action: "CANCEL",
                    entity: "Reservation",
                    entityId: reservationId,
                    note: $"ホストユーザーが予約ID「{reservationId}」をキャンセルしました。"
                );

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                        ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, out var userId))
            {
                throw new UnauthorizedAccessException("ログインユーザーを取得できません。");
            }

            return userId;
        }
    }
}