using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Application.Dtos;
using Studiobook_backend.Application.Interfaces;

namespace Studiobook_backend.Application.Services
{
    /// <summary>
    /// ホスト向けスタジオ一覧・詳細の取得。
    /// </summary>
    public class HostRoomService
    {
        private readonly IAppDbContext _context;

        public HostRoomService(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponseDto<HostRoomListItemDto>> GetListAsync(
            int hostUserId,
            string? keyword,
            int page,
            int pageSize)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _context.Rooms
                .AsNoTracking()
                .Where(x => x.UserId == hostUserId);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var trimmedKeyword = keyword.Trim();
                query = query.Where(x => x.Name.Contains(trimmedKeyword));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new HostRoomListItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    PostalCode = x.PostalCode,
                    Address = x.Address
                })
                .ToListAsync();

            var totalPages = totalCount == 0
                ? 1
                : (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PagedResponseDto<HostRoomListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        /// <summary>
        /// ログイン中ホストが所有するスタジオの詳細を取得します。存在しない・他ホストのスタジオの場合は null。
        /// </summary>
        public async Task<HostRoomDetailDto?> FindDetailAsync(
            int hostUserId,
            int roomId)
        {
            return await _context.Rooms
                .AsNoTracking()
                .Where(x => x.Id == roomId && x.UserId == hostUserId)
                .Select(x => new HostRoomDetailDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    ImageName = x.ImageName,
                    Description = x.Description,
                    Price = x.Price,
                    Capacity = x.Capacity,
                    PostalCode = x.PostalCode,
                    Address = x.Address
                })
                .FirstOrDefaultAsync();
        }
    }
}
