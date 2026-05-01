namespace Studiobook_backend.Dtos.Reviews;

public class ReviewListItemDto
{
    public int Id { get; set; }

    public int Score { get; set; }

    public string Content { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}