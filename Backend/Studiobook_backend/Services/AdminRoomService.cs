using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Admin;
using Studiobook_backend.Entities;

namespace Studiobook_backend.Services
{
    public class AdminRoomService
    {
        private readonly AppDbContext _context;

        public AdminRoomService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdminRoomListResponseDto> GetListAsync(
            string? keyword,
            int page,
            int pageSize)
        {
            var normalizedKeyword = keyword?.Trim() ?? string.Empty;

            var safePage = page <= 0 ? 1 : page;
            var safePageSize = pageSize <= 0 ? 10 : pageSize;
            safePageSize = Math.Min(safePageSize, 100);

            var query = _context.Rooms
                .AsNoTracking()
                .Include(x => x.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                query = query.Where(x =>
                    x.Name.Contains(normalizedKeyword) ||
                    x.Address.Contains(normalizedKeyword) ||
                    x.PostalCode.Contains(normalizedKeyword) ||
                    x.User.Name.Contains(normalizedKeyword));
            }

            var totalCount = await query.CountAsync();

            var rooms = await query
                .OrderBy(x => x.Id)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .Select(x => new AdminRoomListItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    PostalCode = x.PostalCode,
                    Address = x.Address,
                    HostUserId = x.UserId,
                    HostName = x.User.Name
                })
                .ToListAsync();

            var totalPages = totalCount == 0
                ? 1
                : (int)Math.Ceiling(totalCount / (double)safePageSize);

            return new AdminRoomListResponseDto
            {
                Items = rooms,
                Keyword = normalizedKeyword,
                Page = safePage,
                PageSize = safePageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        public async Task<AdminRoomDetailDto?> GetDetailAsync(int roomId)
        {
            return await _context.Rooms
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.Id == roomId)
                .Select(x => new AdminRoomDetailDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    HostName = x.User.Name,
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

        public async Task<List<AdminRoomHostOptionDto>> GetHostOptionsAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .Where(x =>
                    x.Enabled &&
                    x.UserRoles.Any(ur => ur.Role.Name == "Host"))
                .OrderBy(x => x.Id)
                .Select(x => new AdminRoomHostOptionDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Email = x.Email
                })
                .ToListAsync();
        }

        public async Task<AdminRoomDetailDto> CreateAsync(AdminRoomUpsertRequestDto request)
        {
            await ValidateHostAsync(request.UserId);

            var normalizedName = request.Name.Trim();
            var normalizedAddress = request.Address.Trim();

            var duplicated = await _context.Rooms
                .AnyAsync(x =>
                    x.Name == normalizedName &&
                    x.Address == normalizedAddress);

            if (duplicated)
            {
                throw new InvalidOperationException("ROOM_DUPLICATED");
            }

            var now = DateTime.UtcNow;

            var room = new Room
            {
                UserId = request.UserId,
                Name = normalizedName,
                ImageName = request.ImageName.Trim(),
                Description = request.Description.Trim(),
                Price = request.Price,
                Capacity = request.Capacity,
                PostalCode = request.PostalCode.Trim(),
                Address = normalizedAddress,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            var created = await GetDetailAsync(room.Id);

            if (created == null)
            {
                throw new InvalidOperationException("ROOM_CREATE_FAILED");
            }

            return created;
        }

        public async Task<AdminRoomDetailDto?> UpdateAsync(
            int roomId,
            AdminRoomUpsertRequestDto request)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(x => x.Id == roomId);

            if (room == null)
            {
                return null;
            }

            await ValidateHostAsync(request.UserId);

            var normalizedName = request.Name.Trim();
            var normalizedAddress = request.Address.Trim();

            var duplicated = await _context.Rooms
                .AnyAsync(x =>
                    x.Id != roomId &&
                    x.Name == normalizedName &&
                    x.Address == normalizedAddress);

            if (duplicated)
            {
                throw new InvalidOperationException("ROOM_DUPLICATED");
            }

            room.UserId = request.UserId;
            room.Name = normalizedName;
            room.ImageName = request.ImageName.Trim();
            room.Description = request.Description.Trim();
            room.Price = request.Price;
            room.Capacity = request.Capacity;
            room.PostalCode = request.PostalCode.Trim();
            room.Address = normalizedAddress;
            room.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetDetailAsync(room.Id);
        }

        private async Task ValidateHostAsync(int userId)
        {
            var hostExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id == userId &&
                    x.Enabled &&
                    x.UserRoles.Any(ur => ur.Role.Name == "Host"));

            if (!hostExists)
            {
                throw new InvalidOperationException("HOST_NOT_FOUND");
            }
        }
    }
}