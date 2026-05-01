namespace Studiobook_backend.Dtos.Host
{
    public class HostReviewListResponseDto
    {
        public List<HostReviewRowDto> Items { get; set; } = new();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public List<HostReviewRoomOptionDto> RoomOptions { get; set; } = new();
    }

    public class HostReviewRowDto
    {
        public int Id { get; set; }

        public int RoomId { get; set; }

        public string RoomName { get; set; } = string.Empty;

        public int UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public int Score { get; set; }

        public string Content { get; set; } = string.Empty;

        public bool PublicVisible { get; set; }

        public string? HiddenReason { get; set; }

        public string? HostReply { get; set; }

        public DateTime? HostReplyAt { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public class HostReviewRoomOptionDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public class HostReviewReplyRequestDto
    {
        public string HostReply { get; set; } = string.Empty;
    }

    public class HostReviewVisibilityRequestDto
    {
        public bool IsPublic { get; set; }

        public string? Reason { get; set; }
    }
}