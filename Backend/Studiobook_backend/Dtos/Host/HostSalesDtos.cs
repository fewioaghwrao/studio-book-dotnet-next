namespace Studiobook_backend.Dtos.Host
{
    public class HostSalesListResponseDto
    {
        public List<HostSalesRowDto> Items { get; set; } = new();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public List<HostSalesRoomOptionDto> RoomOptions { get; set; } = new();
    }

    public class HostSalesRowDto
    {
        public int ReservationId { get; set; }

        public int RoomId { get; set; }

        public string RoomName { get; set; } = string.Empty;

        public string GuestName { get; set; } = string.Empty;

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public int Amount { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool HasItems { get; set; }
    }

    public class HostSalesRoomOptionDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public class HostSalesDetailResponseDto
    {
        public int ReservationId { get; set; }

        public int RoomId { get; set; }

        public string RoomName { get; set; } = string.Empty;

        public string GuestName { get; set; } = string.Empty;

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public int Amount { get; set; }

        public string Status { get; set; } = string.Empty;

        public List<HostSalesItemDto> Items { get; set; } = new();
    }

    public class HostSalesItemDto
    {
        public string Kind { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime? SliceStart { get; set; }

        public DateTime? SliceEnd { get; set; }

        public int? UnitRatePerHour { get; set; }

        public int SliceAmount { get; set; }
    }
}