using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Entities;

namespace Studiobook_backend.Services;

public class HostClosureService
{
    private readonly AppDbContext _context;

    public HostClosureService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Room> GetOwnedRoomOrThrowAsync(int roomId, int userId)
    {
        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == roomId && r.UserId == userId);

        if (room is null)
        {
            throw new UnauthorizedAccessException("対象のスタジオが見つからないか、操作権限がありません。");
        }

        return room;
    }

    public async Task<List<ClosureDto>> ListAsync(int roomId, int userId)
    {
        await GetOwnedRoomOrThrowAsync(roomId, userId);

        return await _context.Closures
            .Where(c => c.RoomId == roomId)
            .OrderBy(c => c.StartAt)
            .Select(c => new ClosureDto
            {
                Id = c.Id,
                RoomId = c.RoomId,
                StartAt = c.StartAt,
                EndAt = c.EndAt,
                Reason = c.Reason
            })
            .ToListAsync();
    }

    public async Task<List<ClosureEventDto>> EventsAsync(int roomId, int userId)
    {
        var closures = await ListAsync(roomId, userId);

        return closures.Select(c =>
        {
            var allDay = c.StartAt.TimeOfDay == TimeSpan.Zero
                         && c.EndAt.TimeOfDay == TimeSpan.Zero;

            return new ClosureEventDto
            {
                Id = c.Id,
                Title = string.IsNullOrWhiteSpace(c.Reason)
                    ? "休館"
                    : $"休館: {c.Reason}",
                Start = c.StartAt,
                End = c.EndAt,
                AllDay = allDay
            };
        }).ToList();
    }

    public async Task<ClosureDto> CreateAsync(int roomId, int userId, CreateClosureRequest request)
    {
        await GetOwnedRoomOrThrowAsync(roomId, userId);

        if (request.EndAt <= request.StartAt)
        {
            throw new InvalidOperationException("終了日時は開始日時より後にしてください。");
        }

        var closure = new Closure
        {
            RoomId = roomId,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Reason = request.Reason
        };

        _context.Closures.Add(closure);
        await _context.SaveChangesAsync();

        return new ClosureDto
        {
            Id = closure.Id,
            RoomId = closure.RoomId,
            StartAt = closure.StartAt,
            EndAt = closure.EndAt,
            Reason = closure.Reason
        };
    }

    public async Task DeleteAsync(int roomId, int closureId, int userId)
    {
        await GetOwnedRoomOrThrowAsync(roomId, userId);

        var closure = await _context.Closures
            .FirstOrDefaultAsync(c => c.Id == closureId && c.RoomId == roomId);

        if (closure is null)
        {
            throw new KeyNotFoundException("休館日が見つかりません。");
        }

        _context.Closures.Remove(closure);
        await _context.SaveChangesAsync();
    }
}