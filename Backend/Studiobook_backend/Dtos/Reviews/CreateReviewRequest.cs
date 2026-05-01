using System.ComponentModel.DataAnnotations;

namespace Studiobook_backend.Dtos.Reviews;

public class CreateReviewRequest
{
    public int? ReservationId { get; set; }

    [Range(1, 5, ErrorMessage = "評価は1〜5で指定してください。")]
    public int Score { get; set; }

    [Required(ErrorMessage = "コメントを入力してください。")]
    [MaxLength(1000, ErrorMessage = "コメントは1000文字以内で入力してください。")]
    public string Content { get; set; } = string.Empty;
}