using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Reservations;

namespace Studiobook_backend.Services;

public class UserReservationService
{
    private readonly AppDbContext _db;

    public UserReservationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserReservationListResponse> GetMyReservationsAsync(
        int userId,
        int page,
        int pageSize)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 10;
        }

        if (pageSize > 50)
        {
            pageSize = 50;
        }

        var query = _db.Reservations
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.StartAt)
            .ThenByDescending(x => x.Id);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserReservationListItemDto
            {
                ReservationId = x.Id,
                RoomId = x.RoomId,
                RoomName = x.Room.Name,
                RoomImageName = x.Room.ImageName,
                RoomAddress = x.Room.Address,
                StartAt = x.StartAt,
                EndAt = x.EndAt,
                Amount = x.Amount,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        var totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new UserReservationListResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}