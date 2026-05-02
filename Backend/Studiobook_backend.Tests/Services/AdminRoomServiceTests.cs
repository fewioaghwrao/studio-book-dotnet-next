using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Dtos.Admin;

namespace Studiobook_backend.Tests.Services;

public class AdminRoomServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task SeedHostAndRoomsAsync(AppDbContext context)
    {
        var hostRole = new Role
        {
            Id = 1,
            Name = "Host"
        };

        var generalRole = new Role
        {
            Id = 2,
            Name = "GeneralUser"
        };

        var hostUser = new User
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

        var disabledHostUser = new User
        {
            Id = 2,
            Name = "無効ホスト",
            Kana = "ムコウホスト",
            Email = "disabled-host@example.com",
            PasswordHash = "hash",
            PostalCode = "100-0002",
            Address = "東京都中央区",
            PhoneNumber = "090-0000-0002",
            UsageType = "Host",
            Enabled = false
        };

        var generalUser = new User
        {
            Id = 3,
            Name = "一般ユーザー",
            Kana = "イッパンユーザー",
            Email = "general@example.com",
            PasswordHash = "hash",
            PostalCode = "100-0003",
            Address = "東京都港区",
            PhoneNumber = "090-0000-0003",
            UsageType = "General",
            Enabled = true
        };

        context.Roles.AddRange(hostRole, generalRole);
        context.Users.AddRange(hostUser, disabledHostUser, generalUser);

        context.UserRoles.AddRange(
            new UserRole
            {
                Id = 1,
                UserId = hostUser.Id,
                RoleId = hostRole.Id,
                User = hostUser,
                Role = hostRole
            },
            new UserRole
            {
                Id = 2,
                UserId = disabledHostUser.Id,
                RoleId = hostRole.Id,
                User = disabledHostUser,
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

        context.Rooms.AddRange(
            new Room
            {
                Id = 1,
                UserId = hostUser.Id,
                User = hostUser,
                Name = "A Studio",
                ImageName = "room01.jpg",
                Description = "Aスタジオ説明",
                Price = 3000,
                Capacity = 5,
                PostalCode = "107-0062",
                Address = "東京都港区南青山",
                CreatedAtUtc = new DateTime(2026, 5, 1),
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            },
            new Room
            {
                Id = 2,
                UserId = hostUser.Id,
                User = hostUser,
                Name = "B Studio",
                ImageName = "room02.jpg",
                Description = "Bスタジオ説明",
                Price = 4000,
                Capacity = 8,
                PostalCode = "171-0021",
                Address = "東京都豊島区西池袋",
                CreatedAtUtc = new DateTime(2026, 5, 1),
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            }
        );

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetListAsync_条件なしの場合_Id昇順で一覧を返す()
    {
        await using var context = CreateDbContext();
        await SeedHostAndRoomsAsync(context);

        var service = new AdminRoomService(context);

        var result = await service.GetListAsync(
            keyword: null,
            page: 1,
            pageSize: 10);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Items[0].Id);
        Assert.Equal(2, result.Items[1].Id);
        Assert.Equal("A Studio", result.Items[0].Name);
        Assert.Equal("B Studio", result.Items[1].Name);
    }

    [Fact]
    public async Task GetListAsync_keyword指定の場合_部屋名住所郵便番号ホスト名で部分一致検索する()
    {
        await using var context = CreateDbContext();
        await SeedHostAndRoomsAsync(context);

        var service = new AdminRoomService(context);

        var result = await service.GetListAsync(
            keyword: "池袋",
            page: 1,
            pageSize: 10);

        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal("B Studio", result.Items[0].Name);
    }

    [Fact]
    public async Task GetListAsync_pageとpageSizeが0以下の場合_既定値に丸める()
    {
        await using var context = CreateDbContext();
        await SeedHostAndRoomsAsync(context);

        var service = new AdminRoomService(context);

        var result = await service.GetListAsync(
            keyword: null,
            page: 0,
            pageSize: 0);

        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetDetailAsync_存在するRoomIdの場合_詳細DTOを返す()
    {
        await using var context = CreateDbContext();
        await SeedHostAndRoomsAsync(context);

        var service = new AdminRoomService(context);

        var result = await service.GetDetailAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
        Assert.Equal("A Studio", result.Name);
        Assert.Equal("ホスト太郎", result.HostName);
    }

    [Fact]
    public async Task GetDetailAsync_存在しないRoomIdの場合_nullを返す()
    {
        await using var context = CreateDbContext();
        await SeedHostAndRoomsAsync(context);

        var service = new AdminRoomService(context);

        var result = await service.GetDetailAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetHostOptionsAsync_有効なHostロールユーザーのみ返す()
    {
        await using var context = CreateDbContext();
        await SeedHostAndRoomsAsync(context);

        var service = new AdminRoomService(context);

        var result = await service.GetHostOptionsAsync();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("ホスト太郎", result[0].Name);
        Assert.Equal("host@example.com", result[0].Email);
    }

    [Fact]
    public async Task CreateAsync_有効なHostの場合_部屋を作成する()
    {
        await using var context = CreateDbContext();
        await SeedHostAndRoomsAsync(context);

        var service = new AdminRoomService(context);

        var request = new AdminRoomUpsertRequestDto
        {
            UserId = 1,
            Name = " C Studio ",
            ImageName = " room03.jpg ",
            Description = " 新規スタジオ ",
            Price = 5000,
            Capacity = 10,
            PostalCode = "150-0001",
            Address = " 東京都渋谷区 "
        };

        var result = await service.CreateAsync(request);

        Assert.NotEqual(0, result.Id);
        Assert.Equal("C Studio", result.Name);
        Assert.Equal("東京都渋谷区", result.Address);
        Assert.Equal("ホスト太郎", result.HostName);

        var created = await context.Rooms.FirstOrDefaultAsync(x => x.Id == result.Id);
        Assert.NotNull(created);
        Assert.Equal("C Studio", created!.Name);
    }

    [Fact]
    public async Task CreateAsync_HostでないUserIdの場合_HOST_NOT_FOUNDを投げる()
    {
        await using var context = CreateDbContext();
        await SeedHostAndRoomsAsync(context);

        var service = new AdminRoomService(context);

        var request = new AdminRoomUpsertRequestDto
        {
            UserId = 3,
            Name = "C Studio",
            ImageName = "room03.jpg",
            Description = "新規スタジオ",
            Price = 5000,
            Capacity = 10,
            PostalCode = "150-0001",
            Address = "東京都渋谷区"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(request));

        Assert.Equal("HOST_NOT_FOUND", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_同一の部屋名と住所が存在する場合_ROOM_DUPLICATEDを投げる()
    {
        await using var context = CreateDbContext();
        await SeedHostAndRoomsAsync(context);

        var service = new AdminRoomService(context);

        var request = new AdminRoomUpsertRequestDto
        {
            UserId = 1,
            Name = "A Studio",
            ImageName = "room99.jpg",
            Description = "重複テスト",
            Price = 5000,
            Capacity = 10,
            PostalCode = "150-0001",
            Address = "東京都港区南青山"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(request));

        Assert.Equal("ROOM_DUPLICATED", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_存在するRoomIdの場合_部屋を更新する()
    {
        await using var context = CreateDbContext();
        await SeedHostAndRoomsAsync(context);

        var service = new AdminRoomService(context);

        var request = new AdminRoomUpsertRequestDto
        {
            UserId = 1,
            Name = "A Studio Updated",
            ImageName = "room-updated.jpg",
            Description = "更新後説明",
            Price = 6000,
            Capacity = 12,
            PostalCode = "160-0022",
            Address = "東京都新宿区"
        };

        var result = await service.UpdateAsync(1, request);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
        Assert.Equal("A Studio Updated", result.Name);
        Assert.Equal("東京都新宿区", result.Address);
        Assert.Equal(6000, result.Price);
    }

    [Fact]
    public async Task UpdateAsync_存在しないRoomIdの場合_nullを返す()
    {
        await using var context = CreateDbContext();
        await SeedHostAndRoomsAsync(context);

        var service = new AdminRoomService(context);

        var request = new AdminRoomUpsertRequestDto
        {
            UserId = 1,
            Name = "Not Found Room",
            ImageName = "room404.jpg",
            Description = "存在しない",
            Price = 6000,
            Capacity = 12,
            PostalCode = "160-0022",
            Address = "東京都新宿区"
        };

        var result = await service.UpdateAsync(999, request);

        Assert.Null(result);
    }
}