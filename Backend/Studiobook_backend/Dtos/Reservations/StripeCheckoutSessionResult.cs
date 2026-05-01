namespace Studiobook_backend.Dtos.Reservations;

public class StripeCheckoutSessionResult
{
    public string SessionId { get; set; } = string.Empty;

    public string CheckoutUrl { get; set; } = string.Empty;
}