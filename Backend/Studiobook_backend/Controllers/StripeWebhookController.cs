using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using Studiobook_backend.Services;
using Studiobook_backend.Settings;

namespace Studiobook_backend.Controllers;

[ApiController]
[Route("api/stripe/webhook")]
public class StripeWebhookController : ControllerBase
{
    private readonly StripeSettings _stripeSettings;
    private readonly ReservationCompleteService _reservationCompleteService;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        IOptions<StripeSettings> stripeOptions,
        ReservationCompleteService reservationCompleteService,
        ILogger<StripeWebhookController> logger)
    {
        _stripeSettings = stripeOptions.Value;
        _reservationCompleteService = reservationCompleteService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Handle()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        _logger.LogInformation(
            "Stripe webhook received. BodyLength={BodyLength}, SignatureExists={SignatureExists}, WebhookSecretLength={WebhookSecretLength}",
            json.Length,
            Request.Headers.ContainsKey("Stripe-Signature"),
            _stripeSettings.WebhookSecret?.Length ?? 0
        );

        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _stripeSettings.WebhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(
                ex,
                "Stripe webhook signature verification failed. Message={Message}",
                ex.Message);

            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Stripe webhook parse failed. Message={Message}",
                ex.Message);

            return BadRequest(new
            {
                message = ex.Message
            });
        }

        _logger.LogInformation(
            "Stripe webhook verified. EventType={EventType}, EventId={EventId}",
            stripeEvent.Type,
            stripeEvent.Id);

        try
        {
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompletedAsync(stripeEvent);
                    break;

                default:
                    _logger.LogInformation(
                        "Unhandled Stripe event type: {EventType}",
                        stripeEvent.Type);
                    break;
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Stripe webhook handling failed. EventType={EventType}, EventId={EventId}",
                stripeEvent.Type,
                stripeEvent.Id);

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent)
    {
        var sessionId = GetCheckoutSessionId(stripeEvent);

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new InvalidOperationException("checkout.session.completed から sessionId を取得できませんでした。");
        }

        var sessionService = new SessionService();

        var session = await sessionService.GetAsync(
            sessionId,
            new SessionGetOptions
            {
                Expand = new List<string>
                {
                    "payment_intent"
                }
            });

        var paymentIntent = session.PaymentIntent;

        if (paymentIntent == null)
        {
            throw new InvalidOperationException("Checkout Session から PaymentIntent を取得できませんでした。");
        }

        var metadata = paymentIntent.Metadata;

        if (metadata == null || metadata.Count == 0)
        {
            throw new InvalidOperationException("PaymentIntent metadata が空です。");
        }

        await _reservationCompleteService.CompleteFromStripeAsync(
            metadata,
            paymentIntent.Id,
            session.Id,
            paymentIntent.Amount);
    }

    private static string? GetCheckoutSessionId(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is Session session)
        {
            return session.Id;
        }

        return null;
    }
}