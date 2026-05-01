using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Host")]
    [Route("api/host/sales")]
    public class HostSalesController : ControllerBase
    {
        private readonly HostSalesService _service;

        public HostSalesController(HostSalesService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<HostSalesListResponseDto>> GetList(
            [FromQuery] int? roomId,
            [FromQuery] bool onlyWithItems = true,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var hostUserId = GetCurrentUserId();

            var result = await _service.GetListAsync(
                hostUserId,
                roomId,
                onlyWithItems,
                page,
                pageSize
            );

            return Ok(result);
        }

        [HttpGet("{reservationId:int}")]
        public async Task<ActionResult<HostSalesDetailResponseDto>> GetDetail(
            int reservationId)
        {
            var hostUserId = GetCurrentUserId();

            try
            {
                var result = await _service.GetDetailAsync(hostUserId, reservationId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
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