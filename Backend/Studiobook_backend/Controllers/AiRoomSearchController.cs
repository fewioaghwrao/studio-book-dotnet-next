using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Studiobook_backend.Dtos.Ai;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers;

[ApiController]
[Route("api/ai/room-search")]
[EnableRateLimiting("AiSearchPolicy")]
public class AiRoomSearchController : ControllerBase
{
    private readonly AiRoomSearchService _service;
    private readonly AiSearchLogService _logService;

    public AiRoomSearchController(
        AiRoomSearchService service,
        AiSearchLogService logService)
    {
        _service = service;
        _logService = logService;
    }

    [HttpPost]
    public async Task<ActionResult<AiRoomSearchResponse>> Search(
        [FromBody] AiRoomSearchRequest request)
    {
        var query = request.Query?.Trim() ?? string.Empty;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userId = GetCurrentUserId();

        try
        {
            var result = await _service.SearchAsync(query);

            await _logService.WriteAsync(
                query: query,
                ipAddress: ipAddress,
                userId: userId,
                succeeded: true,
                resultCount: result.Rooms.Count
            );

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            await _logService.WriteAsync(
                query: query,
                ipAddress: ipAddress,
                userId: userId,
                succeeded: false,
                resultCount: 0,
                errorMessage: ex.Message
            );

            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            await _logService.WriteAsync(
                query: query,
                ipAddress: ipAddress,
                userId: userId,
                succeeded: false,
                resultCount: 0,
                errorMessage: ex.Message
            );

            return StatusCode(500, new
            {
                message = "AI検索の実行中にエラーが発生しました。",
                detail = ex.Message
            });
        }
    }

    private int? GetCurrentUserId()
    {
        var value =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("userId") ??
            User.FindFirstValue("id");

        return int.TryParse(value, out var userId)
            ? userId
            : null;
    }
}