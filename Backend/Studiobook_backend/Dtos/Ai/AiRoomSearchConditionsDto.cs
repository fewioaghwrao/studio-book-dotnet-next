namespace Studiobook_backend.Dtos.Ai
{
    public class AiRoomSearchConditionsDto
    {
        public string? Keyword { get; set; }

        public string? Area { get; set; }

        public int? Price { get; set; }

        public int? Capacity { get; set; }

        public string? CapacityCondition { get; set; }

        public string? Purpose { get; set; }

        public string? Atmosphere { get; set; }

        public string? TimePreference { get; set; }

        public List<string> Keywords { get; set; } = new();
    }
}