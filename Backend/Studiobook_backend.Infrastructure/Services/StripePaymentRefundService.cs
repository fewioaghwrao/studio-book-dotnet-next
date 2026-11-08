using Stripe;
using Studiobook_backend.Application.Interfaces;

namespace Studiobook_backend.Infrastructure.Services;

/// <summary>
/// Stripe の返金 API で PaymentIntent を全額返金する。
/// </summary>
public class StripePaymentRefundService : IPaymentRefundService
{
    public async Task RefundAsync(
        string paymentIntentId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var options = new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId,
            // 返金理由の詳細は監査ログに残す。冪等キーで同じ内容を送り直すため、ここには可変の値を入れない
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "studiobook-webhook",
                ["kind"] = "reservation-not-created"
            }
        };

        // 同じ PaymentIntent への返金は、Webhook の再送で何度呼ばれても 1 回だけ行う
        var requestOptions = new RequestOptions
        {
            IdempotencyKey = $"studiobook-refund-{paymentIntentId}"
        };

        try
        {
            await new RefundService().CreateAsync(options, requestOptions, cancellationToken);
        }
        catch (StripeException ex) when (ex.StripeError?.Code == "charge_already_refunded")
        {
            // 冪等キーの有効期限（24 時間）を過ぎた再送で、すでに返金済みの場合
        }
    }
}
