namespace Studiobook_backend.Entities
{
    public class Reservation
    {
        public int Id { get; set; }

        public int RoomId { get; set; }

        public int UserId { get; set; }

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public int Amount { get; set; }

        // booked / paid / canceled
        public string Status { get; set; } = "booked";

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public Room Room { get; set; } = null!;

        public User User { get; set; } = null!;

        public ICollection<ReservationChargeItem> ChargeItems { get; set; } = new List<ReservationChargeItem>();
    }
}
