namespace Studiobook_backend.Domain.Entities
{
    public class Reservation
    {
        public int Id { get; set; }

        public int RoomId { get; set; }

        public int UserId { get; set; }

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public int Amount { get; set; }

        // booked / paid / canceled（ReservationStatuses を参照）
        public string Status { get; set; } = "booked";

        /// <summary>
        /// Stripe の PaymentIntent ID（決済で登録された予約のみ）。
        /// 一意制約により、同じ決済の Webhook が再送されても予約を二重に作らない。
        /// </summary>
        public string? PaymentIntentId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public Room Room { get; set; } = null!;

        public User User { get; set; } = null!;

        public ICollection<ReservationChargeItem> ChargeItems { get; set; } = new List<ReservationChargeItem>();
    }
}
