using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Application.Dtos.Reservations;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Domain.Entities;

namespace Studiobook_backend.Application.Services;

/// <summary>
/// Stripe の決済完了（checkout.session.completed）から予約を登録する。
///
/// - 冪等：同じ PaymentIntent の予約は 1 件だけ（再送・同時受信は「処理済み」として成功扱い）
/// - 同時予約：スタジオ単位のロック内で「重複確認 → 登録」を行う
/// - 予約できない場合（時間帯が埋まった・料金が変わった等）は全額返金し、Webhook には成功を返して再送を止める
/// - DB 障害やロック取得のタイムアウトなど一時的な失敗は例外のまま返し、Stripe の再送で再試行する
/// </summary>
public class ReservationCompleteService
{
    private const int AuditNoteMaxLength = 500;

    private readonly IAppDbContext _db;
    private readonly ReservationConfirmService _reservationConfirmService;
    private readonly IRoomReservationLock _roomLock;
    private readonly IPaymentRefundService _refundService;
    private readonly AuditLogService _auditLogService;
    private readonly ILogger<ReservationCompleteService> _logger;

    public ReservationCompleteService(
        IAppDbContext db,
        ReservationConfirmService reservationConfirmService,
        IRoomReservationLock roomLock,
        IPaymentRefundService refundService,
        AuditLogService auditLogService,
        ILogger<ReservationCompleteService> logger)
    {
        _db = db;
        _reservationConfirmService = reservationConfirmService;
        _roomLock = roomLock;
        _refundService = refundService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<ReservationCompleteResult> CompleteFromStripeAsync(
        IReadOnlyDictionary<string, string> metadata,
        string paymentIntentId,
        string? checkoutSessionId,
        long? paidAmount)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId))
        {
            throw new InvalidOperationException("PaymentIntent ID がありません。");
        }

        // 1. 同じ決済の予約が登録済みなら何もしない（Webhook の再送）
        var existingId = await FindReservationIdAsync(paymentIntentId);

        if (existingId != null)
        {
            return new ReservationCompleteResult(ReservationCompleteStatus.AlreadyProcessed, existingId);
        }

        // 2. metadata を読む。読めない場合も決済は完了しているため返金する
        int roomId;
        int userId;
        DateTime startAt;
        DateTime endAt;
        int amount;

        try
        {
            roomId = GetRequiredInt(metadata, "roomId");
            userId = GetRequiredInt(metadata, "userId");
            startAt = GetRequiredDateTime(metadata, "startAt");
            endAt = GetRequiredDateTime(metadata, "endAt");
            amount = GetRequiredInt(metadata, "amount");

            if (paidAmount != null && paidAmount.Value != amount)
            {
                throw new InvalidOperationException(
                    $"Stripeの支払金額と予約金額が一致しません。paidAmount={paidAmount.Value}, amount={amount}");
            }
        }
        catch (InvalidOperationException ex)
        {
            return await RefundAsync(paymentIntentId, actorId: null, ex.Message);
        }

        // 3. 同じスタジオの登録を直列化し、その中で重複確認と登録を行う
        await using var roomLock = await _roomLock.AcquireAsync(roomId);

        // ロック待ちの間に、同じ決済の別の受信が登録している場合がある
        existingId = await FindReservationIdAsync(paymentIntentId);

        if (existingId != null)
        {
            return new ReservationCompleteResult(ReservationCompleteStatus.AlreadyProcessed, existingId);
        }

        // 決済完了時点で再計算・再検証する（フロントや metadata だけを信用しない）
        ReservationConfirmResponse confirm;

        try
        {
            confirm = await _reservationConfirmService.BuildConfirmAsync(
                userId,
                new ReservationConfirmRequest
                {
                    RoomId = roomId,
                    StartAt = startAt,
                    EndAt = endAt
                },
                allowPastStart: true);

            if (confirm.Amount != amount)
            {
                throw new InvalidOperationException(
                    $"予約金額が再計算結果と一致しません。metadataAmount={amount}, recalculatedAmount={confirm.Amount}");
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return await RefundAsync(paymentIntentId, userId, ex.Message);
        }

        var reservation = BuildReservation(roomId, userId, startAt, endAt, paymentIntentId, confirm);

        _db.Reservations.Add(reservation);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // PaymentIntentId の一意制約違反（別サーバーが同じ決済を先に登録）なら処理済みとして扱う
            _db.ReservationChargeItems.RemoveRange(reservation.ChargeItems);
            _db.Reservations.Remove(reservation);

            existingId = await FindReservationIdAsync(paymentIntentId);

            if (existingId != null)
            {
                return new ReservationCompleteResult(ReservationCompleteStatus.AlreadyProcessed, existingId);
            }

            throw;
        }

        await _auditLogService.WriteAsync(
            userId,
            "RESERVATION_PAID",
            "Reservation",
            reservation.Id,
            Truncate($"Stripe決済で予約を登録しました。PaymentIntent={paymentIntentId}"));

        return new ReservationCompleteResult(ReservationCompleteStatus.Created, reservation.Id);
    }

    private async Task<int?> FindReservationIdAsync(string paymentIntentId)
    {
        return await _db.Reservations
            .AsNoTracking()
            .Where(x => x.PaymentIntentId == paymentIntentId)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<ReservationCompleteResult> RefundAsync(
        string paymentIntentId,
        int? actorId,
        string reason)
    {
        _logger.LogWarning(
            "Reservation could not be created from Stripe payment. Refunding. PaymentIntent={PaymentIntentId}, Reason={Reason}",
            paymentIntentId,
            reason);

        // 返金に失敗した場合は例外のまま返し、Stripe の再送で再試行する（返金は二重に行われない）
        await _refundService.RefundAsync(paymentIntentId, reason);

        await _auditLogService.WriteAsync(
            actorId,
            "STRIPE_REFUND",
            "Reservation",
            null,
            Truncate($"予約を登録できなかったため全額返金しました。PaymentIntent={paymentIntentId} 理由={reason}"));

        return new ReservationCompleteResult(ReservationCompleteStatus.Refunded, RefundReason: reason);
    }

    private static Reservation BuildReservation(
        int roomId,
        int userId,
        DateTime startAt,
        DateTime endAt,
        string paymentIntentId,
        ReservationConfirmResponse confirm)
    {
        var now = DateTime.UtcNow;

        var reservation = new Reservation
        {
            RoomId = roomId,
            UserId = userId,
            StartAt = startAt,
            EndAt = endAt,
            Amount = confirm.Amount,
            Status = ReservationStatuses.Paid,
            PaymentIntentId = paymentIntentId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        foreach (var item in confirm.Items)
        {
            reservation.ChargeItems.Add(new ReservationChargeItem
            {
                Kind = ToChargeItemKind(item.Label),
                Description = item.Label,
                SliceAmount = item.Amount,
                SliceStart = item.SliceStart,
                SliceEnd = item.SliceEnd,
                UnitRatePerHour = item.UnitRatePerHour
            });
        }

        if (confirm.Tax != null && confirm.Tax.Value > 0)
        {
            reservation.ChargeItems.Add(new ReservationChargeItem
            {
                Kind = "tax",
                Description = $"消費税（{confirm.TaxRatePercent:0.##}%）",
                SliceAmount = confirm.Tax.Value,
                SliceStart = null,
                SliceEnd = null,
                UnitRatePerHour = null
            });
        }

        if (confirm.PlatformFee != null && confirm.PlatformFee.Value > 0)
        {
            reservation.ChargeItems.Add(new ReservationChargeItem
            {
                Kind = "platform_fee",
                Description = $"プラットフォーム使用料（{confirm.PlatformFeeRatePercent:0.##}%）",
                SliceAmount = confirm.PlatformFee.Value,
                SliceStart = null,
                SliceEnd = null,
                UnitRatePerHour = null
            });
        }

        return reservation;
    }

    private static string Truncate(string value)
    {
        return value.Length <= AuditNoteMaxLength ? value : value[..AuditNoteMaxLength];
    }

    private static string ToChargeItemKind(string label)
    {
        if (label.Contains("通常"))
        {
            return "base";
        }

        if (label.Contains("料金ルール"))
        {
            return "price_rule";
        }

        if (label.Contains("夜間"))
        {
            return "price_rule";
        }

        return "other";
    }

    private static int GetRequiredInt(
        IReadOnlyDictionary<string, string> metadata,
        string key)
    {
        if (!metadata.TryGetValue(key, out var value) ||
            string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Stripe metadata に {key} がありません。");
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            throw new InvalidOperationException($"Stripe metadata の {key} が数値ではありません。value={value}");
        }

        return result;
    }

    private static DateTime GetRequiredDateTime(
        IReadOnlyDictionary<string, string> metadata,
        string key)
    {
        if (!metadata.TryGetValue(key, out var value) ||
            string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Stripe metadata に {key} がありません。");
        }

        if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var result))
        {
            return result;
        }

        throw new InvalidOperationException($"Stripe metadata の {key} が日時ではありません。value={value}");
    }
}