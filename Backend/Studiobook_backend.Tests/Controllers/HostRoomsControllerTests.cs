using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos;
using Studiobook_backend.Entities;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Controllers;

public class HostRoomsControllerTests
{
    [Fact]
    public async Task Index_ReturnsOk_WithOnlyLoginHostRooms()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedHostUserAsync(context, userId: 99, name: "ホストB");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");
        await SeedRoomAsync(context, roomId: 2, hostUserId: 10, name: "渋谷スタジオ");
        await SeedRoomAsync(context, roomId: 3, hostUserId: 99, name: "他ホストのスタジオ");

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.Index(
            keyword: null,
            page: 1,
            pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResponseDto<HostRoomListItemDto>>(okResult.Value);

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(1, response.Items[0].Id);
        Assert.Equal("新宿スタジオ", response.Items[0].Name);
        Assert.Equal(2, response.Items[1].Id);
        Assert.Equal("渋谷スタジオ", response.Items[1].Name);

        Assert.Equal(1, response.Page);
        Assert.Equal(10, response.PageSize);
        Assert.Equal(2, response.TotalCount);
        Assert.Equal(1, response.TotalPages);
    }

    [Fact]
    public async Task Index_FiltersByKeyword()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");
        await SeedRoomAsync(context, roomId: 2, hostUserId: 10, name: "渋谷スタジオ");

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.Index(
            keyword: " 新宿 ",
            page: 1,
            pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResponseDto<HostRoomListItemDto>>(okResult.Value);

        Assert.Single(response.Items);
        Assert.Equal(1, response.Items[0].Id);
        Assert.Equal("新宿スタジオ", response.Items[0].Name);
        Assert.Equal(1, response.TotalCount);
    }

    [Fact]
    public async Task Index_NormalizesPageAndPageSize()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.Index(
            keyword: null,
            page: 0,
            pageSize: 0);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResponseDto<HostRoomListItemDto>>(okResult.Value);

        Assert.Equal(1, response.Page);
        Assert.Equal(1, response.PageSize);
        Assert.Single(response.Items);
    }

    [Fact]
    public async Task Index_ClampsPageSizeTo50()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");

        for (var i = 1; i <= 60; i++)
        {
            await SeedRoomAsync(
                context,
                roomId: i,
                hostUserId: 10,
                name: $"スタジオ{i:00}");
        }

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.Index(
            keyword: null,
            page: 1,
            pageSize: 100);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResponseDto<HostRoomListItemDto>>(okResult.Value);

        Assert.Equal(1, response.Page);
        Assert.Equal(50, response.PageSize);
        Assert.Equal(50, response.Items.Count);
        Assert.Equal(60, response.TotalCount);
        Assert.Equal(2, response.TotalPages);
    }

    [Fact]
    public async Task Index_ReturnsUnauthorized_WhenUserIdClaimIsMissing()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var controller = new HostRoomsController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        // Act
        var result = await controller.Index(
            keyword: null,
            page: 1,
            pageSize: 10);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var error = Assert.IsType<ApiErrorResponseDto>(unauthorizedResult.Value);

        Assert.Equal("AUTH_REQUIRED", error.Code);
        Assert.Equal("ログインが必要です。", error.Message);
    }

    [Fact]
    public async Task Index_ReturnsEmptyItems_WhenHostHasNoRooms()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.Index(
            keyword: null,
            page: 1,
            pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResponseDto<HostRoomListItemDto>>(okResult.Value);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
        Assert.Equal(1, response.TotalPages);
    }

    [Fact]
    public async Task Show_ReturnsOk_WhenRoomBelongsToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.Show(id: 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<HostRoomDetailDto>(okResult.Value);

        Assert.Equal(1, response.Id);
        Assert.Equal("新宿スタジオ", response.Name);
        Assert.Equal("room01.jpg", response.ImageName);
        Assert.Equal("テスト用スタジオ", response.Description);
        Assert.Equal(3000, response.Price);
        Assert.Equal(5, response.Capacity);
        Assert.Equal("100-0001", response.PostalCode);
        Assert.Equal("東京都テスト区サンプル1", response.Address);
    }

    [Fact]
    public async Task Show_ReturnsNotFound_WhenRoomDoesNotBelongToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedHostUserAsync(context, userId: 99, name: "ホストB");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 99, name: "他ホストのスタジオ");

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.Show(id: 1);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<ApiErrorResponseDto>(notFoundResult.Value);

        Assert.Equal("ROOM_NOT_FOUND", error.Code);
        Assert.Equal("スタジオが見つかりません。", error.Message);
    }

    [Fact]
    public async Task Show_ReturnsNotFound_WhenRoomDoesNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.Show(id: 999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<ApiErrorResponseDto>(notFoundResult.Value);

        Assert.Equal("ROOM_NOT_FOUND", error.Code);
        Assert.Equal("スタジオが見つかりません。", error.Message);
    }

    [Fact]
    public async Task Show_ReturnsUnauthorized_WhenUserIdClaimIsMissing()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var controller = new HostRoomsController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        // Act
        var result = await controller.Show(id: 1);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var error = Assert.IsType<ApiErrorResponseDto>(unauthorizedResult.Value);

        Assert.Equal("AUTH_REQUIRED", error.Code);
        Assert.Equal("ログインが必要です。", error.Message);
    }

    [Fact]
    public async Task Show_ReturnsOk_WhenOnlyNameIdentifierClaimExists()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");

        var controller = CreateControllerWithNameIdentifierOnly(context, userId: 10);

        // Act
        var result = await controller.Show(id: 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<HostRoomDetailDto>(okResult.Value);

        Assert.Equal(1, response.Id);
        Assert.Equal("新宿スタジオ", response.Name);
    }

    private static HostRoomsController CreateController(
        AppDbContext context,
        int userId)
    {
        var controller = new HostRoomsController(context);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, "Host")
                }, "TestAuth"))
            }
        };

        return controller;
    }

    private static HostRoomsController CreateControllerWithNameIdentifierOnly(
        AppDbContext context,
        int userId)
    {
        var controller = new HostRoomsController(context);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, "Host")
                }, "TestAuth"))
            }
        };

        return controller;
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
        string name)
    {
        context.Rooms.Add(new Room
        {
            Id = roomId,
            UserId = hostUserId,
            Name = name,
            ImageName = $"room{roomId:00}.jpg",
            Description = "テスト用スタジオ",
            Price = 3000,
            Capacity = 5,
            PostalCode = "100-0001",
            Address = $"東京都テスト区サンプル{roomId}",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }
}