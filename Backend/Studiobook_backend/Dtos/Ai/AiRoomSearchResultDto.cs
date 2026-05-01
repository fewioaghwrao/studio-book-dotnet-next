namespace Studiobook_backend.Dtos.Ai
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

        public string Reason { get; set; } = string.Empty;
    }
}