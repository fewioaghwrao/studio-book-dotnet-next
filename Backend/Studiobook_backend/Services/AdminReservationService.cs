using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Admin;

namespace Studiobook_backend.Services
{
    public class AdminReservationService
    {
        private readonly AppDbContext _context;

        public AdminReservationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdminReservationListResponseDto> GetListAsync(
            string? keyword,
            string? status,
            int? reservationId,
            int? roomId,
            DateTime? startFrom,
            DateTime? startTo,
            int page,
            int pageSize)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;
            pageSize = Math.Min(pageSize, 100);

            var query = _context.Reservations
                .AsNoTracking()
                .Include(x => x.Room)
                    .ThenInclude(x => x.User)
                .Include(x => x.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();

                query = query.Where(x =>
                    x.Room.Name.Contains(kw) ||
                    x.Room.User.Name.Contains(kw) ||
                    x.User.Name.Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            if (reservationId.HasValue)
            {
                query = query.Where(x => x.Id == reservationId.Value);
            }

            if (roomId.HasValue && roomId.Value > 0)
            {
                query = query.Where(x => x.RoomId == roomId.Value);
            }

            if (startFrom.HasValue)
            {
                query = query.Where(x => x.StartAt >= startFrom.Value.Date);
            }

            if (startTo.HasValue)
            {
                var to = startTo.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(x => x.StartAt <= to);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.StartAt)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AdminReservationRowDto
                {
                    ReservationId = x.Id,
                    RoomId = x.RoomId,
                    RoomName = x.Room.Name,
                    HostUserId = x.Room.UserId,
                    HostName = x.Room.User.Name,
                    GuestUserId = x.UserId,
                    GuestName = x.User.Name,
                    StartAt = x.StartAt,
                    EndAt = x.EndAt,
                    Amount = x.Amount,
                    Status = x.Status
                })
                .ToListAsync();

            var roomOptions = await _context.Rooms
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new AdminReservationRoomOptionDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            var totalPages = totalCount == 0
                ? 1
                : (int)Math.Ceiling(totalCount / (double)pageSize);

            return new AdminReservationListResponseDto
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                RoomOptions = roomOptions
            };
        }
    }
}