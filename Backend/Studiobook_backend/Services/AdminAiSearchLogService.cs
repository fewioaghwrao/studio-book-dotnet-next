using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Admin;

namespace Studiobook_backend.Services;

public class AdminAiSearchLogService
{
    private readonly AppDbContext _context;

    public AdminAiSearchLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AdminAiSearchLogListResponse> GetListAsync(
        string? q,
        int? userId,
        string? ipAddress,
        bool? succeeded,
        string? model,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize)
    {
        var normalizedQ = q?.Trim() ?? string.Empty;
        var normalizedIpAddress = ipAddress?.Trim() ?? string.Empty;
        var normalizedModel = model?.Trim() ?? string.Empty;

        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize <= 0 ? 10 : pageSize;
        safePageSize = Math.Min(safePageSize, 50);

        var query = _context.AiSearchLogs
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedQ))
        {
            query = query.Where(x =>
                x.Query.Contains(normalizedQ) ||
                (x.ErrorMessage != null && x.ErrorMessage.Contains(normalizedQ)));
        }

        if (userId.HasValue && userId.Value > 0)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedIpAddress))
        {
            query = query.Where(x =>
                x.IpAddress != null &&
                x.IpAddress.Contains(normalizedIpAddress));
        }

        if (succeeded.HasValue)
        {
            query = query.Where(x => x.Succeeded == succeeded.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedModel))
        {
            query = query.Where(x =>
                x.Model != null &&
                x.Model.Contains(normalizedModel));
        }

        if (from.HasValue)
        {
            var fromUtc = from.Value.Date;
            query = query.Where(x => x.CreatedAtUtc >= fromUtc);
        }

        if (to.HasValue)
        {
            var toExclusiveUtc = to.Value.Date.AddDays(1);
            query = query.Where(x => x.CreatedAtUtc < toExclusiveUtc);
        }

        query = query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(x => new AdminAiSearchLogListItemDto
            {
                Id = x.Id,
                CreatedAtUtc = x.CreatedAtUtc,
                Query = x.Query,
                IpAddress = x.IpAddress,
                UserId = x.UserId,
                Model = x.Model,
                Succeeded = x.Succeeded,
                ResultCount = x.ResultCount,
                ErrorMessage = x.ErrorMessage
            })
            .ToListAsync();

        var totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)safePageSize);

        return new AdminAiSearchLogListResponse
        {
            Items = items,
            Page = safePage,
            PageSize = safePageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}