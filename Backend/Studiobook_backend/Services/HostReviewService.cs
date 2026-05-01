using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Host;

namespace Studiobook_backend.Services
{
    public class HostReviewService
    {
        private readonly AppDbContext _context;

        public HostReviewService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<HostReviewListResponseDto> GetListAsync(
            int hostUserId,
            int? roomId,
            int? stars,
            bool? onlyHidden,
            int page,
            int pageSize)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var query = _context.Reviews
                .AsNoTracking()
                .Include(x => x.Room)
                .Include(x => x.User)
                .Where(x => x.Room.UserId == hostUserId);

            if (roomId.HasValue)
            {
                query = query.Where(x => x.RoomId == roomId.Value);
            }

            if (stars.HasValue)
            {
                query = query.Where(x => x.Score == stars.Value);
            }

            if (onlyHidden == true)
            {
                query = query.Where(x => !x.PublicVisible);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new HostReviewRowDto
                {
                    Id = x.Id,
                    RoomId = x.RoomId,
                    RoomName = x.Room.Name,
                    UserId = x.UserId,
                    UserName = x.User.Name,
                    Score = x.Score,
                    Content = x.Content,
                    PublicVisible = x.PublicVisible,
                    HiddenReason = x.HiddenReason,
                    HostReply = x.HostReply,
                    HostReplyAt = x.HostReplyAt,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToListAsync();

            var roomOptions = await _context.Rooms
                .AsNoTracking()
                .Where(x => x.UserId == hostUserId)
                .OrderBy(x => x.Name)
                .Select(x => new HostReviewRoomOptionDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            return new HostReviewListResponseDto
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                RoomOptions = roomOptions
            };
        }

        public async Task SaveReplyAsync(
            int hostUserId,
            int reviewId,
            HostReviewReplyRequestDto request)
        {
            var review = await _context.Reviews
                .Include(x => x.Room)
                .FirstOrDefaultAsync(x =>
                    x.Id == reviewId &&
                    x.Room.UserId == hostUserId);

            if (review == null)
            {
                throw new KeyNotFoundException("レビューが見つからないか、アクセス権限がありません。");
            }

            review.HostReply = string.IsNullOrWhiteSpace(request.HostReply)
                ? null
                : request.HostReply.Trim();

            review.HostReplyAt = DateTime.UtcNow;
            review.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task ChangeVisibilityAsync(
            int hostUserId,
            int reviewId,
            HostReviewVisibilityRequestDto request)
        {
            var review = await _context.Reviews
                .Include(x => x.Room)
                .FirstOrDefaultAsync(x =>
                    x.Id == reviewId &&
                    x.Room.UserId == hostUserId);

            if (review == null)
            {
                throw new KeyNotFoundException("レビューが見つからないか、アクセス権限がありません。");
            }

            review.PublicVisible = request.IsPublic;
            review.HiddenReason = request.IsPublic
                ? null
                : string.IsNullOrWhiteSpace(request.Reason)
                    ? null
                    : request.Reason.Trim();

            review.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}