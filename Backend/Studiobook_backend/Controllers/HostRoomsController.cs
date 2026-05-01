using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Route("api/host/rooms")]
    [Authorize(Roles = "Host")]
    public class HostRoomsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public HostRoomsController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
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

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _dbContext.Rooms
                .AsNoTracking()
                .Where(x => x.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var trimmedKeyword = keyword.Trim();
                query = query.Where(x => x.Name.Contains(trimmedKeyword));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new HostRoomListItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    PostalCode = x.PostalCode,
                    Address = x.Address
                })
                .ToListAsync();

            var totalPages = totalCount == 0
                ? 1
                : (int)Math.Ceiling(totalCount / (double)pageSize);

            return Ok(new PagedResponseDto<HostRoomListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            });
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

            var room = await _dbContext.Rooms
                .AsNoTracking()
                .Where(x => x.Id == id && x.UserId == userId.Value)
                .Select(x => new HostRoomDetailDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    ImageName = x.ImageName,
                    Description = x.Description,
                    Price = x.Price,
                    Capacity = x.Capacity,
                    PostalCode = x.PostalCode,
                    Address = x.Address
                })
                .FirstOrDefaultAsync();

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