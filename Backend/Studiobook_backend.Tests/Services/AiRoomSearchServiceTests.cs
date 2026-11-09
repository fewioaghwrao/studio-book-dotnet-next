using Studiobook_backend.Application.Dtos.Ai;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Application.Services;
using Studiobook_backend.Domain.Entities;
using Studiobook_backend.Infrastructure.Persistence;
using Studiobook_backend.Infrastructure.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Services;

public class AiRoomSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_Queryが空の場合_ArgumentExceptionを投げる()
    {
        var service = new AiRoomSearchService(
            context: null!,
            openAiClient: null!);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SearchAsync("   "));

        Assert.Equal("検索文を入力してください。", ex.Message);
    }

    [Fact]
    public async Task SearchAsync_Queryが200文字を超える場合_ArgumentExceptionを投げる()
    {
        var service = new AiRoomSearchService(
            context: null!,
            openAiClient: null!);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SearchAsync(new string('あ', 201)));

        Assert.Equal("検索文は200文字以内で入力してください。", ex.Message);
    }

    // 現在：日本時間 2026/11/10(火) 10:00（UTC 1:00）
    private static FixedTimeProvider Clock => FixedTimeProvider.AtUtc(2026, 11, 10, 1, 0);

    [Fact]
    public async Task SearchAsync_日時の指定があると空きのあるスタジオを上位にし_空き状況を返す()
    {
        await using var context = TestDbContextFactory.Create();
        await SeedRoomsAsync(context);

        var client = new FakeOpenAiRoomSearchClient(new AiRoomSearchConditionsDto
        {
            Area = "渋谷区",
            Purpose = "撮影",
            Keywords = new List<string> { "撮影" },
            TimePreference = "夜",
            Date = "2026-11-14",
            StartTime = "18:00",
            DurationMinutes = 120
        });

        var service = new AiRoomSearchService(context, client, timeProvider: Clock);

        var result = await service.SearchAsync("来週土曜の18時から2時間、渋谷で撮影");

        // AI には日本時間の現在日時を渡す（「来週土曜」の解決用）
        Assert.Equal(new DateTime(2026, 11, 10, 10, 0, 0), client.LastNowJapan);

        Assert.NotNull(result.RequestedSlot);
        Assert.Equal(new DateTime(2026, 11, 14, 18, 0, 0), result.RequestedSlot!.StartAt);
        Assert.Equal(new DateTime(2026, 11, 14, 20, 0, 0), result.RequestedSlot.EndAt);
        Assert.False(result.RequestedSlot.DurationAssumed);
        Assert.False(result.Relaxed);

        // 新宿のスタジオはエリア条件で除外。評価の高い 2 は予約済みのため、空きのある 1 が先頭
        Assert.Equal(new[] { 1, 2, 3 }, result.Rooms.Select(x => x.Id));

        Assert.Equal("available", result.Rooms[0].Availability!.Status);
        Assert.True(result.Rooms[0].Availability!.IsAvailable);
        Assert.Equal("reserved", result.Rooms[1].Availability!.Status);
        Assert.Equal("outside_business_hours", result.Rooms[2].Availability!.Status);

        Assert.Equal("11/14（土）18:00〜20:00に空きがあります", result.Rooms[0].Reasons[0]);
        Assert.Contains("渋谷区にあります", result.Rooms[0].Reasons);
        Assert.Contains("名称・説明に「撮影」を含みます", result.Rooms[0].Reasons);

        // 空いていないスタジオに「空きがあります」は付けない
        Assert.DoesNotContain(result.Rooms[1].Reasons, x => x.Contains("空きがあります"));
        Assert.Contains("公開レビューの平均は5.0（1件）です", result.Rooms[1].Reasons);
    }

    [Fact]
    public async Task SearchAsync_キーワードに一致するスタジオがなければ条件を緩めて探し_その旨を返す()
    {
        await using var context = TestDbContextFactory.Create();
        await SeedRoomsAsync(context);

        var client = new FakeOpenAiRoomSearchClient(new AiRoomSearchConditionsDto
        {
            Area = "新宿区",
            Keywords = new List<string> { "サウナ" }
        });

        var service = new AiRoomSearchService(context, client, timeProvider: Clock);

        var result = await service.SearchAsync("新宿でサウナ付きのスタジオ");

        Assert.True(result.Relaxed);
        Assert.Contains(result.Notices, x => x.Contains("エリア・予算・人数の条件だけで探した"));

        var room = Assert.Single(result.Rooms);
        Assert.Equal(4, room.Id);
        Assert.Null(room.Availability);

        // 一致していない語を理由にしない
        Assert.DoesNotContain(room.Reasons, x => x.Contains("サウナ"));
        Assert.Contains("新宿区にあります", room.Reasons);
    }

    [Fact]
    public async Task SearchAsync_利用時間の指定がなければ1時間として判定し_その旨を返す()
    {
        await using var context = TestDbContextFactory.Create();
        await SeedRoomsAsync(context);

        var client = new FakeOpenAiRoomSearchClient(new AiRoomSearchConditionsDto
        {
            Area = "渋谷区",
            Date = "2026-11-14",
            StartTime = "10:00"
        });

        var service = new AiRoomSearchService(context, client, timeProvider: Clock);

        var result = await service.SearchAsync("11/14の10時から渋谷で");

        Assert.True(result.RequestedSlot!.DurationAssumed);
        Assert.Equal(new DateTime(2026, 11, 14, 11, 0, 0), result.RequestedSlot.EndAt);
        Assert.Contains(result.Notices, x => x.Contains("1時間として"));
        Assert.All(result.Rooms, x => Assert.Equal("available", x.Availability!.Status));
    }

    [Fact]
    public async Task SearchAsync_日時の指定がない検索では空き状況を返さない()
    {
        await using var context = TestDbContextFactory.Create();
        await SeedRoomsAsync(context);

        var client = new FakeOpenAiRoomSearchClient(new AiRoomSearchConditionsDto
        {
            Area = "渋谷区",
            TimePreference = "夜"
        });

        var service = new AiRoomSearchService(context, client, timeProvider: Clock);

        var result = await service.SearchAsync("渋谷で夜に使えるスタジオ");

        Assert.Null(result.RequestedSlot);
        Assert.All(result.Rooms, x => Assert.Null(x.Availability));

        // 評価の高い順（2 → 1・3 は評価なしのため料金の安い順）
        Assert.Equal(2, result.Rooms[0].Id);
    }

    [Fact]
    public void BuildSearchKeywords_時間帯の希望はキーワードに含めない()
    {
        var keywords = AiRoomSearchService.BuildSearchKeywords(new AiRoomSearchConditionsDto
        {
            Keyword = "ダンス 練習",
            Purpose = "ダンス",
            TimePreference = "夜",
            Keywords = new List<string> { "鏡", "ダンス" }
        });

        Assert.Equal(new[] { "ダンス", "練習", "鏡" }, keywords);
    }

    [Theory]
    [InlineData("2026-11-14", "18:00", "20:30", null, "2026-11-14 18:00", "2026-11-14 20:30", false)]
    [InlineData("2026-11-14", "18:00", null, 90, "2026-11-14 18:00", "2026-11-14 19:30", false)]
    [InlineData("2026-11-14", "9:00", null, null, "2026-11-14 09:00", "2026-11-14 10:00", true)]
    [InlineData("2026-11-14", "18:00", "17:00", null, "2026-11-14 18:00", "2026-11-14 19:00", true)]
    public void ParseRequestedSlot_終了は終了時刻_利用時間_1時間の順に決める(
        string date, string startTime, string? endTime, int? durationMinutes,
        string expectedStart, string expectedEnd, bool expectedAssumed)
    {
        var slot = AiRoomSearchService.ParseRequestedSlot(new AiRoomSearchConditionsDto
        {
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            DurationMinutes = durationMinutes
        });

        Assert.NotNull(slot);
        Assert.Equal(DateTime.Parse(expectedStart), slot!.StartAt);
        Assert.Equal(DateTime.Parse(expectedEnd), slot.EndAt);
        Assert.Equal(expectedAssumed, slot.DurationAssumed);
    }

    [Theory]
    [InlineData(null, "18:00")]
    [InlineData("2026-11-14", null)]
    [InlineData("来週土曜", "18:00")]
    [InlineData("2026-11-14", "夜")]
    public void ParseRequestedSlot_日付か開始時刻が読み取れなければnull(string? date, string? startTime)
    {
        var slot = AiRoomSearchService.ParseRequestedSlot(new AiRoomSearchConditionsDto
        {
            Date = date,
            StartTime = startTime
        });

        Assert.Null(slot);
    }

    private static async Task SeedRoomsAsync(AppDbContext context)
    {
        var now = new DateTime(2026, 11, 1);

        context.Rooms.AddRange(
            CreateRoom(1, "渋谷フォトスタジオ", "自然光が入る撮影スタジオです。", "東京都渋谷区神南1-1-1", 4000, 6, now),
            CreateRoom(2, "渋谷撮影ルームB", "撮影向けの白ホリゾントがあります。", "東京都渋谷区道玄坂2-2-2", 3000, 4, now),
            CreateRoom(3, "代々木スタジオ", "撮影やダンスに使えます。", "東京都渋谷区代々木3-3-3", 3500, 8, now),
            CreateRoom(4, "新宿ダンススタジオ", "ダンス練習向けの鏡張りスタジオ。", "東京都新宿区西新宿4-4-4", 2500, 10, now));

        // 土曜（6）の営業時間。3 は 17 時まで
        context.BusinessHours.AddRange(
            CreateSaturday(1, 1, 22),
            CreateSaturday(2, 2, 22),
            CreateSaturday(3, 3, 17),
            CreateSaturday(4, 4, 22));

        // 2 は 11/14 18:30〜19:30 が予約済み
        context.Reservations.Add(new Reservation
        {
            Id = 1,
            RoomId = 2,
            UserId = 20,
            StartAt = new DateTime(2026, 11, 14, 18, 30, 0),
            EndAt = new DateTime(2026, 11, 14, 19, 30, 0),
            Amount = 3000,
            Status = ReservationStatuses.Paid,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        context.Reviews.Add(new Review
        {
            Id = 1,
            RoomId = 2,
            UserId = 20,
            Score = 5,
            Content = "とても良かったです",
            PublicVisible = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        await context.SaveChangesAsync();
    }

    private static Room CreateRoom(int id, string name, string description, string address, int price, int capacity, DateTime now)
    {
        return new Room
        {
            Id = id,
            UserId = 10,
            Name = name,
            Description = description,
            ImageName = $"room{id}.jpg",
            PostalCode = "150-0001",
            Address = address,
            Price = price,
            Capacity = capacity,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    private static BusinessHour CreateSaturday(int id, int roomId, int endHour)
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

    private sealed class FakeOpenAiRoomSearchClient : IOpenAiRoomSearchClient
    {
        private readonly AiRoomSearchConditionsDto _conditions;

        public FakeOpenAiRoomSearchClient(AiRoomSearchConditionsDto conditions)
        {
            _conditions = conditions;
        }

        public DateTime? LastNowJapan { get; private set; }

        public Task<AiRoomSearchConditionsDto> AnalyzeQueryAsync(string query, DateTime? nowJapan = null)
        {
            LastNowJapan = nowJapan;
            return Task.FromResult(_conditions);
        }
    }
}