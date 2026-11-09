namespace Studiobook_backend.Application.Dtos.Ai;

/// <summary>AI 検索結果の空き状況。</summary>
public class AiRoomAvailabilityDto
{
    /// <summary>
    /// available / reserved / holiday / business_hours_not_set /
    /// outside_business_hours / closure / cross_day / past
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>表示名（例：空きあり、その時間は予約済み）。</summary>
    public string Label { get; set; } = string.Empty;

    public bool IsAvailable { get; set; }
}
