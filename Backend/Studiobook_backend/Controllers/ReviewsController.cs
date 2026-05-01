using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Reviews;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers;

[ApiController]
[Route("api/rooms/{roomId:int}/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly ReviewService _reviewService;

    public ReviewsController(ReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet("new")]
    [Authorize]
    public async Task<IActionResult> GetNewReviewPage(
        int roomId,
        [FromQuery] int? reservationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized(new
            {
                message = "ログインが必要です。"
            });
        }

        try
        {
            var result = await _reviewService.GetRoomReviewPageAsync(
                roomId,
                userId.Value,
                page,
                pageSize,
                reservationId);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        int roomId,
        [FromBody] CreateReviewRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized(new
            {
                message = "ログインが必要です。"
            });
        }

        try
        {
            await _reviewService.CreateAsync(roomId, userId.Value, request);

            return Ok(new
            {
                message = "レビューを投稿しました。"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
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