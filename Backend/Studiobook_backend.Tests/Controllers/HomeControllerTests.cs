using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Home;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Controllers;

public class HomeControllerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Get_正常時_Okを返す()
    {
        // Arrange
        await using var context = CreateDbContext();

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
            Description = "A",
            Price = 3000,
            Capacity = 4,
            PostalCode = "100-0001",
            Address = "東京都渋谷区",
            CreatedAtUtc = new DateTime(2026, 5, 1),
            UpdatedAtUtc = new DateTime(2026, 5, 1)
        });

        await context.SaveChangesAsync();

        var service = new HomeService(context);
        var controller = new HomeController(service);

        // Act
        var result = await controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<HomeResponseDto>(okResult.Value);

        Assert.Single(body.PopularRooms);
        Assert.Single(body.NewRooms);
    }
}