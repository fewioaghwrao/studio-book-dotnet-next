namespace Studiobook_backend.Dtos.Admin
{
    public class AdminRoomListResponseDto
    {
        public List<AdminRoomListItemDto> Items { get; set; } = new();

        public string Keyword { get; set; } = string.Empty;

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }
    }
}