using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Admin;
using Studiobook_backend.Entities;

namespace Studiobook_backend.Services
{
    public class AdminUserService
    {
        private readonly AppDbContext _context;

        public AdminUserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdminUserListResponseDto> GetListAsync(
            string? keyword,
            int page,
            int pageSize)
        {
            var normalizedKeyword = keyword?.Trim() ?? string.Empty;

            var safePage = page <= 0 ? 1 : page;
            var safePageSize = pageSize <= 0 ? 10 : pageSize;
            safePageSize = Math.Min(safePageSize, 100);

            var query = _context.Users
                .AsNoTracking()
                .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                query = query.Where(x =>
                    x.Name.Contains(normalizedKeyword) ||
                    x.Kana.Contains(normalizedKeyword) ||
                    x.Email.Contains(normalizedKeyword));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(x => x.Id)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .ToListAsync();

            var items = users
                .Select(ToListItemDto)
                .ToList();

            var totalPages = totalCount == 0
                ? 1
                : (int)Math.Ceiling(totalCount / (double)safePageSize);

            return new AdminUserListResponseDto
            {
                Items = items,
                Keyword = normalizedKeyword,
                Page = safePage,
                PageSize = safePageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        public async Task<AdminUserDetailDto?> GetDetailAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
            {
                return null;
            }

            var roleNames = user.UserRoles
                .Select(x => x.Role.Name)
                .Distinct()
                .ToList();

            var roleName = ResolvePrimaryRoleName(roleNames);

            return new AdminUserDetailDto
            {
                Id = user.Id,
                Name = user.Name,
                Kana = user.Kana,
                PostalCode = user.PostalCode,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                UsageType = user.UsageType,
                RoleName = roleName,
                RoleLabel = ToRoleLabel(roleName),
                Enabled = user.Enabled
            };
        }

        private static AdminUserListItemDto ToListItemDto(User user)
        {
            var roleNames = user.UserRoles
                .Select(x => x.Role.Name)
                .Distinct()
                .ToList();

            var roleName = ResolvePrimaryRoleName(roleNames);

            return new AdminUserListItemDto
            {
                Id = user.Id,
                Name = user.Name,
                Kana = user.Kana,
                Email = user.Email,
                RoleName = roleName,
                RoleLabel = ToRoleLabel(roleName),
                Enabled = user.Enabled
            };
        }

        private static string ResolvePrimaryRoleName(List<string> roleNames)
        {
            if (roleNames.Any(x => x.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
            {
                return "Admin";
            }

            if (roleNames.Any(x => x.Equals("Host", StringComparison.OrdinalIgnoreCase)))
            {
                return "Host";
            }

            if (roleNames.Any(x => x.Equals("GeneralUser", StringComparison.OrdinalIgnoreCase)))
            {
                return "GeneralUser";
            }

            return roleNames.FirstOrDefault() ?? string.Empty;
        }

        private static string ToRoleLabel(string roleName)
        {
            return roleName switch
            {
                "Admin" => "管理者",
                "Host" => "スタジオ提供者",
                "GeneralUser" => "会員",
                _ => "未設定"
            };
        }
    }
}