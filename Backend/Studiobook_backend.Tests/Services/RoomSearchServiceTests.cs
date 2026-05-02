using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Services;

public class RoomSearchServiceTests
{
    [Fact]
    public async Task GetListAsync_ReturnsAllRooms_WhenNoFilters()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");

        await SeedRoomAsync(
            context,
            roomId: 1,
            hostUserId: 10,
            name: "新宿スタジオ",
            description: "ダンス練習向け",
            price: 3000,
            address: "東京都新宿区サンプル",
            createdAtUtc: new DateTime(2026, 5, 1));

        await SeedRoomAsync(
            context,
            roomId: 2,
            hostUserId: 10,
            name: "渋谷スタジオ",
            description: "撮影向け",
            price: 5000,
            address: "東京都渋谷区サンプル",
            createdAtUtc: new DateTime(2026, 5, 2));

        var service = new RoomSearchService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: null,
            area: null,
            price: null,
            order: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Equal(2, result.Items.Count);

        // default: CreatedAtUtc desc, Id desc
        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal("渋谷スタジオ", result.Items[0].Name);

        Assert.Equal(1, result.Items[1].Id);
        Assert.Equal("新宿スタジオ", result.Items[1].Name);

        Assert.Equal("", result.Keyword);
        Assert.Equal("", result.Area);
        Assert.Null(result.Price);
        Assert.Equal("createdAtDesc", result.Order);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetListAsync_FiltersByKeyword_Name()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");

        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", "ダンス練習向け", 3000, "東京都新宿区サンプル");
        await SeedRoomAsync(context, 2, 10, "渋谷スタジオ", "撮影向け", 5000, "東京都渋谷区サンプル");

