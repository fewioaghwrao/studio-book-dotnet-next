namespace Studiobook_backend.Application.Dtos.Ai
{
    public class AiRoomSearchResponse
    {
        public string Query { get; set; } = string.Empty;

        public AiRoomSearchConditionsDto InterpretedConditions { get; set; } = new();

        public List<AiRoomSearchResultDto> Rooms { get; set; } = new();

        /// <summary>空き状況を判定した日時。日付と開始時刻が読み取れなかった場合は null。</summary>
        public AiRequestedSlotDto? RequestedSlot { get; set; }

        /// <summary>キーワードに一致するスタジオがなく、キーワード条件を外して探した場合 true。</summary>
        public bool Relaxed { get; set; }

        /// <summary>利用者への補足（条件を緩めた・利用時間を仮定した等）。</summary>
        public List<string> Notices { get; set; } = new();
    }
}