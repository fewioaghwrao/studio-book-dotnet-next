using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Entities;

namespace Studiobook_backend.Services
{
    public class HostStatusService
    {
        private readonly AppDbContext _context;

        public HostStatusService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<HostStatusResponseDto> GetAsync(
            int hostUserId,
            int? roomId,
            DateTime baseMonth)
        {
            var targetRoomsQuery = _context.Rooms
                .AsNoTracking()
                .Where(x => x.UserId == hostUserId);

            if (roomId.HasValue && roomId.Value > 0)
            {
                targetRoomsQuery = targetRoomsQuery.Where(x => x.Id == roomId.Value);
            }

            var targetRooms = await targetRoomsQuery
                .OrderBy(x => x.Id)
                .ToListAsync();

            var allRoomOptions = await _context.Rooms
                .AsNoTracking()
                .Where(x => x.UserId == hostUserId)
                .OrderBy(x => x.Name)
                .Select(x => new HostStatusRoomOptionDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            if (targetRooms.Count == 0)
            {
                return new HostStatusResponseDto
                {
                    Labels = BuildRecentMonthLabels(baseMonth),
                    Booked = new List<int> { 0, 0, 0 },
                    Paid = new List<int> { 0, 0, 0 },
                    UtilizationPercents = new List<decimal?> { null, null, null },
                    ReviewAvgAny = null,
                    ReviewAvgPublic = null,
                    RoomOptions = allRoomOptions
                };
            }

            var roomIds = targetRooms.Select(x => x.Id).ToList();
            var months = BuildRecentMonths(baseMonth);

            var firstMonthStart = months.First();
            var lastMonthEndExclusive = months.Last().AddMonths(1);

            var reservations = await _context.Reservations
                .AsNoTracking()
                .Where(x =>
                    roomIds.Contains(x.RoomId) &&
                    x.StartAt < lastMonthEndExclusive &&
                    x.EndAt >= firstMonthStart)
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

                var bookedAmount = reservations
                    .Where(x =>
                        x.Status == "booked" &&
                        x.StartAt >= monthStart &&
                        x.StartAt < monthEnd)
                    .Sum(x => x.Amount);

                var paidAmount = reservations
                    .Where(x =>
                        x.Status == "paid" &&
                        x.StartAt >= monthStart &&
                        x.StartAt < monthEnd)
                    .Sum(x => x.Amount);

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

                    // デモデータでは営業時間外予約もあり得るため、表示上は100%上限に丸める
                    if (percent > 100m)
                    {
                        percent = 100m;
                    }

                    utilizationPercents.Add(Math.Round(percent, 1, MidpointRounding.AwayFromZero));
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

            return new HostStatusResponseDto
            {
                Labels = labels,
                Booked = booked,
                Paid = paid,
                UtilizationPercents = utilizationPercents,
                ReviewAvgAny = reviewAvgAny,
                ReviewAvgPublic = reviewAvgPublic,
                RoomOptions = allRoomOptions
            };
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