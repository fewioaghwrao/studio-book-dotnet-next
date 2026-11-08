namespace Studiobook_backend.Application.Dtos.Reservations;

/// <summary>決済完了（Webhook）処理の結果。</summary>
public enum ReservationCompleteStatus
{
    /// <summary>予約を登録した。</summary>
    Created,

    /// <summary>同じ決済の予約が登録済みだった（Webhook の再送・同時受信）。</summary>
    AlreadyProcessed,

    /// <summary>予約を登録できなかったため全額返金した（同時予約で時間帯が埋まった等）。</summary>
    Refunded
}

public sealed record ReservationCompleteResult(
    ReservationCompleteStatus Status,
    int? ReservationId = null,
    string? RefundReason = null);
