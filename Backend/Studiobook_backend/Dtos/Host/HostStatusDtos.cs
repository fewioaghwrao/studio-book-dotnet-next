namespace Studiobook_backend.Dtos.Host
{
    public class HostStatusResponseDto
    {
        public List<string> Labels { get; set; } = new();

        public List<int> Booked { get; set; } = new();

        public List<int> Paid { get; set; } = new();

        public List<decimal?> UtilizationPercents { get; set; } = new();

        public double? ReviewAvgAny { get; set; }

        public double? ReviewAvgPublic { get; set; }

        public List<HostStatusRoomOptionDto> RoomOptions { get; set; } = new();
    }

    public class HostStatusRoomOptionDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}