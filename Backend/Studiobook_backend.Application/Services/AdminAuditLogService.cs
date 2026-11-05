using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Application.Common;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Application.Dtos.Admin;

namespace Studiobook_backend.Application.Services
{
    public class AdminAuditLogService
    {
        private readonly IAppDbContext _context;

        public AdminAuditLogService(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<AdminAuditLogListResponseDto> GetListAsync(
            string? q,
            int? actorId,
            string? action,
            string? entity,
            int? entityId,
            DateTime? from,
            DateTime? to,
            int page,
            int pageSize)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;
            pageSize = Math.Min(pageSize, 100);

            var query = _context.AuditLogs
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var keyword = q.Trim();

                query = query.Where(x =>
                    x.Action.Contains(keyword) ||
                    x.Entity.Contains(keyword) ||
                    x.Note.Contains(keyword));
            }

            if (actorId.HasValue)
            {
                query = query.Where(x => x.ActorId == actorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                var actionKeyword = action.Trim();
                query = query.Where(x => x.Action.Contains(actionKeyword));
            }

            if (!string.IsNullOrWhiteSpace(entity))
            {
                var entityKeyword = entity.Trim();
                query = query.Where(x => x.Entity.Contains(entityKeyword));
            }

            if (entityId.HasValue)
            {
                query = query.Where(x => x.EntityId == entityId.Value);
            }

            // 画面で指定する日付は日本時間の 1 日（0:00〜24:00）。Ts は UTC のため変換して比較する
            if (from.HasValue)
            {
                var fromUtc = JapanTime.ToUtc(from.Value.Date);
                query = query.Where(x => x.Ts >= fromUtc);
            }

            if (to.HasValue)
            {
                var toExclusiveUtc = JapanTime.ToUtc(to.Value.Date.AddDays(1));
                query = query.Where(x => x.Ts < toExclusiveUtc);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.Ts)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AdminAuditLogRowDto
                {
                    Id = x.Id,
                    Ts = x.Ts,
                    ActorId = x.ActorId,
                    Action = x.Action,
                    Entity = x.Entity,
                    EntityId = x.EntityId,
                    Note = x.Note
                })
                .ToListAsync();

            var totalPages = totalCount == 0
                ? 1
                : (int)Math.Ceiling(totalCount / (double)pageSize);

            return new AdminAuditLogListResponseDto
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }
    }
}