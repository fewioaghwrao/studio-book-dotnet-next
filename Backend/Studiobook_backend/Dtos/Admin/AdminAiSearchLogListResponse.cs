namespace Studiobook_backend.Dtos.Admin;

public class AdminAiSearchLogListResponse
{
    public List<AdminAiSearchLogListItemDto> Items { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}