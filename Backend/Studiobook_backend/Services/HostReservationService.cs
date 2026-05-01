using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Host;

namespace Studiobook_backend.Services
{
    public class HostReservationService
    {
        private readonly AppDbContext _context;

        public HostReservationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<HostReservationListResponseDto> GetListAsync(
            int hostUserId,
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

            var query = _context.Reservations
                .AsNoTracking()
                .Include(x => x.Room)
                .Include(x => x.User)
                .Where(x => x.Room.UserId == hostUserId);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();

                query = query.Where(x =>
                    x.Room.Name.Contains(kw) ||
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

            if (roomId.HasValue)
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
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new HostReservationRowDto
                {
                    ReservationId = x.Id,
                    RoomId = x.RoomId,
                    RoomName = x.Room.Name,
                    GuestName = x.User.Name,
                    StartAt = x.StartAt,
                    EndAt = x.EndAt,
                    Amount = x.Amount,
                    Status = x.Status
                })
                .ToListAsync();

            var roomOptions = await _context.Rooms
                .AsNoTracking()
                .Where(x => x.UserId == hostUserId)
                .OrderBy(x => x.Name)
                .Select(x => new HostReservationRoomOptionDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            return new HostReservationListResponseDto
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                RoomOptions = roomOptions
            };
        }

        public async Task ApproveAsync(int hostUserId, int reservationId)
        {
            var reservation = await _context.Reservations
                .Include(x => x.Room)
                .FirstOrDefaultAsync(x =>
                    x.Id == reservationId &&
                    x.Room.UserId == hostUserId);

            if (reservation == null)
            {
                throw new KeyNotFoundException("予約が見つからないか、アクセス権限がありません。");
            }

            if (reservation.Status == "booked")
            {
                reservation.Status = "paid";
                reservation.UpdatedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task CancelAsync(int hostUserId, int reservationId)
        {
            var reservation = await _context.Reservations
                .Include(x => x.Room)
                .FirstOrDefaultAsync(x =>
                    x.Id == reservationId &&
                    x.Room.UserId == hostUserId);

            if (reservation == null)
            {
                throw new KeyNotFoundException("予約が見つからないか、アクセス権限がありません。");
            }

            if (reservation.Status == "booked")
            {
                reservation.Status = "canceled";
                reservation.UpdatedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}