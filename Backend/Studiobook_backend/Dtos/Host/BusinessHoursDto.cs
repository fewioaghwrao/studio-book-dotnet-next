namespace Studiobook_backend.Dtos.Host
{
    public class BusinessHoursResponseDto
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public List<BusinessHourRowDto> Rows { get; set; } = new();
    }

    public class BusinessHoursUpdateRequestDto
    {
        public List<BusinessHourRowDto> Rows { get; set; } = new();
    }

    public class BusinessHourRowDto
    {
        public int DayOfWeek { get; set; }

        // "09:00" 形式で受け渡しする
        public string? StartTime { get; set; }

        public string? EndTime { get; set; }

        public bool IsHoliday { get; set; }
    }
}