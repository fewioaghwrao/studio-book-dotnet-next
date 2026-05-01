using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Admin;
using Studiobook_backend.Entities;

namespace Studiobook_backend.Services
{
    public class AdminStatusService
    {
        private readonly AppDbContext _context;

        public AdminStatusService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdminStatusResponseDto> GetAsync(
            int? roomId,
            DateTime baseMonth)
        {
            var targetRoomsQuery = _context.Rooms
                .AsNoTracking();

            if (roomId.HasValue && roomId.Value > 0)
            {
                targetRoomsQuery = targetRoomsQuery
                    .Where(x => x.Id == roomId.Value);
            }

            var targetRooms = await targetRoomsQuery
                .OrderBy(x => x.Id)
                .ToListAsync();

            var allRoomOptions = await _context.Rooms
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new AdminStatusRoomOptionDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            var months = BuildRecentMonths(baseMonth);

            if (targetRooms.Count == 0)
            {
                return new AdminStatusResponseDto
                {
                    Labels = BuildRecentMonthLabels(baseMonth),
                    Booked = new List<int> { 0, 0, 0 },
                    Paid = new List<int> { 0, 0, 0 },
                    UtilizationPercents = new List<decimal?> { null, null, null },
                    ReviewAvgAny = null,
                    ReviewAvgPublic = null,
                    TotalRoomCount = await _context.Rooms.CountAsync(),
                    TotalHostCount = await CountHostUsersAsync(),
                    TotalReservationCount = await _context.Reservations.CountAsync(),
                    TotalPaidAmount = await _context.Reservations
                        .Where(x => x.Status == "paid")
                        .SumAsync(x => (int?)x.Amount) ?? 0,
                    RoomOptions = allRoomOptions
                };
            }

            var roomIds = targetRooms.Select(x => x.Id).ToList();

            var firstMonthStart = months.First();
            var lastMonthEndExclusive = months.Last().AddMonths(1);

            var reservations = await _context.Reservations
                .AsNoTracking()
                .Where(x =>
                    roomIds.Contains(x.RoomId) &&
                    x.StartAt < lastMonthEndExclusive &&
                    x.EndAt >= firstMonthStart)
                .ToListAsync();

            var platformFeeItems = await _context.ReservationChargeItems
    .AsNoTracking()
    .Include(x => x.Reservation)
    .Where(x =>
        x.Kind == "platform_fee" &&
        roomIds.Contains(x.Reservation.RoomId) &&
        x.Reservation.StartAt < lastMonthEndExclusive &&
        x.Reservation.EndAt >= firstMonthStart)
    .ToListAsync();

            var businessHours = await _context.BusinessHours
                .AsNoTracking()
                .Where(x => roomIds.Contains(x.RoomId))
                .ToListAsync();

            var labels = new List<string>();
            var booked = new List<int>();
            var paid = new List<int>();
            var utilizationPercents = new List<decimal?>();

            foreach (var month in months)
            {
                var monthStart = month;
                var monthEnd = month.AddMonths(1);

                labels.Add(monthStart.ToString("yyyy-MM"));

                var bookedAmount = platformFeeItems
                    .Where(x =>
                        x.Reservation.Status == "booked" &&
                        x.Reservation.StartAt >= monthStart &&
                        x.Reservation.StartAt < monthEnd)
                    .Sum(x => x.SliceAmount);

                var paidAmount = platformFeeItems
                    .Where(x =>
                        x.Reservation.Status == "paid" &&
                        x.Reservation.StartAt >= monthStart &&
                        x.Reservation.StartAt < monthEnd)
                    .Sum(x => x.SliceAmount);

                booked.Add(bookedAmount);
                paid.Add(paidAmount);

                var availableHours = CalculateAvailableHours(
                    targetRooms,
                    businessHours,
                    monthStart,
                    monthEnd
                );

                var reservedHours = CalculateReservedHours(
                    reservations.Where(x => x.Status is "booked" or "paid"),
                    monthStart,
                    monthEnd
                );

                if (availableHours <= 0)
                {
                    utilizationPercents.Add(null);
                }
                else
                {
                    var percent = reservedHours / availableHours * 100m;

                    if (percent > 100m)
                    {
                        percent = 100m;
                    }

                    utilizationPercents.Add(
                        Math.Round(percent, 1, MidpointRounding.AwayFromZero)
                    );
                }
            }

            var reviewQuery = _context.Reviews
                .AsNoTracking()
                .Where(x => roomIds.Contains(x.RoomId));

            var reviewAvgAny = await reviewQuery
                .Select(x => (double?)x.Score)
                .AverageAsync();

            var reviewAvgPublic = await reviewQuery
                .Where(x => x.PublicVisible)
                .Select(x => (double?)x.Score)
                .AverageAsync();

            var totalRoomCount = await _context.Rooms.CountAsync();
            var totalHostCount = await CountHostUsersAsync();
            var totalReservationCount = await _context.Reservations.CountAsync();

            var totalPaidAmount = await _context.ReservationChargeItems
                .AsNoTracking()
                .Where(x =>
                    x.Kind == "platform_fee" &&
                    x.Reservation.Status == "paid")
                .SumAsync(x => (int?)x.SliceAmount) ?? 0;

            return new AdminStatusResponseDto
            {
                Labels = labels,
                Booked = booked,
                Paid = paid,
                UtilizationPercents = utilizationPercents,
                ReviewAvgAny = reviewAvgAny,
                ReviewAvgPublic = reviewAvgPublic,
                TotalRoomCount = totalRoomCount,
                TotalHostCount = totalHostCount,
                TotalReservationCount = totalReservationCount,
                TotalPaidAmount = totalPaidAmount,
                RoomOptions = allRoomOptions
            };
        }

