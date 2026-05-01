namespace Studiobook_backend.Dtos.Reservations;

public class UserReservationListItemDto
{
    public int ReservationId { get; set; }

    public int RoomId { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public string? RoomImageName { get; set; }

    public string RoomAddress { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public int Amount { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
