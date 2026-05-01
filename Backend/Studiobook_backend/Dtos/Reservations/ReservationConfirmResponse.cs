namespace Studiobook_backend.Dtos.Reservations;

public class ReservationConfirmResponse
{
    public int RoomId { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public int HourlyPrice { get; set; }

    public decimal Hours { get; set; }

    public int Subtotal { get; set; }

    public decimal TaxRatePercent { get; set; }

    public int? Tax { get; set; }

    public decimal PlatformFeeRatePercent { get; set; }

    public int? PlatformFee { get; set; }

    public int Amount { get; set; }

    public List<ReservationConfirmItemDto> Items { get; set; } = new();

    public string StripePublishableKey { get; set; } = string.Empty;

    public string SessionId { get; set; } = string.Empty;

    public string CheckoutUrl { get; set; } = string.Empty;
}