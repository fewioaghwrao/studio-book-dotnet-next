namespace Studiobook_backend.Dtos.Host
{
    public class ClosureDto
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string? Reason { get; set; }
    }
}
