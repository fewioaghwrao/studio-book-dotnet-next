namespace Studiobook_backend.Dtos.Rooms
{
    public class RoomBusinessHourDto
    {
        public int DayOfWeek { get; set; }

        public string? StartTime { get; set; }

        public string? EndTime { get; set; }

        public bool IsHoliday { get; set; }
    }
}