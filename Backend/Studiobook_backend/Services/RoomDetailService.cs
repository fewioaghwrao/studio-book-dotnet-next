using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Rooms;
using Studiobook_backend.Entities;

namespace Studiobook_backend.Services
{
    public class RoomDetailService
    {
        private readonly AppDbContext _context;

        public RoomDetailService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RoomDetailDto?> GetDetailAsync(int roomId)
        {
            var room = await _context.Rooms
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.BusinessHours)
                .Include(x => x.PriceRules)
                .FirstOrDefaultAsync(x => x.Id == roomId);

            if (room == null)
            {
                return null;
            }

            var publicReviews = await _context.Reviews
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.RoomId == roomId && x.PublicVisible)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(5)
                .ToListAsync();

            var hiddenHostReplies = await _context.Reviews
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x =>
                    x.RoomId == roomId &&
                    !x.PublicVisible &&
                    x.HostReply != null &&
                    x.HostReply != "")
                .OrderByDescending(x => x.HostReplyAt ?? x.UpdatedAtUtc)
                .Take(5)
                .ToListAsync();

            var averageScore = await _context.Reviews
                .AsNoTracking()
                .Where(x => x.RoomId == roomId && x.PublicVisible)
                .Select(x => (double?)x.Score)
                .AverageAsync();

            var reviewCount = await _context.Reviews
                .AsNoTracking()
                .CountAsync(x => x.RoomId == roomId && x.PublicVisible);

            var calendarEvents = await BuildCalendarEventsAsync(
                roomId,
                room.BusinessHours.OrderBy(x => x.DayOfWeek).ToList()
            );

            return new RoomDetailDto
            {
                Id = room.Id,
                Name = room.Name,
                ImageName = room.ImageName,
                Description = room.Description,
                Price = room.Price,
                Capacity = room.Capacity,
                PostalCode = room.PostalCode,
                Address = room.Address,
                HostName = room.User.Name,
                AverageScore = averageScore,
                ReviewCount = reviewCount,
                BusinessHours = room.BusinessHours
                    .OrderBy(x => x.DayOfWeek)
                    .Select(x => new RoomBusinessHourDto
                    {
                        DayOfWeek = x.DayOfWeek,
                        StartTime = ToTimeString(x.StartTime),
                        EndTime = ToTimeString(x.EndTime),
                        IsHoliday = x.IsHoliday
                    })
                    .ToList(),
                PriceRules = room.PriceRules
                    .OrderBy(x => x.Weekday == null ? -1 : x.Weekday)
                    .ThenBy(x => x.StartHour)
                    .ThenBy(x => x.EndHour)
                    .Select(x => new RoomPriceRuleDto
                    {
                        Id = x.Id,
                        RuleType = x.RuleType,
                        Weekday = x.Weekday,
                        StartHour = ToTimeString(x.StartHour),
                        EndHour = ToTimeString(x.EndHour),
                        Multiplier = x.Multiplier,
                        FlatFee = x.FlatFee,
                        Note = x.Note
                    })
                    .ToList(),
                Reviews = publicReviews
                    .Select(ToReviewDto)
                    .ToList(),
                HiddenHostReplies = hiddenHostReplies
                    .Select(x => new RoomReviewDto
                    {
                        Id = x.Id,
                        Score = null,
                        Content = string.Empty,
                        UserName = string.Empty,
                        HostReply = x.HostReply,
                        HostReplyAt = x.HostReplyAt,
                        CreatedAtUtc = x.CreatedAtUtc
                    })
                    .ToList(),
                CalendarEvents = calendarEvents
            };
        }

        private async Task<List<RoomCalendarEventDto>> BuildCalendarEventsAsync(
            int roomId,
            List<BusinessHour> businessHours)
        {
            var events = new List<RoomCalendarEventDto>();

            var today = DateTime.Today;
            var from = today.AddDays(-7);
            var to = today.AddDays(60);

            // 営業時間背景
            for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
            {
                var businessDayOfWeek = ToBusinessDayOfWeek(date.DayOfWeek);
                var businessHour = businessHours
                    .FirstOrDefault(x => x.DayOfWeek == businessDayOfWeek);

                if (businessHour == null)
                {
                    continue;
                }

                if (businessHour.IsHoliday)
                {
                    events.Add(new RoomCalendarEventDto
                    {
                        Id = $"holiday-{date:yyyyMMdd}",
                        Title = "休業",
                        Start = date,
                        End = date.AddDays(1),
                        AllDay = true,
                        Type = "closure"
                    });

                    continue;
                }

                if (businessHour.StartTime.HasValue && businessHour.EndTime.HasValue)
                {
                    events.Add(new RoomCalendarEventDto
                    {
                        Id = $"open-{date:yyyyMMdd}",
                        Title = "営業時間",
                        Start = date.Add(businessHour.StartTime.Value.ToTimeSpan()),
                        End = date.Add(businessHour.EndTime.Value.ToTimeSpan()),
                        AllDay = false,
                        Type = "open"
                    });
                }
            }

            // 休館日
            var closures = await _context.Closures
                .AsNoTracking()
                .Where(x =>
                    x.RoomId == roomId &&
                    x.EndAt >= from &&
                    x.StartAt <= to)
                .OrderBy(x => x.StartAt)
                .ToListAsync();

            foreach (var closure in closures)
            {
                events.Add(new RoomCalendarEventDto
                {
                    Id = $"closure-{closure.Id}",
                    Title = string.IsNullOrWhiteSpace(closure.Reason)
                        ? "休館"
                        : closure.Reason,
                    Start = closure.StartAt,
                    End = closure.EndAt,
                    AllDay = false,
                    Type = "closure"
                });
            }

            // 予約済み
            var reservations = await _context.Reservations
                .AsNoTracking()
                .Where(x =>
                    x.RoomId == roomId &&
                    x.Status != "canceled" &&
                    x.EndAt >= from &&
                    x.StartAt <= to)
                .OrderBy(x => x.StartAt)
                .ToListAsync();

            foreach (var reservation in reservations)
            {
                events.Add(new RoomCalendarEventDto
                {
                    Id = $"reservation-{reservation.Id}",
                    Title = "予約済み",
                    Start = reservation.StartAt,
                    End = reservation.EndAt,
                    AllDay = false,
                    Type = "reservation"
                });
            }

            return events;
        }

        private static RoomReviewDto ToReviewDto(Review review)
        {
            return new RoomReviewDto
            {
                Id = review.Id,
                Score = review.Score,
                Content = review.Content,
                UserName = review.User?.Name ?? "ユーザー",
                HostReply = review.HostReply,
                HostReplyAt = review.HostReplyAt,
                CreatedAtUtc = review.CreatedAtUtc
            };
        }

        private static string? ToTimeString(TimeOnly? value)
        {
            return value?.ToString("HH:mm");
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