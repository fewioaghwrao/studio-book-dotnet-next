using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Admin;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/rooms")]
    public class AdminRoomsController : ControllerBase
    {
        private readonly AdminRoomService _service;
        private readonly AuditLogService _auditLogService;

        public AdminRoomsController(
            AdminRoomService service,
            AuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<ActionResult<AdminRoomListResponseDto>> GetList(
            [FromQuery] string? keyword,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetListAsync(keyword, page, pageSize);
            return Ok(result);
        }

        [HttpGet("host-options")]
        public async Task<ActionResult<List<AdminRoomHostOptionDto>>> GetHostOptions()
        {
            var result = await _service.GetHostOptionsAsync();
            return Ok(result);
        }

        [HttpGet("{roomId:int}")]
        public async Task<ActionResult<AdminRoomDetailDto>> GetDetail(int roomId)
        {
            var result = await _service.GetDetailAsync(roomId);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "スタジオが見つかりません。"
                });
            }

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<AdminRoomDetailDto>> Create(
            [FromBody] AdminRoomUpsertRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "入力内容を確認してください。"
                });
            }

            try
            {
                var result = await _service.CreateAsync(request);

                await _auditLogService.WriteAsync(
                    actorId: GetCurrentUserId(),
                    action: "CREATE",
                    entity: "Room",
                    entityId: result.Id,
                    note: $"スタジオ「{result.Name}」を登録しました。"
                );

                return Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "HOST_NOT_FOUND")
            {
                return BadRequest(new
                {
                    message = "有効なスタジオ提供者を選択してください。"
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "ROOM_DUPLICATED")
            {
                return Conflict(new
                {
                    message = "同一の『スタジオ名 × 住所』のスタジオが既に存在します。"
                });
            }
        }

        [HttpPut("{roomId:int}")]
        public async Task<ActionResult<AdminRoomDetailDto>> Update(
            int roomId,
            [FromBody] AdminRoomUpsertRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "入力内容を確認してください。"
                });
            }

            try
            {
                var result = await _service.UpdateAsync(roomId, request);

                if (result == null)
                {
                    return NotFound(new
                    {
                        message = "スタジオが見つかりません。"
                    });
                }

                await _auditLogService.WriteAsync(
                    actorId: GetCurrentUserId(),
                    action: "UPDATE",
                    entity: "Room",
                    entityId: result.Id,
                    note: $"スタジオ「{result.Name}」の基本情報を更新しました。"
                );

                return Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "HOST_NOT_FOUND")
            {
                return BadRequest(new
                {
                    message = "有効なスタジオ提供者を選択してください。"
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "ROOM_DUPLICATED")
            {
                return Conflict(new
                {
                    message = "同一の『スタジオ名 × 住所』のスタジオが既に存在します。"
                });
            }
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                        ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return int.TryParse(value, out var userId)
                ? userId
                : null;
        }
    }
}