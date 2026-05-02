using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Admin;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Controllers;

public class AdminUsersControllerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetList_正常時_Okを返す()
    {
        await using var context = CreateDbContext();

        var role = new Role
        {
            Id = 1,
            Name = "GeneralUser"
        };

        var user = new User
        {
            Id = 1,
            Name = "テスト太郎",
            Kana = "テストタロウ",
            Email = "test@example.com",
            PasswordHash = "hash",
            PostalCode = "100-0001",
            Address = "東京都千代田区",
            PhoneNumber = "090-0000-0001",
            UsageType = "General",
            Enabled = true
        };

        context.Roles.Add(role);
        context.Users.Add(user);
        context.UserRoles.Add(new UserRole
        {
            Id = 1,
            UserId = user.Id,
            RoleId = role.Id,
            User = user,
            Role = role
        });

        await context.SaveChangesAsync();

        var service = new AdminUserService(context);
        var controller = new AdminUsersController(service);

        var result = await controller.GetList(
            keyword: null,
            page: 1,
            pageSize: 10);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AdminUserListResponseDto>(okResult.Value);

        Assert.Single(body.Items);
        Assert.Equal(1, body.TotalCount);
        Assert.Equal("テスト太郎", body.Items[0].Name);
    }

    [Fact]
    public async Task GetDetail_存在するUserIdの場合_Okを返す()
    {
        await using var context = CreateDbContext();

        var role = new Role
        {
            Id = 1,
            Name = "GeneralUser"
        };

        var user = new User
        {
            Id = 1,
            Name = "テスト太郎",
            Kana = "テストタロウ",
            Email = "test@example.com",
            PasswordHash = "hash",
            PostalCode = "100-0001",
            Address = "東京都千代田区",
            PhoneNumber = "090-0000-0001",
            UsageType = "General",
            Enabled = true
        };

        context.Roles.Add(role);
        context.Users.Add(user);
        context.UserRoles.Add(new UserRole
        {
            Id = 1,
            UserId = user.Id,
            RoleId = role.Id,
            User = user,
            Role = role
        });

        await context.SaveChangesAsync();

        var service = new AdminUserService(context);
        var controller = new AdminUsersController(service);

        var result = await controller.GetDetail(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AdminUserDetailDto>(okResult.Value);

        Assert.Equal(1, body.Id);
        Assert.Equal("テスト太郎", body.Name);
        Assert.Equal("GeneralUser", body.RoleName);
        Assert.Equal("会員", body.RoleLabel);
    }

    [Fact]
    public async Task GetDetail_存在しないUserIdの場合_NotFoundを返す()
    {
        await using var context = CreateDbContext();

        var service = new AdminUserService(context);
        var controller = new AdminUsersController(service);

        var result = await controller.GetDetail(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
