using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Ai;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers;

[ApiController]
[Route("api/ai/review-assist")]
public class AiReviewAssistController : ControllerBase
{
    private readonly AiReviewAssistService _service;

    public AiReviewAssistController(AiReviewAssistService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<AiReviewAssistResponse>> Assist(
        [FromBody] AiReviewAssistRequest request)
    {
        try
        {
            var result = await _service.AssistAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new
            {
                message = "AIレビュー文補助の実行中にエラーが発生しました。",
                detail = ex.Message
            });
        }
    }
}