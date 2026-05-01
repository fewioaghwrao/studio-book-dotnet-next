using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Entities;

namespace Studiobook_backend.Services
{
    public class HostPriceRuleService
    {
        private readonly AppDbContext _context;

        public HostPriceRuleService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PriceRulesResponseDto> GetAsync(int roomId, int userId)
        {
            var room = await _context.Rooms
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == roomId && x.UserId == userId);

            if (room == null)
            {
                throw new KeyNotFoundException("スタジオが見つからないか、アクセス権限がありません。");
            }

            var rules = await _context.PriceRules
                .AsNoTracking()
                .Where(x => x.RoomId == roomId)
                .OrderBy(x => x.Weekday == null ? -1 : x.Weekday)
                .ThenBy(x => x.StartHour)
                .ThenBy(x => x.Id)
                .Select(x => new PriceRuleDto
                {
                    Id = x.Id,
                    RuleType = x.RuleType,
                    Weekday = x.Weekday,
                    StartHour = x.StartHour.HasValue ? x.StartHour.Value.ToString("HH:mm") : null,
                    EndHour = x.EndHour.HasValue ? x.EndHour.Value.ToString("HH:mm") : null,
                    Multiplier = x.Multiplier,
                    FlatFee = x.FlatFee,
                    Note = x.Note
                })
                .ToListAsync();

            return new PriceRulesResponseDto
            {
                RoomId = room.Id,
                RoomName = room.Name,
                Rules = rules
            };
        }

        public async Task AddAsync(int roomId, int userId, CreatePriceRuleRequestDto request)
        {
            var roomExists = await _context.Rooms
                .AnyAsync(x => x.Id == roomId && x.UserId == userId);

            if (!roomExists)
            {
                throw new KeyNotFoundException("スタジオが見つからないか、アクセス権限がありません。");
            }

            await ValidateAsync(roomId, request);

            var now = DateTime.UtcNow;

            var entity = new PriceRule
            {
                RoomId = roomId,
                RuleType = request.RuleType,
                Weekday = request.Weekday,
                Note = request.Note,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            if (request.RuleType == "multiplier")
            {
                entity.StartHour = ParseTime(request.StartHour);
                entity.EndHour = ParseTime(request.EndHour);
                entity.Multiplier = request.Multiplier;
                entity.FlatFee = null;
            }
            else if (request.RuleType == "flat_fee")
            {
                entity.StartHour = null;
                entity.EndHour = null;
                entity.Multiplier = null;
                entity.FlatFee = request.FlatFee;
            }

            _context.PriceRules.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int roomId, int ruleId, int userId)
        {
            var rule = await _context.PriceRules
                .Include(x => x.Room)
                .FirstOrDefaultAsync(x =>
                    x.Id == ruleId &&
                    x.RoomId == roomId &&
                    x.Room.UserId == userId);

            if (rule == null)
            {
                throw new KeyNotFoundException("料金ルールが見つからないか、アクセス権限がありません。");
            }

            _context.PriceRules.Remove(rule);
            await _context.SaveChangesAsync();
        }

        private async Task ValidateAsync(int roomId, CreatePriceRuleRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.RuleType))
            {
                throw new ArgumentException("タイプを選択してください。");
            }

            if (request.Weekday is < 0 or > 6)
            {
                throw new ArgumentException("曜日の指定が不正です。");
            }

            if (request.RuleType == "flat_fee")
            {
                if (request.FlatFee == null)
                {
                    throw new ArgumentException("固定費を入力してください。");
                }

                if (request.FlatFee < 0)
                {
                    throw new ArgumentException("固定費は0以上で入力してください。");
                }

                if (!string.IsNullOrWhiteSpace(request.StartHour) ||
                    !string.IsNullOrWhiteSpace(request.EndHour) ||
                    request.Multiplier != null)
                {
                    throw new ArgumentException("固定費の場合、開始/終了時刻と倍率は入力できません。");
                }

                var exists = await _context.PriceRules.AnyAsync(x =>
                    x.RoomId == roomId &&
                    x.RuleType == "flat_fee" &&
                    x.Weekday == request.Weekday);

                if (exists)
                {
                    throw new ArgumentException("同一曜日の固定費はすでに登録されています。");
                }

                return;
            }

            if (request.RuleType == "multiplier")
            {
                if (request.Multiplier == null)
                {
                    throw new ArgumentException("倍率を入力してください。");
                }

                if (request.Multiplier <= 0)
                {
                    throw new ArgumentException("倍率は0より大きい値を入力してください。");
                }

                if (request.FlatFee != null)
                {
                    throw new ArgumentException("倍率の場合、固定費は入力できません。");
                }

                var start = ParseTime(request.StartHour);
                var end = ParseTime(request.EndHour);

                if (start == null || end == null)
                {
                    throw new ArgumentException("開始時刻・終了時刻を選択してください。");
                }

                if (!IsQuarter(start.Value) || !IsQuarter(end.Value))
                {
                    throw new ArgumentException("開始・終了は15分刻みで指定してください。");
                }

                if (end <= start)
                {
                    throw new ArgumentException("終了時刻は開始時刻より後にしてください。");
                }

                return;
            }

            throw new ArgumentException("不正なタイプです。");
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

        private static bool IsQuarter(TimeOnly time)
        {
            return time.Minute % 15 == 0 && time.Second == 0 && time.Millisecond == 0;
        }
    }
}