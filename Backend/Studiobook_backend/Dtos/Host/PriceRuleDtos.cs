namespace Studiobook_backend.Dtos.Host
{
    public class PriceRulesResponseDto
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public List<PriceRuleDto> Rules { get; set; } = new();
    }

    public class CreatePriceRuleRequestDto
    {
        public string RuleType { get; set; } = string.Empty;

        // null = 全て, 0=日, 1=月, ... 6=土
        public int? Weekday { get; set; }

        public string? StartHour { get; set; }

        public string? EndHour { get; set; }

        public decimal? Multiplier { get; set; }

        public int? FlatFee { get; set; }

        public string? Note { get; set; }
    }

    public class PriceRuleDto
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