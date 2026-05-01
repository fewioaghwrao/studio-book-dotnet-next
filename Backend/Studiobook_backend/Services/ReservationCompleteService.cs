using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;

namespace Studiobook_backend.Services;

public class ReservationCompleteService
{
    private readonly AppDbContext _db;
    private readonly ReservationConfirmService _reservationConfirmService;

    public ReservationCompleteService(
        AppDbContext db,
        ReservationConfirmService reservationConfirmService)
    {
        _db = db;
        _reservationConfirmService = reservationConfirmService;
    }

    public async Task CompleteFromStripeAsync(
        IReadOnlyDictionary<string, string> metadata,
        string paymentIntentId,
        string? checkoutSessionId,
        long? paidAmount)
    {
        var roomId = GetRequiredInt(metadata, "roomId");
        var userId = GetRequiredInt(metadata, "userId");
        var startAt = GetRequiredDateTime(metadata, "startAt");
        var endAt = GetRequiredDateTime(metadata, "endAt");
        var amount = GetRequiredInt(metadata, "amount");

        if (paidAmount != null && paidAmount.Value != amount)
        {
            throw new InvalidOperationException(
                $"Stripeの支払金額と予約金額が一致しません。paidAmount={paidAmount.Value}, amount={amount}");
        }

        // すでに同じ条件の paid/booked 予約がある場合は二重登録しない。
        // 本来は PaymentIntentId を Reservations に持たせるのがベストだが、
        // 現在の Reservation エンティティには無いため、room/user/time/amount で暫定的に冪等化する。
        var alreadyExists = await _db.Reservations.AnyAsync(x =>
            x.RoomId == roomId &&
            x.UserId == userId &&
            x.StartAt == startAt &&
            x.EndAt == endAt &&
            x.Amount == amount &&
            x.Status != "canceled");

        if (alreadyExists)
        {
            return;
        }

        // 決済完了時点で再計算・再検証する。
        // これにより、フロントやStripe metadataだけを信用せずに済む。
        var confirm = await _reservationConfirmService.BuildConfirmAsync(
            userId,
            new Studiobook_backend.Dtos.Reservations.ReservationConfirmRequest
            {
                RoomId = roomId,
                StartAt = startAt,
                EndAt = endAt
            });

        if (confirm.Amount != amount)
        {
            throw new InvalidOperationException(
                $"予約金額が再計算結果と一致しません。metadataAmount={amount}, recalculatedAmount={confirm.Amount}");
        }

        var now = DateTime.UtcNow;

        var reservation = new Reservation
        {
            RoomId = roomId,
            UserId = userId,
            StartAt = startAt,
            EndAt = endAt,
            Amount = confirm.Amount,
            Status = "paid",
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

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();
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