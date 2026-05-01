namespace Studiobook_backend.Entities
{
    public class BusinessHour
    {
        public int Id { get; set; }

        public int RoomId { get; set; }

        // 1:月, 2:火, 3:水, 4:木, 5:金, 6:土, 7:日
        public int DayOfWeek { get; set; }

        public TimeOnly? StartTime { get; set; }

        public TimeOnly? EndTime { get; set; }

        public bool IsHoliday { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public Room Room { get; set; } = null!;
    }
}
