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

public class HostBusinessHoursControllerTests
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
    public async Task Get_自分のRoomの場合_Okを返す()
    {
        await using var context = CreateDbContext();
        await SeedRoomAsync(context);

        var service = new HostBusinessHourService(context);
        var controller = new HostBusinessHoursController(service);
        SetUser(controller, userId: 1);

        var result = await controller.Get(roomId: 1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<BusinessHoursResponseDto>(okResult.Value);

        Assert.Equal(1, body.RoomId);
        Assert.Equal("A Studio", body.RoomName);
        Assert.Equal(7, body.Rows.Count);
    }

    [Fact]
    public async Task Get_存在しないRoomの場合_NotFoundを返す()
    {
        await using var context = CreateDbContext();
        await SeedRoomAsync(context);

        var service = new HostBusinessHourService(context);
        var controller = new HostBusinessHoursController(service);
        SetUser(controller, userId: 1);

        var result = await controller.Get(roomId: 999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_有効なリクエストの場合_NoContentを返す()
    {
        await using var context = CreateDbContext();
        await SeedRoomAsync(context);

        var service = new HostBusinessHourService(context);
        var controller = new HostBusinessHoursController(service);
        SetUser(controller, userId: 1);

        var result = await controller.Update(
            roomId: 1,
            request: CreateValidRequest());

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_不正な時刻の場合_BadRequestを返す()
    {
        await using var context = CreateDbContext();
        await SeedRoomAsync(context);

        var service = new HostBusinessHourService(context);
        var controller = new HostBusinessHoursController(service);
        SetUser(controller, userId: 1);

        var request = CreateValidRequest();
        request.Rows[0].StartTime = "09:10";

        var result = await controller.Update(roomId: 1, request);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}