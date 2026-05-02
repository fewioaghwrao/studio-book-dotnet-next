using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Controllers;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Controllers;

public class HostPriceRulesControllerTests
{
    [Fact]
    public async Task Get_ReturnsOk_WhenRoomBelongsToLoginUser()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        context.Rooms.Add(new Room
        {
            Id = 1,
            UserId = 10,
            Name = "テストスタジオ"
        });

        context.PriceRules.Add(new PriceRule
        {
            Id = 1,
            RoomId = 1,
            RuleType = "multiplier",
            Weekday = 1,
            StartHour = new TimeOnly(19, 0),
            EndHour = new TimeOnly(22, 0),
            Multiplier = 1.5m,
            FlatFee = null,
            Note = "夜間料金",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new HostPriceRuleService(context);
        var controller = CreateController(service, userId: 10);

        // Act
        var result = await controller.Get(roomId: 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PriceRulesResponseDto>(okResult.Value);

        Assert.Equal(1, response.RoomId);
        Assert.Equal("テストスタジオ", response.RoomName);
        Assert.Single(response.Rules);
        Assert.Equal("multiplier", response.Rules[0].RuleType);
        Assert.Equal("19:00", response.Rules[0].StartHour);
        Assert.Equal("22:00", response.Rules[0].EndHour);
        Assert.Equal(1.5m, response.Rules[0].Multiplier);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenRoomDoesNotBelongToLoginUser()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        context.Rooms.Add(new Room
        {
            Id = 1,
            UserId = 99,
            Name = "他人のスタジオ"
        });

        await context.SaveChangesAsync();

        var service = new HostPriceRuleService(context);
        var controller = CreateController(service, userId: 10);

        // Act
        var result = await controller.Get(roomId: 1);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task Create_ReturnsNoContent_WhenRequestIsValid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        context.Rooms.Add(new Room
        {
            Id = 1,
            UserId = 10,
            Name = "テストスタジオ"
        });

        await context.SaveChangesAsync();

        var service = new HostPriceRuleService(context);
        var controller = CreateController(service, userId: 10);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "multiplier",
            Weekday = 1,
            StartHour = "19:00",
            EndHour = "22:00",
            Multiplier = 1.5m,
            FlatFee = null,
            Note = "夜間料金"
        };

        // Act
        var result = await controller.Create(roomId: 1, request);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var savedRule = Assert.Single(context.PriceRules);
        Assert.Equal(1, savedRule.RoomId);
        Assert.Equal("multiplier", savedRule.RuleType);
        Assert.Equal(1, savedRule.Weekday);
        Assert.Equal(new TimeOnly(19, 0), savedRule.StartHour);
        Assert.Equal(new TimeOnly(22, 0), savedRule.EndHour);
        Assert.Equal(1.5m, savedRule.Multiplier);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenRuleTypeIsEmpty()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        context.Rooms.Add(new Room
        {
            Id = 1,
            UserId = 10,
            Name = "テストスタジオ"
        });

        await context.SaveChangesAsync();

        var service = new HostPriceRuleService(context);
        var controller = CreateController(service, userId: 10);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "",
            Weekday = 1,
            StartHour = "19:00",
            EndHour = "22:00",
            Multiplier = 1.5m,
            FlatFee = null,
            Note = "不正データ"
        };

        // Act
        var result = await controller.Create(roomId: 1, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);

        Assert.Empty(context.PriceRules);
    }

    [Fact]
    public async Task Create_ReturnsNotFound_WhenRoomDoesNotBelongToLoginUser()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        context.Rooms.Add(new Room
        {
            Id = 1,
            UserId = 99,
            Name = "他人のスタジオ"
        });

        await context.SaveChangesAsync();

        var service = new HostPriceRuleService(context);
        var controller = CreateController(service, userId: 10);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "multiplier",
            Weekday = 1,
            StartHour = "19:00",
            EndHour = "22:00",
            Multiplier = 1.5m,
            FlatFee = null,
            Note = "夜間料金"
        };

        // Act
        var result = await controller.Create(roomId: 1, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);

        Assert.Empty(context.PriceRules);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenRuleExistsAndRoomBelongsToLoginUser()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        context.Rooms.Add(new Room
        {
            Id = 1,
            UserId = 10,
            Name = "テストスタジオ"
        });

        context.PriceRules.Add(new PriceRule
        {
            Id = 1,
            RoomId = 1,
            RuleType = "flat_fee",
            Weekday = 6,
            StartHour = null,
            EndHour = null,
            Multiplier = null,
            FlatFee = 1000,
            Note = "休日固定費",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new HostPriceRuleService(context);
        var controller = CreateController(service, userId: 10);

        // Act
        var result = await controller.Delete(roomId: 1, ruleId: 1);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.Empty(context.PriceRules);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenRuleDoesNotBelongToLoginUser()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        context.Rooms.Add(new Room
        {
            Id = 1,
            UserId = 99,
            Name = "他人のスタジオ"
        });

        context.PriceRules.Add(new PriceRule
        {
            Id = 1,
            RoomId = 1,
            RuleType = "flat_fee",
            Weekday = 6,
            StartHour = null,
            EndHour = null,
            Multiplier = null,
            FlatFee = 1000,
            Note = "休日固定費",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new HostPriceRuleService(context);
        var controller = CreateController(service, userId: 10);

        // Act
        var result = await controller.Delete(roomId: 1, ruleId: 1);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);

        Assert.Single(context.PriceRules);
    }

    private static HostPriceRulesController CreateController(
        HostPriceRuleService service,
        int userId)
    {
        var controller = new HostPriceRulesController(service);

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
}