namespace Studiobook_backend.Application.Dtos.Ai
{
    public class AiRoomSearchConditionsDto
    {
        public string? Keyword { get; set; }

        public string? Area { get; set; }

        public int? Price { get; set; }

        public int? Capacity { get; set; }

        public string? CapacityCondition { get; set; }

        public string? Purpose { get; set; }

        public string? Atmosphere { get; set; }

        public string? TimePreference { get; set; }

        /// <summary>利用日（yyyy-MM-dd、日本時間）。「来週土曜」などは AI が今日の日付から解決する。</summary>
        public string? Date { get; set; }

        /// <summary>開始時刻（HH:mm）。</summary>
        public string? StartTime { get; set; }

        /// <summary>終了時刻（HH:mm）。</summary>
        public string? EndTime { get; set; }

        /// <summary>利用時間（分）。「2時間」なら 120。</summary>
        public int? DurationMinutes { get; set; }

        public List<string> Keywords { get; set; } = new();
    }
}