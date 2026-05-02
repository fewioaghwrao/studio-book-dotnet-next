using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Rooms;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Controllers;

public class RoomsControllerTests
{
    [Fact]
    public async Task GetList_ReturnsOk_WithRoomList()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");

        await SeedRoomAsync(
            context,
            roomId: 1,
            hostUserId: 10,
            name: "新宿スタジオ",
            description: "ダンス向け",
            price: 3000,
            address: "東京都新宿区サンプル");

        await SeedRoomAsync(
            context,
            roomId: 2,
            hostUserId: 10,
            name: "渋谷スタジオ",
            description: "撮影向け",
            price: 5000,
            address: "東京都渋谷区サンプル");

        var controller = CreateController(context);

        // Act
        var result = await controller.GetList(
            keyword: null,
            area: null,
            price: null,
            order: null,
            page: 1,
            pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<RoomListResponseDto>(okResult.Value);

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(2, response.TotalCount);
        Assert.Equal(1, response.TotalPages);
    }

    [Fact]
    public async Task GetList_AppliesQueryFilters()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");

        await SeedRoomAsync(
            context,
            roomId: 1,
            hostUserId: 10,
            name: "新宿ダンススタジオ",
            description: "ダンス練習向け",
            price: 3000,
            address: "東京都新宿区サンプル");

        await SeedRoomAsync(
            context,
            roomId: 2,
            hostUserId: 10,
            name: "渋谷撮影スタジオ",
            description: "撮影向け",
            price: 8000,
            address: "東京都渋谷区サンプル");

        var controller = CreateController(context);

        // Act
        var result = await controller.GetList(
            keyword: "ダンス",
            area: "新宿区",
            price: 5000,
            order: "priceAsc",
            page: 1,
            pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<RoomListResponseDto>(okResult.Value);

        var item = Assert.Single(response.Items);

        Assert.Equal(1, item.Id);
        Assert.Equal("新宿ダンススタジオ", item.Name);

        Assert.Equal("ダンス", response.Keyword);
        Assert.Equal("新宿区", response.Area);
        Assert.Equal(5000, response.Price);
        Assert.Equal("priceAsc", response.Order);
    }

    [Fact]
    public async Task GetDetail_ReturnsOk_WhenRoomExists()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, 10, "ホストA");

        await SeedRoomAsync(
            context,
            roomId: 1,
            hostUserId: 10,
            name: "新宿スタジオ",
            description: "ダンス向け",
            price: 3000,
            address: "東京都新宿区サンプル");

        await SeedBusinessHourAsync(
            context,
            roomId: 1,
            dayOfWeek: 1,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(18, 0),
            isHoliday: false);

        var controller = CreateController(context);

        // Act
        var result = await controller.GetDetail(roomId: 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<RoomDetailDto>(okResult.Value);

        Assert.Equal(1, response.Id);
        Assert.Equal("新宿スタジオ", response.Name);
        Assert.Equal("ホストA", response.HostName);
        Assert.Single(response.BusinessHours);
    }

    [Fact]
    public async Task GetDetail_ReturnsNotFound_WhenRoomDoesNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var controller = CreateController(context);

        // Act
        var result = await controller.GetDetail(roomId: 999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    private static RoomsController CreateController(AppDbContext context)
    {
        return new RoomsController(
            new RoomSearchService(context),
            new RoomDetailService(context));
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

    private static async Task SeedRoomAsync(
        AppDbContext context,
        int roomId,
        int hostUserId,
        string name,
        string description,
        int price,
        string address)
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
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(roomId),
            UpdatedAtUtc = DateTime.UtcNow.AddMinutes(roomId)
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedBusinessHourAsync(
        AppDbContext context,
        int roomId,
        int dayOfWeek,
        TimeOnly? startTime,
        TimeOnly? endTime,
        bool isHoliday)
    {
        context.BusinessHours.Add(new BusinessHour
        {
            RoomId = roomId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            IsHoliday = isHoliday,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }
}