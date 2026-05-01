using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Reservations;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly ReservationConfirmService _reservationConfirmService;
    private readonly StripeCheckoutService _stripeCheckoutService;
    private readonly UserReservationService _userReservationService;

    public ReservationsController(
        ReservationConfirmService reservationConfirmService,
        StripeCheckoutService stripeCheckoutService,
        UserReservationService userReservationService)
    {
        _reservationConfirmService = reservationConfirmService;
        _stripeCheckoutService = stripeCheckoutService;
        _userReservationService = userReservationService;
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ReservationConfirmRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized(new
            {
                message = "ログインが必要です。"
            });
        }

        if (!User.IsInRole("GeneralUser"))
        {
            return Forbid();
        }

        try
        {
            var confirm = await _reservationConfirmService.BuildConfirmAsync(
                userId.Value,
                request);

            var checkoutSession = await _stripeCheckoutService.CreateCheckoutSessionAsync(
                confirm,
                userId.Value);

            confirm.StripePublishableKey = _stripeCheckoutService.PublishableKey;
            confirm.SessionId = checkoutSession.SessionId;
            confirm.CheckoutUrl = checkoutSession.CheckoutUrl;

            return Ok(confirm);
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

    [HttpGet]
    public async Task<IActionResult> GetMyReservations(
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

        if (!User.IsInRole("GeneralUser"))
        {
            return Forbid();
        }

        var result = await _userReservationService.GetMyReservationsAsync(
            userId.Value,
            page,
            pageSize);

        return Ok(result);
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