using Studiobook_backend.Application.Dtos.Reservations;

namespace Studiobook_backend.Application.Interfaces;

/// <summary>
/// 決済（Stripe Checkout）セッションを作成する外部サービス。
/// </summary>
public interface IStripeCheckoutService
{
    string PublishableKey { get; }

    Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(
        ReservationConfirmResponse confirm,
        int userId);
}
