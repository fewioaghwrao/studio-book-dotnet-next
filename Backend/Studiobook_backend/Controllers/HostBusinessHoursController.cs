using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Route("api/host/rooms/{roomId:int}/business-hours")]
    [Authorize(Roles = "Host")]
    public class HostBusinessHoursController : ControllerBase
    {
        private readonly HostBusinessHourService _service;

        public HostBusinessHoursController(HostBusinessHourService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<BusinessHoursResponseDto>> Get(int roomId)
        {
            var userId = GetCurrentUserId();

            try
            {
                var result = await _service.GetAsync(roomId, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update(
            int roomId,
            [FromBody] BusinessHoursUpdateRequestDto request)
        {
            var userId = GetCurrentUserId();

            try
            {
                await _service.SaveAsync(roomId, userId, request);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
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