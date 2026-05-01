using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Entities;

namespace Studiobook_backend.Services
{
    public class HostBusinessHourService
    {
        private readonly AppDbContext _context;

        public HostBusinessHourService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<BusinessHoursResponseDto> GetAsync(int roomId, int userId)
        {
            var room = await _context.Rooms
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == roomId && x.UserId == userId);

            if (room == null)
            {
                throw new KeyNotFoundException("スタジオが見つからないか、アクセス権限がありません。");
            }

            var businessHours = await _context.BusinessHours
                .AsNoTracking()
                .Where(x => x.RoomId == roomId)
                .OrderBy(x => x.DayOfWeek)
                .ToListAsync();

            var rows = Enumerable.Range(1, 7)
                .Select(day =>
                {
                    var item = businessHours.FirstOrDefault(x => x.DayOfWeek == day);

                    return new BusinessHourRowDto
                    {
                        DayOfWeek = day,
                        StartTime = item?.StartTime?.ToString("HH:mm"),
                        EndTime = item?.EndTime?.ToString("HH:mm"),
                        IsHoliday = item?.IsHoliday ?? false
                    };
                })
                .ToList();

            return new BusinessHoursResponseDto
            {
                RoomId = room.Id,
                RoomName = room.Name,
                Rows = rows
            };
        }

        public async Task SaveAsync(int roomId, int userId, BusinessHoursUpdateRequestDto request)
        {
            var roomExists = await _context.Rooms
                .AnyAsync(x => x.Id == roomId && x.UserId == userId);

            if (!roomExists)
            {
                throw new KeyNotFoundException("スタジオが見つからないか、アクセス権限がありません。");
            }

            if (request.Rows == null || request.Rows.Count != 7)
            {
                throw new ArgumentException("営業時間は月曜から日曜まで7件指定してください。");
            }

            var daySet = request.Rows.Select(x => x.DayOfWeek).ToHashSet();
            if (!Enumerable.Range(1, 7).All(daySet.Contains))
            {
                throw new ArgumentException("曜日の指定が不正です。");
            }

            foreach (var row in request.Rows)
            {
                ValidateRow(row);
            }

            var now = DateTime.UtcNow;

            var existingRows = await _context.BusinessHours
                .Where(x => x.RoomId == roomId)
                .ToListAsync();

            foreach (var row in request.Rows)
            {
                var entity = existingRows.FirstOrDefault(x => x.DayOfWeek == row.DayOfWeek);

                if (entity == null)
                {
                    entity = new BusinessHour
                    {
                        RoomId = roomId,
                        DayOfWeek = row.DayOfWeek,
                        CreatedAtUtc = now
                    };

                    _context.BusinessHours.Add(entity);
                }

                entity.IsHoliday = row.IsHoliday;

                if (row.IsHoliday)
                {
                    entity.StartTime = null;
                    entity.EndTime = null;
                }
                else
                {
                    entity.StartTime = ParseTime(row.StartTime);
                    entity.EndTime = ParseTime(row.EndTime);
                }

                entity.UpdatedAtUtc = now;
            }

            await _context.SaveChangesAsync();
        }

        private static void ValidateRow(BusinessHourRowDto row)
        {
            if (row.DayOfWeek < 1 || row.DayOfWeek > 7)
            {
                throw new ArgumentException("曜日の指定が不正です。");
            }

            if (row.IsHoliday)
            {
                return;
            }

            var start = ParseTime(row.StartTime);
            var end = ParseTime(row.EndTime);

            if (start == null)
            {
                throw new ArgumentException("開始時刻を選択してください。");
            }

            if (end == null)
            {
                throw new ArgumentException("終了時刻を選択してください。");
            }

            if (start.Value.Minute % 15 != 0 || end.Value.Minute % 15 != 0)
            {
                throw new ArgumentException("時刻は15分単位で指定してください。");
            }

            if (end.Value <= start.Value)
            {
                throw new ArgumentException("終了は開始より後の時刻を指定してください。");
            }
        }

        private static TimeOnly? ParseTime(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (TimeOnly.TryParse(value, out var result))
            {
                return result;
            }

            throw new ArgumentException("時刻の形式が不正です。");
        }
    }
}