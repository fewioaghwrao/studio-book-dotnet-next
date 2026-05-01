namespace Studiobook_backend.Dtos.Rooms
{
    public class RoomPriceRuleDto
    {
        public int Id { get; set; }

        public string RuleType { get; set; } = string.Empty;

        public int? Weekday { get; set; }

        public string? StartHour { get; set; }

        public string? EndHour { get; set; }

        public decimal? Multiplier { get; set; }

        public int? FlatFee { get; set; }

        public string? Note { get; set; }
    }
}