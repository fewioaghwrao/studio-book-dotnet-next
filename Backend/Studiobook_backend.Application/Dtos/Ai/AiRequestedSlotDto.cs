namespace Studiobook_backend.Application.Dtos.Ai;

/// <summary>AI 検索で空き状況を判定した日時（日本時間の壁時計時刻）。</summary>
public class AiRequestedSlotDto
{
    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    /// <summary>利用時間の指定がなく、1 時間として判定した場合 true。</summary>
    public bool DurationAssumed { get; set; }
}
