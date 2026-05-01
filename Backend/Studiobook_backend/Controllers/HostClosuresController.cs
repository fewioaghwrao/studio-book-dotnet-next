using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers;

[ApiController]
[Authorize(Roles = "Host")]
[Route("api/host/rooms/{roomId:int}/closures")]
public class HostClosuresController : ControllerBase
{
    private readonly HostClosureService _service;

    public HostClosuresController(HostClosureService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<ClosureDto>>> GetList(int roomId)
    {
        var userId = GetUserId();
        var result = await _service.ListAsync(roomId, userId);
        return Ok(result);
    }

    [HttpGet("events")]
    public async Task<ActionResult<List<ClosureEventDto>>> GetEvents(int roomId)
    {
        var userId = GetUserId();
        var result = await _service.EventsAsync(roomId, userId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ClosureDto>> Create(
        int roomId,
        [FromBody] CreateClosureRequest request)
    {
        var userId = GetUserId();
        var result = await _service.CreateAsync(roomId, userId, request);
        return CreatedAtAction(nameof(GetList), new { roomId }, result);
    }

    [HttpDelete("{closureId:int}")]
    public async Task<IActionResult> Delete(int roomId, int closureId)
    {
        var userId = GetUserId();
        await _service.DeleteAsync(roomId, closureId, userId);
        return NoContent();
    }

    private int GetUserId()
    {
        var userIdText =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("id");

        if (!int.TryParse(userIdText, out var userId))
        {
            throw new UnauthorizedAccessException("ログインユーザーを特定できません。");
        }

        return userId;
    }
}
