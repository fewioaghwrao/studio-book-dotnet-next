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
    [Route("api/admin/settings")]
    public class AdminSettingsController : ControllerBase
    {
        private readonly AdminSettingsService _service;
        private readonly AuditLogService _auditLogService;

        public AdminSettingsController(
            AdminSettingsService service,
            AuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<ActionResult<AdminSettingsDto>> Get()
        {
            var result = await _service.GetAsync();
            return Ok(result);
        }

        [HttpPut]
        public async Task<ActionResult<AdminSettingsDto>> Update(
            [FromBody] UpdateAdminSettingsRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "入力内容を確認してください。"
                });
            }

            var result = await _service.UpdateAsync(request);

            var actorId = GetCurrentUserId();

            await _auditLogService.WriteAsync(
                actorId: actorId,
                action: "SETTING_UPDATE",
                entity: "AppSetting",
                entityId: null,
                note: $"管理設定を更新しました。税率: {request.TaxRatePercent}% / 手数料: {request.AdminFeeRatePercent}%"
            );

            return Ok(result);
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