using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Admin;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers;

[ApiController]
[Route("api/admin/ai-search-logs")]
[Authorize(Roles = "Admin")]
public class AdminAiSearchLogsController : ControllerBase
{
    private readonly AdminAiSearchLogService _service;

    public AdminAiSearchLogsController(AdminAiSearchLogService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<AdminAiSearchLogListResponse>> GetList(
        [FromQuery] string? q,
        [FromQuery] int? userId,
        [FromQuery] string? ipAddress,
        [FromQuery] bool? succeeded,
        [FromQuery] string? model,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetListAsync(
            q,
            userId,
            ipAddress,
            succeeded,
            model,
            from,
            to,
            page,
            pageSize);

        return Ok(result);
    }
}