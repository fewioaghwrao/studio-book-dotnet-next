using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Services;

public class AdminUserServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task SeedUsersAsync(AppDbContext context)
    {
        var adminRole = new Role { Id = 1, Name = "Admin" };
        var hostRole = new Role { Id = 2, Name = "Host" };
        var generalRole = new Role { Id = 3, Name = "GeneralUser" };

        var adminUser = new User
        {
            Id = 1,
            Name = "管理太郎",
            Kana = "カンリタロウ",
            Email = "admin@example.com",
            PasswordHash = "hash",
            PostalCode = "100-0001",
            Address = "東京都千代田区",
            PhoneNumber = "090-0000-0001",
            UsageType = "Admin",
            Enabled = true
        };

        var hostUser = new User
        {
            Id = 2,
            Name = "ホスト花子",
            Kana = "ホストハナコ",
            Email = "host@example.com",
            PasswordHash = "hash",
            PostalCode = "100-0002",
            Address = "東京都中央区",
            PhoneNumber = "090-0000-0002",
            UsageType = "Host",
            Enabled = true
        };

        var generalUser = new User
        {
            Id = 3,
            Name = "一般次郎",
            Kana = "イッパンジロウ",
            Email = "general@example.com",
            PasswordHash = "hash",
            PostalCode = "100-0003",
            Address = "東京都港区",
            PhoneNumber = "090-0000-0003",
            UsageType = "General",
            Enabled = false
        };

        context.Roles.AddRange(adminRole, hostRole, generalRole);
        context.Users.AddRange(adminUser, hostUser, generalUser);

        context.UserRoles.AddRange(
            new UserRole
            {
                Id = 1,
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                User = adminUser,
                Role = adminRole
            },
            new UserRole
            {
                Id = 2,
                UserId = hostUser.Id,
                RoleId = hostRole.Id,
                User = hostUser,
                Role = hostRole
            },
            new UserRole
            {
                Id = 3,
                UserId = generalUser.Id,
                RoleId = generalRole.Id,
                User = generalUser,
                Role = generalRole
            }
        );

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetListAsync_条件なしの場合_Id昇順で一覧を返す()
    {
        await using var context = CreateDbContext();
        await SeedUsersAsync(context);

        var service = new AdminUserService(context);

        var result = await service.GetListAsync(
            keyword: null,
            page: 1,
            pageSize: 10);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);

        Assert.Equal(1, result.Items[0].Id);
        Assert.Equal(2, result.Items[1].Id);
        Assert.Equal(3, result.Items[2].Id);

        Assert.Equal("管理太郎", result.Items[0].Name);
        Assert.Equal("ホスト花子", result.Items[1].Name);
        Assert.Equal("一般次郎", result.Items[2].Name);
    }

    [Fact]
    public async Task GetListAsync_keyword指定の場合_NameKanaEmailで部分一致検索する()
    {
        await using var context = CreateDbContext();
        await SeedUsersAsync(context);

        var service = new AdminUserService(context);

        var result = await service.GetListAsync(
            keyword: "host",
            page: 1,
            pageSize: 10);

        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal("host@example.com", result.Items[0].Email);
    }

    [Fact]
    public async Task GetListAsync_pageとpageSizeが0以下の場合_既定値に丸める()
    {
        await using var context = CreateDbContext();
        await SeedUsersAsync(context);

        var service = new AdminUserService(context);

        var result = await service.GetListAsync(
            keyword: null,
            page: 0,
            pageSize: 0);

        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task GetListAsync_pageSizeが100を超える場合_100に丸める()
    {
        await using var context = CreateDbContext();

        var generalRole = new Role { Id = 1, Name = "GeneralUser" };
        context.Roles.Add(generalRole);

        for (var i = 1; i <= 120; i++)
        {
            var user = new User
            {
                Id = i,
                Name = $"ユーザー{i}",
                Kana = $"ユーザー{i}",
                Email = $"user{i}@example.com",
                PasswordHash = "hash",
                PostalCode = "100-0001",
                Address = "東京都",
                PhoneNumber = $"090-0000-{i:0000}",
                UsageType = "General",
                Enabled = true
            };

            context.Users.Add(user);
            context.UserRoles.Add(new UserRole
            {
                Id = i,
                UserId = user.Id,
                RoleId = generalRole.Id,
                User = user,
                Role = generalRole
            });
        }

        await context.SaveChangesAsync();

        var service = new AdminUserService(context);

        var result = await service.GetListAsync(
            keyword: null,
            page: 1,
            pageSize: 999);

        Assert.Equal(120, result.TotalCount);
        Assert.Equal(100, result.PageSize);
        Assert.Equal(100, result.Items.Count);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetListAsync_ロール表示ラベルを返す()
    {
        await using var context = CreateDbContext();
        await SeedUsersAsync(context);

        var service = new AdminUserService(context);

        var result = await service.GetListAsync(
            keyword: null,
            page: 1,
            pageSize: 10);

        Assert.Equal("Admin", result.Items[0].RoleName);
        Assert.Equal("管理者", result.Items[0].RoleLabel);

        Assert.Equal("Host", result.Items[1].RoleName);
        Assert.Equal("スタジオ提供者", result.Items[1].RoleLabel);

        Assert.Equal("GeneralUser", result.Items[2].RoleName);
        Assert.Equal("会員", result.Items[2].RoleLabel);
    }

    [Fact]
    public async Task GetDetailAsync_存在するUserIdの場合_詳細DTOを返す()
    {
        await using var context = CreateDbContext();
        await SeedUsersAsync(context);

        var service = new AdminUserService(context);

        var result = await service.GetDetailAsync(2);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Id);
        Assert.Equal("ホスト花子", result.Name);
        Assert.Equal("ホストハナコ", result.Kana);
        Assert.Equal("host@example.com", result.Email);
        Assert.Equal("Host", result.RoleName);
        Assert.Equal("スタジオ提供者", result.RoleLabel);
        Assert.True(result.Enabled);
    }

    [Fact]
    public async Task GetDetailAsync_存在しないUserIdの場合_nullを返す()
    {
        await using var context = CreateDbContext();
        await SeedUsersAsync(context);

        var service = new AdminUserService(context);

        var result = await service.GetDetailAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDetailAsync_複数ロールがある場合_Adminを優先する()
    {
        await using var context = CreateDbContext();

        var adminRole = new Role { Id = 1, Name = "Admin" };
        var hostRole = new Role { Id = 2, Name = "Host" };

        var user = new User
        {
            Id = 1,
            Name = "複数ロール太郎",
            Kana = "フクスウロールタロウ",
            Email = "multi@example.com",
            PasswordHash = "hash",
            PostalCode = "100-0001",
            Address = "東京都",
            PhoneNumber = "090-0000-0001",
            UsageType = "Admin",
            Enabled = true
        };

        context.Roles.AddRange(adminRole, hostRole);
        context.Users.Add(user);

        context.UserRoles.AddRange(
            new UserRole
            {
                Id = 1,
                UserId = user.Id,
                RoleId = hostRole.Id,
                User = user,
                Role = hostRole
            },
            new UserRole
            {
                Id = 2,
                UserId = user.Id,
                RoleId = adminRole.Id,
                User = user,
                Role = adminRole
            }
        );

        await context.SaveChangesAsync();

        var service = new AdminUserService(context);

        var result = await service.GetDetailAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Admin", result!.RoleName);
        Assert.Equal("管理者", result.RoleLabel);
    }
}