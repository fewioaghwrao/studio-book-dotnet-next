namespace Studiobook_backend.Dtos.Admin
{
    public class AdminReservationListResponseDto
    {
        public List<AdminReservationRowDto> Items { get; set; } = new();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public List<AdminReservationRoomOptionDto> RoomOptions { get; set; } = new();
    }

    public class AdminReservationRowDto
    {
        public int ReservationId { get; set; }

        public int RoomId { get; set; }

        public string RoomName { get; set; } = string.Empty;

        public int HostUserId { get; set; }

        public string HostName { get; set; } = string.Empty;

        public int GuestUserId { get; set; }

        public string GuestName { get; set; } = string.Empty;

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public int Amount { get; set; }

        public string Status { get; set; } = string.Empty;
    }

    public class AdminReservationRoomOptionDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}