        private async Task<int> CountHostUsersAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Host"))
                .CountAsync();
        }

        private static List<DateTime> BuildRecentMonths(DateTime baseMonth)
        {
            var targetMonth = new DateTime(baseMonth.Year, baseMonth.Month, 1);

            return new List<DateTime>
            {
                targetMonth.AddMonths(-2),
                targetMonth.AddMonths(-1),
                targetMonth
            };
        }

        private static List<string> BuildRecentMonthLabels(DateTime baseMonth)
        {
            return BuildRecentMonths(baseMonth)
                .Select(x => x.ToString("yyyy-MM"))
                .ToList();
        }

        private static decimal CalculateAvailableHours(
            List<Room> rooms,
            List<BusinessHour> businessHours,
            DateTime monthStart,
            DateTime monthEnd)
        {
            decimal total = 0m;

            var roomIds = rooms.Select(x => x.Id).ToHashSet();

            for (var date = monthStart.Date; date < monthEnd.Date; date = date.AddDays(1))
            {
                var dayOfWeek = ToBusinessDayOfWeek(date.DayOfWeek);

                foreach (var roomId in roomIds)
                {
                    var row = businessHours.FirstOrDefault(x =>
                        x.RoomId == roomId &&
                        x.DayOfWeek == dayOfWeek);

                    if (row == null || row.IsHoliday)
                    {
                        continue;
                    }

                    if (row.StartTime == null || row.EndTime == null)
                    {
                        continue;
                    }

                    var hours = (decimal)(row.EndTime.Value - row.StartTime.Value).TotalHours;

                    if (hours > 0)
                    {
                        total += hours;
                    }
                }
            }

            return total;
        }

        private static decimal CalculateReservedHours(
            IEnumerable<Reservation> reservations,
            DateTime monthStart,
            DateTime monthEnd)
        {
            decimal total = 0m;

            foreach (var reservation in reservations)
            {
                var start = reservation.StartAt > monthStart
                    ? reservation.StartAt
                    : monthStart;

                var end = reservation.EndAt < monthEnd
                    ? reservation.EndAt
                    : monthEnd;

                if (end <= start)
                {
                    continue;
                }

                total += (decimal)(end - start).TotalHours;
            }

            return total;
        }

        private static int ToBusinessDayOfWeek(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => 1,
                DayOfWeek.Tuesday => 2,
                DayOfWeek.Wednesday => 3,
                DayOfWeek.Thursday => 4,
                DayOfWeek.Friday => 5,
                DayOfWeek.Saturday => 6,
                DayOfWeek.Sunday => 7,
                _ => 0
            };
        }
    }
}