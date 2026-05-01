namespace Studiobook_backend.Dtos.Admin
{
    public class AdminStatusResponseDto
    {
        public List<string> Labels { get; set; } = new();

        public List<int> Booked { get; set; } = new();

        public List<int> Paid { get; set; } = new();

        public List<decimal?> UtilizationPercents { get; set; } = new();

        public double? ReviewAvgAny { get; set; }

        public double? ReviewAvgPublic { get; set; }

        public int TotalRoomCount { get; set; }

        public int TotalHostCount { get; set; }

        public int TotalReservationCount { get; set; }

        public int TotalPaidAmount { get; set; }

        public List<AdminStatusRoomOptionDto> RoomOptions { get; set; } = new();
    }

    public class AdminStatusRoomOptionDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}