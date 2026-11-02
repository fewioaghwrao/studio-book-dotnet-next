using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Application.Dtos;
using Studiobook_backend.Application.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Route("api/host/rooms")]
    [Authorize(Roles = "Host")]
    public class HostRoomsController : ControllerBase
    {
        private readonly HostRoomService _hostRoomService;

        public HostRoomsController(HostRoomService hostRoomService)
        {
            _hostRoomService = hostRoomService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResponseDto<HostRoomListItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Index(
            [FromQuery] string? keyword,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiErrorResponseDto
                {
                    Code = "AUTH_REQUIRED",
                    Message = "ログインが必要です。"
                });
            }

            var result = await _hostRoomService.GetListAsync(
                userId.Value,
                keyword,
                page,
                pageSize);

            return Ok(result);
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                              ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                return null;
            }

            return int.TryParse(userIdClaim, out var userId)
                ? userId
                : null;
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(HostRoomDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Show([FromRoute] int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiErrorResponseDto
                {
                    Code = "AUTH_REQUIRED",
                    Message = "ログインが必要です。"
                });
            }

            var room = await _hostRoomService.FindDetailAsync(userId.Value, id);

            if (room == null)
            {
                return NotFound(new ApiErrorResponseDto
                {
                    Code = "ROOM_NOT_FOUND",
                    Message = "スタジオが見つかりません。"
                });
            }

            return Ok(room);
        }
    }
}