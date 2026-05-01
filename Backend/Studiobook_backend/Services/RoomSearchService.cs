using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Rooms;

namespace Studiobook_backend.Services
{
    public class RoomSearchService
    {
        private readonly AppDbContext _context;

        public RoomSearchService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RoomListResponseDto> GetListAsync(
            string? keyword,
            string? area,
            int? price,
            string? order,
            int page,
            int pageSize)
        {
            var normalizedKeyword = keyword?.Trim() ?? string.Empty;
            var normalizedArea = area?.Trim() ?? string.Empty;
            var normalizedOrder = string.IsNullOrWhiteSpace(order)
                ? "createdAtDesc"
                : order.Trim();

            var safePage = page <= 0 ? 1 : page;
            var safePageSize = pageSize <= 0 ? 10 : pageSize;
            safePageSize = Math.Min(safePageSize, 50);

            var query = _context.Rooms
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                query = query.Where(x =>
                    x.Name.Contains(normalizedKeyword) ||
                    x.Description.Contains(normalizedKeyword) ||
                    x.PostalCode.Contains(normalizedKeyword) ||
                    x.Address.Contains(normalizedKeyword));
            }

            if (!string.IsNullOrWhiteSpace(normalizedArea))
            {
                query = query.Where(x => x.Address.Contains(normalizedArea));
            }

            if (price.HasValue && price.Value > 0)
            {
                query = query.Where(x => x.Price <= price.Value);
            }

            query = normalizedOrder switch
            {
                "priceAsc" => query
                    .OrderBy(x => x.Price)
                    .ThenByDescending(x => x.CreatedAtUtc)
                    .ThenByDescending(x => x.Id),

                _ => query
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .ThenByDescending(x => x.Id)
            };

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .Select(room => new RoomListItemDto
                {
                    Id = room.Id,
                    Name = room.Name,
                    ImageName = room.ImageName,
                    Description = room.Description,
                    PostalCode = room.PostalCode,
                    Address = room.Address,
                    Price = room.Price,
                    AverageScore = room.Reviews
                        .Where(review => review.PublicVisible)
                        .Select(review => (double?)review.Score)
                        .Average(),
                    ReviewCount = room.Reviews
                        .Count(review => review.PublicVisible)
                })
                .ToListAsync();

            var totalPages = totalCount == 0
                ? 1
                : (int)Math.Ceiling(totalCount / (double)safePageSize);

            return new RoomListResponseDto
            {
                Items = items,
                Keyword = normalizedKeyword,
                Area = normalizedArea,
                Price = price,
                Order = normalizedOrder,
                Page = safePage,
                PageSize = safePageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }
    }
}