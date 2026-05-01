namespace Studiobook_backend.Dtos.Admin;

public class AdminAiSearchLogListItemDto
{
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string Query { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public int? UserId { get; set; }

    public string? Model { get; set; }

    public bool Succeeded { get; set; }

    public int ResultCount { get; set; }

    public string? ErrorMessage { get; set; }
}