using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Host")]
    [Route("api/host/reviews")]
    public class HostReviewsController : ControllerBase
    {
        private readonly HostReviewService _service;

        public HostReviewsController(HostReviewService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<HostReviewListResponseDto>> GetList(
            [FromQuery] int? roomId,
            [FromQuery] int? stars,
            [FromQuery] bool? onlyHidden,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var hostUserId = GetCurrentUserId();

            var result = await _service.GetListAsync(
                hostUserId,
                roomId,
                stars,
                onlyHidden,
                page,
                pageSize
            );

            return Ok(result);
        }

        [HttpPost("{reviewId:int}/reply")]
        public async Task<IActionResult> Reply(
            int reviewId,
            [FromBody] HostReviewReplyRequestDto request)
        {
            var hostUserId = GetCurrentUserId();

            try
            {
                await _service.SaveReplyAsync(hostUserId, reviewId, request);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{reviewId:int}/visibility")]
        public async Task<IActionResult> ChangeVisibility(
            int reviewId,
            [FromBody] HostReviewVisibilityRequestDto request)
        {
            var hostUserId = GetCurrentUserId();

            try
            {
                await _service.ChangeVisibilityAsync(hostUserId, reviewId, request);
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