namespace Studiobook_backend.Dtos.Rooms
{
    public class RoomCalendarEventDto
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public bool AllDay { get; set; }

        // open / closure / reservation
        public string Type { get; set; } = string.Empty;
    }
}
