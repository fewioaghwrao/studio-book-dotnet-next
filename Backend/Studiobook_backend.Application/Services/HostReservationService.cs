using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Application.Common;
using Studiobook_backend.Domain.Entities;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Application.Dtos.Host;

namespace Studiobook_backend.Application.Services
{
    public class HostReservationService
    {
        private readonly IAppDbContext _context;
        private readonly TimeProvider _timeProvider;

        public HostReservationService(IAppDbContext context, TimeProvider? timeProvider = null)
        {
            _context = context;
            _timeProvider = timeProvider ?? TimeProvider.System;
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

            var nowJapan = JapanTime.Now(_timeProvider);

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
                    Status = x.Status,
                    IsUsed = x.Status == ReservationStatuses.Paid && x.EndAt <= nowJapan
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

            // 予約済み（未決済）の予約を、ホストが決済済みとして承認する
            if (reservation.Status == ReservationStatuses.Booked)
            {
                reservation.Status = ReservationStatuses.Paid;
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

            // キャンセルできるのは未決済（予約済み）のみ。決済済みのキャンセル（返金）は未対応
            if (reservation.Status == ReservationStatuses.Booked)
            {
                reservation.Status = ReservationStatuses.Canceled;
                reservation.UpdatedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}