        var service = new RoomSearchService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: " 新宿 ",
            area: null,
            price: null,
            order: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].Id);
        Assert.Equal("新宿スタジオ", result.Items[0].Name);
        Assert.Equal("新宿", result.Keyword);
    }

    [Fact]
    public async Task GetListAsync_FiltersByKeyword_Description()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");

        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", "ダンス練習向け", 3000, "東京都新宿区サンプル");
        await SeedRoomAsync(context, 2, 10, "渋谷スタジオ", "写真撮影向け", 5000, "東京都渋谷区サンプル");

        var service = new RoomSearchService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: "写真",
            area: null,
            price: null,
            order: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal("渋谷スタジオ", result.Items[0].Name);
    }

    [Fact]
    public async Task GetListAsync_FiltersByArea()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");

        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", "ダンス練習向け", 3000, "東京都新宿区サンプル");
        await SeedRoomAsync(context, 2, 10, "渋谷スタジオ", "撮影向け", 5000, "東京都渋谷区サンプル");

        var service = new RoomSearchService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: null,
            area: " 渋谷区 ",
            price: null,
            order: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal("渋谷スタジオ", result.Items[0].Name);
        Assert.Equal("渋谷区", result.Area);
    }

    [Fact]
    public async Task GetListAsync_FiltersByPrice()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");

        await SeedRoomAsync(context, 1, 10, "安めスタジオ", "少人数向け", 3000, "東京都新宿区サンプル");
        await SeedRoomAsync(context, 2, 10, "高めスタジオ", "広め", 8000, "東京都渋谷区サンプル");

        var service = new RoomSearchService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: null,
            area: null,
            price: 5000,
            order: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].Id);
        Assert.Equal("安めスタジオ", result.Items[0].Name);
        Assert.Equal(5000, result.Price);
    }

    [Fact]
    public async Task GetListAsync_IgnoresPriceFilter_WhenPriceIsZeroOrLess()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");

        await SeedRoomAsync(context, 1, 10, "安めスタジオ", "少人数向け", 3000, "東京都新宿区サンプル");
        await SeedRoomAsync(context, 2, 10, "高めスタジオ", "広め", 8000, "東京都渋谷区サンプル");

        var service = new RoomSearchService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: null,
            area: null,
            price: 0,
            order: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(0, result.Price);
    }

    [Fact]
    public async Task GetListAsync_OrdersByPriceAsc()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");

        await SeedRoomAsync(context, 1, 10, "高めスタジオ", "広め", 8000, "東京都新宿区サンプル", new DateTime(2026, 5, 1));
        await SeedRoomAsync(context, 2, 10, "安めスタジオ", "少人数向け", 3000, "東京都渋谷区サンプル", new DateTime(2026, 5, 2));
        await SeedRoomAsync(context, 3, 10, "中間スタジオ", "標準", 5000, "東京都品川区サンプル", new DateTime(2026, 5, 3));

        var service = new RoomSearchService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: null,
            area: null,
            price: null,
            order: "priceAsc",
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal(3, result.Items[1].Id);
        Assert.Equal(1, result.Items[2].Id);
        Assert.Equal("priceAsc", result.Order);
    }

    [Fact]
    public async Task GetListAsync_CalculatesAverageScoreAndReviewCount_FromPublicReviewsOnly()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ユーザーA");
        await SeedGuestUserAsync(context, 21, "ユーザーB");
        await SeedGuestUserAsync(context, 22, "ユーザーC");

        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", "説明", 3000, "東京都新宿区サンプル");

        await SeedReviewAsync(context, 1, 1, 20, 5, true);
        await SeedReviewAsync(context, 2, 1, 21, 3, true);
        await SeedReviewAsync(context, 3, 1, 22, 1, false);

        var service = new RoomSearchService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: null,
            area: null,
            price: null,
            order: null,
            page: 1,
            pageSize: 10);

        // Assert
        var item = Assert.Single(result.Items);

        Assert.Equal(4.0, item.AverageScore);
        Assert.Equal(2, item.ReviewCount);
    }

    [Fact]
    public async Task GetListAsync_ReturnsNullAverageScoreAndZeroReviewCount_WhenNoPublicReviews()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedGuestUserAsync(context, 20, "ユーザーA");

        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", "説明", 3000, "東京都新宿区サンプル");

        await SeedReviewAsync(context, 1, 1, 20, 1, false);

        var service = new RoomSearchService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: null,
            area: null,
            price: null,
            order: null,
            page: 1,
            pageSize: 10);

        // Assert
        var item = Assert.Single(result.Items);

        Assert.Null(item.AverageScore);
        Assert.Equal(0, item.ReviewCount);
    }

    [Fact]
    public async Task GetListAsync_AppliesPaging()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");

        await SeedRoomAsync(context, 1, 10, "スタジオ1", "説明", 3000, "東京都新宿区サンプル", new DateTime(2026, 5, 1));
        await SeedRoomAsync(context, 2, 10, "スタジオ2", "説明", 3000, "東京都新宿区サンプル", new DateTime(2026, 5, 2));
        await SeedRoomAsync(context, 3, 10, "スタジオ3", "説明", 3000, "東京都新宿区サンプル", new DateTime(2026, 5, 3));

        var service = new RoomSearchService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: null,
            area: null,
            price: null,
            order: null,
            page: 2,
            pageSize: 2);

        // Assert
        Assert.Single(result.Items);

        // default order: ID 3, 2, 1
        Assert.Equal(1, result.Items[0].Id);

        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetListAsync_NormalizesInvalidPageAndPageSize()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");
        await SeedRoomAsync(context, 1, 10, "新宿スタジオ", "説明", 3000, "東京都新宿区サンプル");

        var service = new RoomSearchService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: null,
            area: null,
            price: null,
            order: null,
            page: 0,
            pageSize: 0);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetListAsync_ClampsPageSizeTo50()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");

        for (var i = 1; i <= 60; i++)
        {
            await SeedRoomAsync(
                context,
                roomId: i,
                hostUserId: 10,
                name: $"スタジオ{i:00}",
                description: "説明",
                price: 3000 + i,
                address: $"東京都新宿区サンプル{i}",
                createdAtUtc: new DateTime(2026, 5, 1).AddDays(i));
        }

        var service = new RoomSearchService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: null,
            area: null,
            price: null,
            order: null,
            page: 1,
            pageSize: 100);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.Equal(50, result.Items.Count);
        Assert.Equal(60, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetListAsync_ReturnsEmptyItems_WhenNoRooms()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var service = new RoomSearchService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: null,
            area: null,
            price: null,
            order: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    private static async Task SeedHostUserAsync(
        AppDbContext context,
        int userId,
        string name)
    {
        context.Users.Add(new User
        {
            Id = userId,
            Name = name,
            Kana = "テストホスト",
            Email = $"host{userId}@example.com",
            PasswordHash = "dummy_hash",
            PostalCode = "100-0001",
            Address = "東京都千代田区テスト",
            PhoneNumber = "090-0000-0000",
            UsageType = "host",
            Enabled = true
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedGuestUserAsync(
        AppDbContext context,
        int userId,
        string name)
    {
        context.Users.Add(new User
        {
            Id = userId,
            Name = name,
            Kana = "テストゲスト",
            Email = $"guest{userId}@example.com",
            PasswordHash = "dummy_hash",
            PostalCode = "100-0001",
            Address = "東京都千代田区テスト",
            PhoneNumber = "090-0000-0000",
            UsageType = "general",
            Enabled = true
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedRoomAsync(
        AppDbContext context,
        int roomId,
        int hostUserId,
        string name,
        string description,
        int price,
        string address,
        DateTime? createdAtUtc = null)
    {
        context.Rooms.Add(new Room
        {
            Id = roomId,
            UserId = hostUserId,
            Name = name,
            ImageName = $"room{roomId:00}.jpg",
            Description = description,
            Price = price,
            Capacity = 5,
            PostalCode = "100-0001",
            Address = address,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
            UpdatedAtUtc = createdAtUtc ?? DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedReviewAsync(
        AppDbContext context,
        int reviewId,
        int roomId,
        int userId,
        int score,
        bool publicVisible)
    {
        context.Reviews.Add(new Review
        {
            Id = reviewId,
            RoomId = roomId,
            UserId = userId,
            Score = score,
            Content = $"レビュー{reviewId}",
            PublicVisible = publicVisible,
            HiddenReason = publicVisible ? null : "非公開理由",
            HostReply = null,
            HostReplyAt = null,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(reviewId),
            UpdatedAtUtc = DateTime.UtcNow.AddMinutes(reviewId)
        });

        await context.SaveChangesAsync();
    }
}