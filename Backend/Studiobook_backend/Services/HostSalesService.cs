using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Entities;

namespace Studiobook_backend.Services
{
    public class HostSalesService
    {
        private readonly AppDbContext _context;

        public HostSalesService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<HostSalesListResponseDto> GetListAsync(
            int hostUserId,
            int? roomId,
            bool onlyWithItems,
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

            if (roomId.HasValue)
            {
                query = query.Where(x => x.RoomId == roomId.Value);
            }

            // 動的明細方式なので、予約があれば明細あり扱い
            if (onlyWithItems)
            {
                query = query.Where(x => x.Amount > 0);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.StartAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new HostSalesRowDto
                {
                    ReservationId = x.Id,
                    RoomId = x.RoomId,
                    RoomName = x.Room.Name,
                    GuestName = x.User.Name,
                    StartAt = x.StartAt,
                    EndAt = x.EndAt,
                    Amount = x.Amount,
                    Status = x.Status,
                    HasItems = x.Amount > 0
                })
                .ToListAsync();

            var roomOptions = await _context.Rooms
                .AsNoTracking()
                .Where(x => x.UserId == hostUserId)
                .OrderBy(x => x.Name)
                .Select(x => new HostSalesRoomOptionDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            return new HostSalesListResponseDto
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                RoomOptions = roomOptions
            };
        }

        public async Task<HostSalesDetailResponseDto> GetDetailAsync(
            int hostUserId,
            int reservationId)
        {
            var reservation = await _context.Reservations
                .AsNoTracking()
                .Include(x => x.Room)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.Id == reservationId &&
                    x.Room.UserId == hostUserId);

            if (reservation == null)
            {
                throw new KeyNotFoundException("売上明細が見つからないか、アクセス権限がありません。");
            }

            var items = await _context.ReservationChargeItems
                .AsNoTracking()
                .Where(x => x.ReservationId == reservationId)
                .OrderBy(x => x.SliceStart == null)
                .ThenBy(x => x.SliceStart)
                .ThenBy(x => x.Id)
                .Select(x => new HostSalesItemDto
                {
                    Kind = x.Kind,
                    Description = x.Description ?? "",
                    SliceStart = x.SliceStart,
                    SliceEnd = x.SliceEnd,
                    UnitRatePerHour = x.UnitRatePerHour,
                    SliceAmount = x.SliceAmount
                })
                .ToListAsync();

            return new HostSalesDetailResponseDto
            {
                ReservationId = reservation.Id,
                RoomId = reservation.RoomId,
                RoomName = reservation.Room.Name,
                GuestName = reservation.User.Name,
                StartAt = reservation.StartAt,
                EndAt = reservation.EndAt,
                Amount = reservation.Amount,
                Status = reservation.Status,
                Items = items
            };
        }


    }
}