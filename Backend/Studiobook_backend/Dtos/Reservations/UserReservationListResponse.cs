namespace Studiobook_backend.Dtos.Reservations;

public class UserReservationListResponse
{
    public List<UserReservationListItemDto> Items { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}