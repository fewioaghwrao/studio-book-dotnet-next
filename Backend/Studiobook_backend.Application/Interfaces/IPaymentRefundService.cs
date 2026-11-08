namespace Studiobook_backend.Application.Interfaces;

/// <summary>
/// 決済の返金（Stripe）。
/// 決済は完了したが予約を登録できなかった場合（同時予約で時間帯が埋まった等）に全額返金する。
/// </summary>
public interface IPaymentRefundService
{
    /// <summary>
    /// PaymentIntent を全額返金する。同じ PaymentIntent に対して何度呼んでも二重に返金しない。
    /// </summary>
    Task RefundAsync(string paymentIntentId, string reason, CancellationToken cancellationToken = default);
}
