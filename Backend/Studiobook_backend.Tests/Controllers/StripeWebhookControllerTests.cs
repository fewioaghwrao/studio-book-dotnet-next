using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Services;
using Studiobook_backend.Settings;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Controllers;

public class StripeWebhookControllerTests
{
    private const string WebhookSecret = "whsec_test_secret";

    [Fact]
    public async Task Handle_ReturnsBadRequest_WhenSignatureIsInvalid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var json = """
        {
          "id": "evt_test_invalid_signature",
          "object": "event",
          "type": "payment_intent.succeeded",
          "data": {
            "object": {
              "id": "pi_test",
              "object": "payment_intent"
            }
          }
        }
        """;

        var controller = CreateController(context);
        SetRequestBody(controller, json);
        controller.Request.Headers["Stripe-Signature"] = "t=1234567890,v1=invalid_signature";

        // Act
        var result = await controller.Handle();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task Handle_ReturnsBadRequest_WhenJsonIsInvalid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var json = "{ invalid json ";

        var controller = CreateController(context);
        SetRequestBody(controller, json);
        controller.Request.Headers["Stripe-Signature"] =
            CreateStripeSignatureHeader(json, WebhookSecret);

        // Act
        var result = await controller.Handle();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task Handle_ReturnsOk_WhenEventTypeIsUnhandled()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var json = """
        {
          "id": "evt_unhandled",
          "object": "event",
          "api_version": "2024-06-20",
          "created": 1710000000,
          "livemode": false,
          "pending_webhooks": 1,
          "request": null,
          "type": "payment_intent.succeeded",
          "data": {
            "object": {
              "id": "pi_test",
              "object": "payment_intent"
            }
          }
        }
        """;

        var controller = CreateController(context);
        SetRequestBody(controller, json);
        controller.Request.Headers["Stripe-Signature"] =
            CreateStripeSignatureHeader(json, WebhookSecret);

        // Act
        var result = await controller.Handle();

        // Assert
        Assert.IsType<OkResult>(result);
    }

    private static StripeWebhookController CreateController(AppDbContext context)
    {
        var adminSettingsService = new AdminSettingsService(context);

        var reservationConfirmService = new ReservationConfirmService(
            context,
            adminSettingsService);

        var reservationCompleteService = new ReservationCompleteService(
            context,
            reservationConfirmService);

        var controller = new StripeWebhookController(
            Options.Create(new StripeSettings
            {
                PublishableKey = "pk_test_dummy",
                SecretKey = "sk_test_dummy",
                SuccessUrl = "https://frontend.example.com/success",
                CancelUrl = "https://frontend.example.com/rooms/{ROOM_ID}",
                WebhookSecret = WebhookSecret
            }),
            reservationCompleteService,
            NullLogger<StripeWebhookController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private static void SetRequestBody(ControllerBase controller, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        controller.HttpContext.Request.Body = new MemoryStream(bytes);
        controller.HttpContext.Request.ContentType = "application/json";
        controller.HttpContext.Request.ContentLength = bytes.Length;
    }

    private static string CreateStripeSignatureHeader(string payload, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{payload}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var signature = Convert.ToHexString(hash).ToLowerInvariant();

        return $"t={timestamp},v1={signature}";
    }
}