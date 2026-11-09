namespace Studiobook_backend.Application.Dtos.Ai
{
    public class AiRoomSearchResultDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? ImageName { get; set; }

        public string Description { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public int Price { get; set; }

        public int Capacity { get; set; }

        public double? AverageScore { get; set; }

        public int ReviewCount { get; set; }

        /// <summary>おすすめ理由（Reasons を「。」でつないだ文章）。</summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>おすすめ理由（実際に当てはまった条件だけ）。</summary>
        public List<string> Reasons { get; set; } = new();

        /// <summary>指定日時の空き状況。日時の指定がない検索では null。</summary>
        public AiRoomAvailabilityDto? Availability { get; set; }
    }
}