using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Admin;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/logs")]
    public class AdminAuditLogsController : ControllerBase
    {
        private readonly AdminAuditLogService _service;

        public AdminAuditLogsController(AdminAuditLogService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<AdminAuditLogListResponseDto>> GetList(
            [FromQuery] string? q,
            [FromQuery] int? actorId,
            [FromQuery] string? action,
            [FromQuery] string? entity,
            [FromQuery] int? entityId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetListAsync(
                q,
                actorId,
                action,
                entity,
                entityId,
                from,
                to,
                page,
                pageSize
            );

            return Ok(result);
        }
    }
}