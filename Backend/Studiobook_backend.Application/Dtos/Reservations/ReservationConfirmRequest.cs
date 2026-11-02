namespace Studiobook_backend.Application.Dtos.Reservations;

public class ReservationConfirmRequest
{
    public int RoomId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }
}