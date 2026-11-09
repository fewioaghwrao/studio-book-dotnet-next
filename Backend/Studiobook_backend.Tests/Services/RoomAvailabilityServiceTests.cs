using Studiobook_backend.Application.Dtos.Reservations;
using Studiobook_backend.Application.Services;
using Studiobook_backend.Domain.Entities;
using Studiobook_backend.Infrastructure.Persistence;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Services;

public class RoomAvailabilityServiceTests
{
    // 現在：日本時間 2026/11/10(火) 10:00（UTC 1:00）。判定する日時は 11/14(土) 18:00〜20:00
    private static readonly DateTime StartAt = new(2026, 11, 14, 18, 0, 0);
    private static readonly DateTime EndAt = new(2026, 11, 14, 20, 0, 0);

    private static RoomAvailabilityService CreateService(AppDbContext context)
    {
        return new RoomAvailabilityService(context, FixedTimeProvider.AtUtc(2026, 11, 10, 1, 0));
    }

    [Fact]
    public async Task CheckAsync_営業時間_休業期間_予約の重複から空き状況を判定する()
    {
        await using var context = TestDbContextFactory.Create();
        var now = new DateTime(2026, 11, 1);

        for (var id = 1; id <= 6; id++)
        {
            context.Rooms.Add(new Room
            {
                Id = id,
                UserId = 10,
                Name = $"スタジオ{id}",
                Address = "東京都渋谷区",
                Price = 3000,
                Capacity = 5,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        // 1: 空きあり / 2: 予約済み / 3: キャンセル済みの予約のみ → 空きあり
        // 4: 土曜は休業日 / 5: 17 時まで / 6: 休業期間と重複
        context.BusinessHours.AddRange(
            Saturday(1, 1, 22),
            Saturday(2, 2, 22),
            Saturday(3, 3, 22),
            new BusinessHour { Id = 4, RoomId = 4, DayOfWeek = 6, IsHoliday = true },
            Saturday(5, 5, 17),
            Saturday(6, 6, 22));

        context.Reservations.AddRange(
            CreateReservation(1, roomId: 2, ReservationStatuses.Paid, now),
            CreateReservation(2, roomId: 3, ReservationStatuses.Canceled, now));

        context.Closures.Add(new Closure
        {
            Id = 1,
            RoomId = 6,
            StartAt = new DateTime(2026, 11, 14, 0, 0, 0),
            EndAt = new DateTime(2026, 11, 15, 0, 0, 0),
            CreatedAt = now
        });

        await context.SaveChangesAsync();

        var result = await CreateService(context).CheckAsync(new[] { 1, 2, 3, 4, 5, 6 }, StartAt, EndAt);

        Assert.Equal(RoomAvailabilityStatus.Available, result[1]);
        Assert.Equal(RoomAvailabilityStatus.Reserved, result[2]);
        Assert.Equal(RoomAvailabilityStatus.Available, result[3]);
        Assert.Equal(RoomAvailabilityStatus.Holiday, result[4]);
        Assert.Equal(RoomAvailabilityStatus.OutsideBusinessHours, result[5]);
        Assert.Equal(RoomAvailabilityStatus.Closure, result[6]);
    }

    [Fact]
    public async Task CheckAsync_開始日時が現在以前なら過去として返す()
    {
        await using var context = TestDbContextFactory.Create();

        var result = await CreateService(context).CheckAsync(
            new[] { 1, 2 },
            new DateTime(2026, 11, 10, 9, 0, 0),
            new DateTime(2026, 11, 10, 11, 0, 0));

        Assert.Equal(RoomAvailabilityStatus.Past, result[1]);
        Assert.Equal(RoomAvailabilityStatus.Past, result[2]);
    }

    private static BusinessHour Saturday(int id, int roomId, int endHour)
    {
        return new BusinessHour
        {
            Id = id,
            RoomId = roomId,
            DayOfWeek = 6,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(endHour, 0),
            IsHoliday = false
        };
    }

    private static Reservation CreateReservation(int id, int roomId, string status, DateTime now)
    {
        return new Reservation
        {
            Id = id,
            RoomId = roomId,
            UserId = 20,
            StartAt = new DateTime(2026, 11, 14, 19, 0, 0),
            EndAt = new DateTime(2026, 11, 14, 21, 0, 0),
            Amount = 6000,
            Status = status,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }
}
