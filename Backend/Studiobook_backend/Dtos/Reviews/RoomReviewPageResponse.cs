namespace Studiobook_backend.Dtos.Reviews;

public class RoomReviewPageResponse
{
    public int RoomId { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public string? RoomImageName { get; set; }

    public string RoomAddress { get; set; } = string.Empty;

    public double AverageScore { get; set; }

    public int ReviewCount { get; set; }

    public List<ReviewListItemDto> Reviews { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public bool AlreadyReviewed { get; set; }

    public bool CanReview { get; set; }
}