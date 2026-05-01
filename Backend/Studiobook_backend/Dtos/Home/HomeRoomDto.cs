namespace Studiobook_backend.Dtos.Home
{
    public class HomeRoomDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public int Price { get; set; }

        public string? ImageName { get; set; }

        public double? AverageScore { get; set; }

        public int ReviewCount { get; set; }
    }
}