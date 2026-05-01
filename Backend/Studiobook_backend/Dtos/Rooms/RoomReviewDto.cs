namespace Studiobook_backend.Dtos.Rooms
{
    public class RoomReviewDto
    {
        public int Id { get; set; }

        public int? Score { get; set; }

        public string Content { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string? HostReply { get; set; }

        public DateTime? HostReplyAt { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}