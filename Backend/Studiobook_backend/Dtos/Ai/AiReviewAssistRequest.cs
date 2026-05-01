using System.ComponentModel.DataAnnotations;

namespace Studiobook_backend.Dtos.Ai;

public class AiReviewAssistRequest
{
    [Required(ErrorMessage = "レビュー本文を入力してください。")]
    [MaxLength(1000, ErrorMessage = "レビュー本文は1000文字以内で入力してください。")]
    public string Content { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "評価は1〜5で指定してください。")]
    public int Score { get; set; }

    [MaxLength(200, ErrorMessage = "スタジオ名は200文字以内で指定してください。")]
    public string? RoomName { get; set; }
}