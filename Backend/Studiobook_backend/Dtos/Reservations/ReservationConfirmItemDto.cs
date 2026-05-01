namespace Studiobook_backend.Dtos.Reservations;

public class ReservationConfirmItemDto
{
    public string Label { get; set; } = string.Empty;

    public int Amount { get; set; }

    public DateTime? SliceStart { get; set; }

    public DateTime? SliceEnd { get; set; }

    public int? UnitRatePerHour { get; set; }
}