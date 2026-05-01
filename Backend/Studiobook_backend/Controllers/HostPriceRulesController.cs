using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Host")]
    [Route("api/host/rooms/{roomId:int}/price-rules")]
    public class HostPriceRulesController : ControllerBase
    {
        private readonly HostPriceRuleService _service;

        public HostPriceRulesController(HostPriceRuleService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<PriceRulesResponseDto>> Get(int roomId)
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

        [HttpPost]
        public async Task<IActionResult> Create(
            int roomId,
            [FromBody] CreatePriceRuleRequestDto request)
        {
            var userId = GetCurrentUserId();

            try
            {
                await _service.AddAsync(roomId, userId, request);
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

        [HttpDelete("{ruleId:int}")]
        public async Task<IActionResult> Delete(int roomId, int ruleId)
        {
            var userId = GetCurrentUserId();

            try
            {
                await _service.DeleteAsync(roomId, ruleId, userId);
                return NoContent();
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