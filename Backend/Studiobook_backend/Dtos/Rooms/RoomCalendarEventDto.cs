namespace Studiobook_backend.Dtos.Rooms
{
    public class RoomCalendarEventDto
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public DateTimeOffset Start { get; set; }

        public DateTimeOffset End { get; set; }

        public bool AllDay { get; set; }

        // open / closure / reservation
        public string Type { get; set; } = string.Empty;
    }
}
