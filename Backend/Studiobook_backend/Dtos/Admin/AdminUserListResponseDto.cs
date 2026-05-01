namespace Studiobook_backend.Dtos.Admin
{
    public class AdminUserListResponseDto
    {
        public List<AdminUserListItemDto> Items { get; set; } = new();

        public string Keyword { get; set; } = string.Empty;

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }
    }
}