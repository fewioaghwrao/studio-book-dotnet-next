using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Services;

public class HostBusinessHourServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task SeedRoomAsync(AppDbContext context)
    {
        var host = new User
        {
            Id = 1,
            Name = "ホスト太郎",
            Kana = "ホストタロウ",
            Email = "host@example.com",
            PasswordHash = "hash",
            PostalCode = "100-0001",
            Address = "東京都千代田区",
            PhoneNumber = "090-0000-0001",
            UsageType = "Host",
            Enabled = true
        };

        var otherHost = new User
        {
            Id = 2,
            Name = "別ホスト",
            Kana = "ベツホスト",
            Email = "other@example.com",
            PasswordHash = "hash",
            PostalCode = "100-0002",
            Address = "東京都中央区",
            PhoneNumber = "090-0000-0002",
            UsageType = "Host",
            Enabled = true
        };

        context.Users.AddRange(host, otherHost);

        context.Rooms.AddRange(
            new Room
            {
                Id = 1,
                UserId = host.Id,
                User = host,
                Name = "A Studio",
                ImageName = "room01.jpg",
                Description = "テスト用",
                Price = 3000,
                Capacity = 5,
                PostalCode = "100-0001",
                Address = "東京都渋谷区",
                CreatedAtUtc = new DateTime(2026, 5, 1),
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            },
            new Room
            {
                Id = 2,
                UserId = otherHost.Id,
                User = otherHost,
                Name = "Other Studio",
                ImageName = "room02.jpg",
                Description = "他人の部屋",
                Price = 4000,
                Capacity = 8,
                PostalCode = "100-0002",
                Address = "東京都新宿区",
                CreatedAtUtc = new DateTime(2026, 5, 1),
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            });

        await context.SaveChangesAsync();
    }

    private static BusinessHoursUpdateRequestDto CreateValidRequest()
    {
        return new BusinessHoursUpdateRequestDto
        {
            Rows = Enumerable.Range(1, 7)
                .Select(day => new BusinessHourRowDto
                {
                    DayOfWeek = day,
                    StartTime = "09:00",
                    EndTime = "18:00",
                    IsHoliday = day is 6 or 7
                })
                .ToList()
        };
    }

    [Fact]
    public async Task GetAsync_自分のRoomの場合_7曜日分を返す()
    {
        await using var context = CreateDbContext();
        await SeedRoomAsync(context);

        context.BusinessHours.Add(new BusinessHour
        {
            Id = 1,
            RoomId = 1,
            DayOfWeek = 1,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(18, 0),
            IsHoliday = false,
            CreatedAtUtc = new DateTime(2026, 5, 1),
            UpdatedAtUtc = new DateTime(2026, 5, 1)
        });

        await context.SaveChangesAsync();

        var service = new HostBusinessHourService(context);

        var result = await service.GetAsync(roomId: 1, userId: 1);

        Assert.Equal(1, result.RoomId);
        Assert.Equal("A Studio", result.RoomName);
        Assert.Equal(7, result.Rows.Count);

        Assert.Equal(1, result.Rows[0].DayOfWeek);
        Assert.Equal("09:00", result.Rows[0].StartTime);
        Assert.Equal("18:00", result.Rows[0].EndTime);
        Assert.False(result.Rows[0].IsHoliday);

        Assert.Equal(2, result.Rows[1].DayOfWeek);
        Assert.Null(result.Rows[1].StartTime);
        Assert.Null(result.Rows[1].EndTime);
        Assert.False(result.Rows[1].IsHoliday);
    }

    [Fact]
    public async Task GetAsync_他人のRoomの場合_KeyNotFoundExceptionを投げる()
    {
        await using var context = CreateDbContext();
        await SeedRoomAsync(context);

        var service = new HostBusinessHourService(context);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetAsync(roomId: 2, userId: 1));

        Assert.Equal("スタジオが見つからないか、アクセス権限がありません。", ex.Message);
    }

    [Fact]
    public async Task SaveAsync_有効な7件の場合_BusinessHoursを保存する()
    {
        await using var context = CreateDbContext();
        await SeedRoomAsync(context);

        var service = new HostBusinessHourService(context);
        var request = CreateValidRequest();

        await service.SaveAsync(roomId: 1, userId: 1, request);

        var rows = await context.BusinessHours
            .Where(x => x.RoomId == 1)
            .OrderBy(x => x.DayOfWeek)
            .ToListAsync();

        Assert.Equal(7, rows.Count);

        Assert.Equal(1, rows[0].DayOfWeek);
        Assert.False(rows[0].IsHoliday);
        Assert.Equal(new TimeOnly(9, 0), rows[0].StartTime);
        Assert.Equal(new TimeOnly(18, 0), rows[0].EndTime);

        Assert.Equal(6, rows[5].DayOfWeek);
        Assert.True(rows[5].IsHoliday);
        Assert.Null(rows[5].StartTime);
        Assert.Null(rows[5].EndTime);
    }

    [Fact]
    public async Task SaveAsync_既存行がある場合_更新する()
    {
        await using var context = CreateDbContext();
        await SeedRoomAsync(context);

        context.BusinessHours.Add(new BusinessHour
        {
            Id = 1,
            RoomId = 1,
            DayOfWeek = 1,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(17, 0),
            IsHoliday = false,
            CreatedAtUtc = new DateTime(2026, 5, 1),
            UpdatedAtUtc = new DateTime(2026, 5, 1)
        });

        await context.SaveChangesAsync();

        var service = new HostBusinessHourService(context);

        var request = CreateValidRequest();
        request.Rows[0].StartTime = "08:30";
        request.Rows[0].EndTime = "19:00";

        await service.SaveAsync(roomId: 1, userId: 1, request);

        var monday = await context.BusinessHours
            .SingleAsync(x => x.RoomId == 1 && x.DayOfWeek == 1);

        Assert.Equal(new TimeOnly(8, 30), monday.StartTime);
        Assert.Equal(new TimeOnly(19, 0), monday.EndTime);

        var totalRows = await context.BusinessHours.CountAsync(x => x.RoomId == 1);
        Assert.Equal(7, totalRows);
    }

    [Fact]
    public async Task SaveAsync_Rowsが7件でない場合_ArgumentExceptionを投げる()
    {
        await using var context = CreateDbContext();
        await SeedRoomAsync(context);

        var service = new HostBusinessHourService(context);

        var request = new BusinessHoursUpdateRequestDto
        {
            Rows = Enumerable.Range(1, 6)
                .Select(day => new BusinessHourRowDto
                {
                    DayOfWeek = day,
                    StartTime = "09:00",
                    EndTime = "18:00",
                    IsHoliday = false
                })
                .ToList()
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveAsync(roomId: 1, userId: 1, request));

        Assert.Equal("営業時間は月曜から日曜まで7件指定してください。", ex.Message);
    }

    [Fact]
    public async Task SaveAsync_曜日指定が不正な場合_ArgumentExceptionを投げる()
    {
        await using var context = CreateDbContext();
        await SeedRoomAsync(context);

        var service = new HostBusinessHourService(context);

        var request = CreateValidRequest();
        request.Rows[0].DayOfWeek = 8;

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveAsync(roomId: 1, userId: 1, request));

        Assert.Equal("曜日の指定が不正です。", ex.Message);
    }

    [Fact]
    public async Task SaveAsync_開始時刻が空の場合_ArgumentExceptionを投げる()
    {
        await using var context = CreateDbContext();
        await SeedRoomAsync(context);

        var service = new HostBusinessHourService(context);

        var request = CreateValidRequest();
        request.Rows[0].StartTime = "";

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveAsync(roomId: 1, userId: 1, request));

        Assert.Equal("開始時刻を選択してください。", ex.Message);
    }

    [Fact]
    public async Task SaveAsync_終了時刻が開始時刻以下の場合_ArgumentExceptionを投げる()
    {
        await using var context = CreateDbContext();
        await SeedRoomAsync(context);

        var service = new HostBusinessHourService(context);

        var request = CreateValidRequest();
        request.Rows[0].StartTime = "18:00";
        request.Rows[0].EndTime = "09:00";

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveAsync(roomId: 1, userId: 1, request));

        Assert.Equal("終了は開始より後の時刻を指定してください。", ex.Message);
    }

    [Fact]
    public async Task SaveAsync_時刻が15分単位でない場合_ArgumentExceptionを投げる()
    {
        await using var context = CreateDbContext();
        await SeedRoomAsync(context);

        var service = new HostBusinessHourService(context);

        var request = CreateValidRequest();
        request.Rows[0].StartTime = "09:10";
        request.Rows[0].EndTime = "18:00";

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveAsync(roomId: 1, userId: 1, request));

        Assert.Equal("時刻は15分単位で指定してください。", ex.Message);
    }
}