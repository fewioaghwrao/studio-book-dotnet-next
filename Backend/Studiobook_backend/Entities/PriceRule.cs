namespace Studiobook_backend.Entities
{
    public class PriceRule
    {
        public int Id { get; set; }

        public int RoomId { get; set; }

        // multiplier / flat_fee
        public string RuleType { get; set; } = string.Empty;

        // null = 全て, 0=日, 1=月, ... 6=土
        public int? Weekday { get; set; }

        public TimeOnly? StartHour { get; set; }

        public TimeOnly? EndHour { get; set; }

        public decimal? Multiplier { get; set; }

        public int? FlatFee { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public Room Room { get; set; } = null!;
    }
}