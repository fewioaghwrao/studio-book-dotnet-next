using Microsoft.Extensions.Options;
using Stripe.Checkout;
using Studiobook_backend.Dtos.Reservations;
using Studiobook_backend.Settings;

namespace Studiobook_backend.Services;

public class StripeCheckoutService
{
    private readonly StripeSettings _settings;

    public StripeCheckoutService(IOptions<StripeSettings> options)
    {
        _settings = options.Value;
    }

    public string PublishableKey => _settings.PublishableKey;

    public async Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(
        ReservationConfirmResponse confirm,
        int userId)
    {
        var successUrl = _settings.SuccessUrl;
        var cancelUrl = _settings.CancelUrl.Replace("{ROOM_ID}", confirm.RoomId.ToString());

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            PaymentMethodTypes = new List<string>
            {
                "card"
            },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "jpy",
                        UnitAmount = confirm.Amount,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{confirm.RoomName} 予約",
                            Description = $"{confirm.StartAt:yyyy/MM/dd HH:mm} - {confirm.EndAt:HH:mm}"
                        }
                    }
                }
            },

            // Webhook で予約情報を復元するため、PaymentIntent 側に metadata を入れる
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId.ToString(),
                    ["roomId"] = confirm.RoomId.ToString(),
                    ["startAt"] = confirm.StartAt.ToString("yyyy-MM-ddTHH:mm"),
                    ["endAt"] = confirm.EndAt.ToString("yyyy-MM-ddTHH:mm"),

                    ["subtotal"] = confirm.Subtotal.ToString(),
                    ["taxRatePercent"] = confirm.TaxRatePercent.ToString(),
                    ["tax"] = (confirm.Tax ?? 0).ToString(),

                    ["platformFeeRatePercent"] = confirm.PlatformFeeRatePercent.ToString(),
                    ["platformFee"] = (confirm.PlatformFee ?? 0).ToString(),

                    ["amount"] = confirm.Amount.ToString(),
                    ["hourlyPrice"] = confirm.HourlyPrice.ToString(),
                    ["hours"] = confirm.Hours.ToString()
                }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return new StripeCheckoutSessionResult
        {
            SessionId = session.Id,
            CheckoutUrl = session.Url
        };
    }
}