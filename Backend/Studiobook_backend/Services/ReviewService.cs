using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Reviews;
using Studiobook_backend.Entities;

namespace Studiobook_backend.Services;

public class ReviewService
{
    private readonly AppDbContext _db;

    public ReviewService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RoomReviewPageResponse> GetRoomReviewPageAsync(
        int roomId,
        int userId,
        int page,
        int pageSize,
        int? reservationId)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var room = await _db.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == roomId);

        if (room == null)
        {
            throw new KeyNotFoundException("スタジオが見つかりません。");
        }

        var alreadyReviewed = await _db.Reviews
            .AnyAsync(x => x.RoomId == roomId && x.UserId == userId);

        var canReviewQuery = _db.Reservations
            .AsNoTracking()
            .Where(x =>
                x.RoomId == roomId &&
                x.UserId == userId &&
                x.Status == "paid");

        if (reservationId != null)
        {
            canReviewQuery = canReviewQuery.Where(x => x.Id == reservationId.Value);
        }

        var canReview = await canReviewQuery.AnyAsync();

        var publicReviewsQuery = _db.Reviews
            .AsNoTracking()
            .Where(x => x.RoomId == roomId && x.PublicVisible)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id);

        var totalCount = await publicReviewsQuery.CountAsync();

        var averageScore = totalCount == 0
            ? 0.0
            : await publicReviewsQuery.AverageAsync(x => (double)x.Score);

        var reviews = await publicReviewsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ReviewListItemDto
            {
                Id = x.Id,
                Score = x.Score,
                Content = x.Content,
                UserName = x.User.Name,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        var totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new RoomReviewPageResponse
        {
            RoomId = room.Id,
            RoomName = room.Name,
            RoomImageName = room.ImageName,
            RoomAddress = room.Address,
            AverageScore = Math.Round(averageScore, 1, MidpointRounding.AwayFromZero),
            ReviewCount = totalCount,
            Reviews = reviews,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            AlreadyReviewed = alreadyReviewed,
            CanReview = canReview && !alreadyReviewed
        };
    }

    public async Task CreateAsync(
        int roomId,
        int userId,
        CreateReviewRequest request)
    {
        if (request.Score < 1 || request.Score > 5)
        {
            throw new InvalidOperationException("評価は1〜5で指定してください。");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new InvalidOperationException("コメントを入力してください。");
        }

        if (request.Content.Length > 1000)
        {
            throw new InvalidOperationException("コメントは1000文字以内で入力してください。");
        }

        var roomExists = await _db.Rooms.AnyAsync(x => x.Id == roomId);

        if (!roomExists)
        {
            throw new KeyNotFoundException("スタジオが見つかりません。");
        }

        var alreadyReviewed = await _db.Reviews
            .AnyAsync(x => x.RoomId == roomId && x.UserId == userId);

        if (alreadyReviewed)
        {
            throw new InvalidOperationException("このスタジオへのレビューは投稿済みです。");
        }

        var reservationQuery = _db.Reservations
            .AsNoTracking()
            .Where(x =>
                x.RoomId == roomId &&
                x.UserId == userId &&
                x.Status == "paid");

        if (request.ReservationId != null)
        {
            reservationQuery = reservationQuery.Where(x => x.Id == request.ReservationId.Value);
        }

        var hasPaidReservation = await reservationQuery.AnyAsync();

        if (!hasPaidReservation)
        {
            throw new InvalidOperationException("決済済みの予約がある場合のみレビューを投稿できます。");
        }

        var now = DateTime.UtcNow;

        var review = new Review
        {
            RoomId = roomId,
            UserId = userId,
            Score = request.Score,
            Content = request.Content.Trim(),
            PublicVisible = true,
            HiddenReason = null,
            HostReply = null,
            HostReplyAt = null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();
    }
}