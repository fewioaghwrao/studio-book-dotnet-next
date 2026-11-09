using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Application.Common;
using Studiobook_backend.Application.Dtos.Reservations;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Domain.Entities;

namespace Studiobook_backend.Application.Services;

/// <summary>
/// 指定した日時に複数のスタジオが予約できるかをまとめて判定する（AI 検索の空き状況表示用）。
/// 判定の規則は予約確認（ReservationConfirmService）と同じ：
/// 過去日時 → 日またぎ・営業時間・休業期間（RoomScheduleRules）→ 既存予約との重複。
/// </summary>
public class RoomAvailabilityService
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public RoomAvailabilityService(IAppDbContext db, TimeProvider? timeProvider = null)
    {
        _db = db;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <param name="startAt">開始日時（日本時間の壁時計時刻）</param>
    /// <param name="endAt">終了日時（日本時間の壁時計時刻）</param>
    public async Task<IReadOnlyDictionary<int, RoomAvailabilityStatus>> CheckAsync(
        IReadOnlyCollection<int> roomIds,
        DateTime startAt,
        DateTime endAt)
    {
        var result = new Dictionary<int, RoomAvailabilityStatus>();

        if (roomIds.Count == 0)
        {
            return result;
        }

        if (startAt <= JapanTime.Now(_timeProvider))
        {
            foreach (var roomId in roomIds)
            {
                result[roomId] = RoomAvailabilityStatus.Past;
            }

            return result;
        }

        var rooms = await _db.Rooms
            .AsNoTracking()
            .Include(x => x.BusinessHours)
            .Include(x => x.Closures)
            .Where(x => roomIds.Contains(x.Id))
            .ToListAsync();

        foreach (var room in rooms)
        {
            result[room.Id] = RoomScheduleRules.Evaluate(room, startAt, endAt);
        }

        var scheduleOkRoomIds = result
            .Where(x => x.Value == RoomAvailabilityStatus.Available)
            .Select(x => x.Key)
            .ToList();

        if (scheduleOkRoomIds.Count > 0)
        {
            var reservedRoomIds = await _db.Reservations
                .AsNoTracking()
                .Where(x =>
                    scheduleOkRoomIds.Contains(x.RoomId) &&
                    x.Status != ReservationStatuses.Canceled &&
                    x.StartAt < endAt &&
                    startAt < x.EndAt)
                .Select(x => x.RoomId)
                .Distinct()
                .ToListAsync();

            foreach (var roomId in reservedRoomIds)
            {
                result[roomId] = RoomAvailabilityStatus.Reserved;
            }
        }

        return result;
    }
}
