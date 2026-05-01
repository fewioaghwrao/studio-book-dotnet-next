namespace Studiobook_backend.Dtos.Rooms
{
    public class RoomDetailDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? ImageName { get; set; }

        public string Description { get; set; } = string.Empty;

        public int Price { get; set; }

        public int Capacity { get; set; }

        public string PostalCode { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string HostName { get; set; } = string.Empty;

        public double? AverageScore { get; set; }

        public int ReviewCount { get; set; }

        public List<RoomBusinessHourDto> BusinessHours { get; set; } = new();

        public List<RoomPriceRuleDto> PriceRules { get; set; } = new();

        public List<RoomReviewDto> Reviews { get; set; } = new();

        public List<RoomReviewDto> HiddenHostReplies { get; set; } = new();

        public List<RoomCalendarEventDto> CalendarEvents { get; set; } = new();
    }
}