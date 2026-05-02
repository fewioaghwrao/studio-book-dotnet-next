using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Controllers;

public class HostClosuresControllerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static void SetUser(ControllerBase controller, int userId)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user
            }
        };
    }

    private static async Task SeedAsync(AppDbContext context)
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

        context.Users.Add(host);

        context.Rooms.Add(new Room
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
        });

        context.Closures.Add(new Closure
        {
            Id = 1,
            RoomId = 1,
            StartAt = new DateTime(2026, 5, 10, 0, 0, 0),
            EndAt = new DateTime(2026, 5, 11, 0, 0, 0),
            Reason = "設備点検"
        });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetList_自分のRoomの場合_Okを返す()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new HostClosureService(context);
        var controller = new HostClosuresController(service);
        SetUser(controller, userId: 1);

        // Act
        var result = await controller.GetList(roomId: 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<List<ClosureDto>>(okResult.Value);

        Assert.Single(body);
        Assert.Equal(1, body[0].Id);
    }

    [Fact]
    public async Task GetEvents_自分のRoomの場合_Okを返す()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new HostClosureService(context);
        var controller = new HostClosuresController(service);
        SetUser(controller, userId: 1);

        // Act
        var result = await controller.GetEvents(roomId: 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<List<ClosureEventDto>>(okResult.Value);

        Assert.Single(body);
        Assert.Equal("休館: 設備点検", body[0].Title);
    }

    [Fact]
    public async Task Create_正常なリクエストの場合_CreatedAtActionを返す()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new HostClosureService(context);
        var controller = new HostClosuresController(service);
        SetUser(controller, userId: 1);

        var request = new CreateClosureRequest
        {
            StartAt = new DateTime(2026, 6, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 6, 1, 18, 0, 0),
            Reason = "臨時休業"
        };

        // Act
        var result = await controller.Create(roomId: 1, request);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(HostClosuresController.GetList), created.ActionName);

        var body = Assert.IsType<ClosureDto>(created.Value);
        Assert.Equal("臨時休業", body.Reason);
    }

    [Fact]
    public async Task Delete_存在する休館日の場合_NoContentを返す()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new HostClosureService(context);
        var controller = new HostClosuresController(service);
        SetUser(controller, userId: 1);

        // Act
        var result = await controller.Delete(
            roomId: 1,
            closureId: 1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}
