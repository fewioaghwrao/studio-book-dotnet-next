namespace Studiobook_backend.Entities
{
    public class Review
    {
        public int Id { get; set; }

        public int RoomId { get; set; }

        public int UserId { get; set; }

        public int Score { get; set; }

        public string Content { get; set; } = string.Empty;

        public bool PublicVisible { get; set; } = true;

        public string? HiddenReason { get; set; }

        public string? HostReply { get; set; }

        public DateTime? HostReplyAt { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public Room Room { get; set; } = null!;

        public User User { get; set; } = null!;
    }
}