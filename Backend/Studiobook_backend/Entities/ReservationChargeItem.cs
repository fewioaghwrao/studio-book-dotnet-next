namespace Studiobook_backend.Entities
{
    public class ReservationChargeItem
    {
        public int Id { get; set; }

        public int ReservationId { get; set; }

        public string Kind { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int SliceAmount { get; set; }

        public DateTime? SliceStart { get; set; }

        public DateTime? SliceEnd { get; set; }

        public int? UnitRatePerHour { get; set; }

        public Reservation Reservation { get; set; } = null!;
    }
}