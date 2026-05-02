using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Services;

public class HomeServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
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

        var guest = new User
        {
            Id = 2,
            Name = "会員花子",
            Kana = "カイインハナコ",
            Email = "guest@example.com",
            PasswordHash = "hash",
            PostalCode = "100-0002",
            Address = "東京都中央区",
            PhoneNumber = "090-0000-0002",
            UsageType = "General",
            Enabled = true
        };

        context.Users.AddRange(host, guest);

        var rooms = new[]
        {
            new Room
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
            },
            new Room
            {
                Id = 2,
                UserId = host.Id,
                User = host,
                Name = "B Studio",
                ImageName = "room02.jpg",
                Description = "B",
                Price = 4000,
                Capacity = 6,
                PostalCode = "100-0002",
                Address = "東京都新宿区",
                CreatedAtUtc = new DateTime(2026, 5, 1),
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            },
            new Room
            {
                Id = 3,
                UserId = host.Id,
                User = host,
                Name = "C Studio",
                ImageName = "room03.jpg",
                Description = "C",
                Price = 5000,
                Capacity = 8,
                PostalCode = "100-0003",
                Address = "東京都池袋",
                CreatedAtUtc = new DateTime(2026, 5, 1),
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            },
            new Room
            {
                Id = 4,
                UserId = host.Id,
                User = host,
                Name = "D Studio",
                ImageName = "room04.jpg",
                Description = "D",
                Price = 6000,
                Capacity = 10,
                PostalCode = "100-0004",
                Address = "東京都上野",
                CreatedAtUtc = new DateTime(2026, 5, 1),
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            },
            new Room
            {
                Id = 5,
                UserId = host.Id,
                User = host,
                Name = "E Studio",
                ImageName = "room05.jpg",
                Description = "E",
                Price = 7000,
                Capacity = 12,
                PostalCode = "100-0005",
                Address = "東京都品川",
                CreatedAtUtc = new DateTime(2026, 5, 1),
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            }
        };

        context.Rooms.AddRange(rooms);

        context.Reviews.AddRange(
            // Room 1: 公開2件 平均4.5
            new Review
            {
                Id = 1,
                RoomId = 1,
                Room = rooms[0],
                UserId = guest.Id,
                User = guest,
                Score = 5,
                Content = "良い",
                PublicVisible = true,
                CreatedAtUtc = new DateTime(2026, 5, 1),
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            },
            new Review
            {
                Id = 2,
                RoomId = 1,
                Room = rooms[0],
                UserId = guest.Id,
                User = guest,
                Score = 4,
                Content = "まあ良い",
                PublicVisible = true,
                CreatedAtUtc = new DateTime(2026, 5, 2),
                UpdatedAtUtc = new DateTime(2026, 5, 2)
            },

            // Room 2: 公開3件 平均3.0 → レビュー件数でRoom1より上
            new Review
            {
                Id = 3,
                RoomId = 2,
                Room = rooms[1],
                UserId = guest.Id,
                User = guest,
                Score = 3,
                Content = "普通",
                PublicVisible = true,
                CreatedAtUtc = new DateTime(2026, 5, 3),
                UpdatedAtUtc = new DateTime(2026, 5, 3)
            },
            new Review
            {
                Id = 4,
                RoomId = 2,
                Room = rooms[1],
                UserId = guest.Id,
                User = guest,
                Score = 3,
                Content = "普通",
                PublicVisible = true,
                CreatedAtUtc = new DateTime(2026, 5, 4),
                UpdatedAtUtc = new DateTime(2026, 5, 4)
            },
            new Review
            {
                Id = 5,
                RoomId = 2,
                Room = rooms[1],
                UserId = guest.Id,
                User = guest,
                Score = 3,
                Content = "普通",
                PublicVisible = true,
                CreatedAtUtc = new DateTime(2026, 5, 5),
                UpdatedAtUtc = new DateTime(2026, 5, 5)
            },

            // Room 3: 公開2件 平均5.0 → Room1と件数同じなので平均点でRoom3が上
            new Review
            {
                Id = 6,
                RoomId = 3,
                Room = rooms[2],
                UserId = guest.Id,
                User = guest,
                Score = 5,
                Content = "最高",
                PublicVisible = true,
                CreatedAtUtc = new DateTime(2026, 5, 6),
                UpdatedAtUtc = new DateTime(2026, 5, 6)
            },
            new Review
            {
                Id = 7,
                RoomId = 3,
                Room = rooms[2],
                UserId = guest.Id,
                User = guest,
                Score = 5,
                Content = "最高",
                PublicVisible = true,
                CreatedAtUtc = new DateTime(2026, 5, 7),
                UpdatedAtUtc = new DateTime(2026, 5, 7)
            },

            // Room 4: 非公開レビューのみ → 件数0、平均null扱い
            new Review
            {
                Id = 8,
                RoomId = 4,
                Room = rooms[3],
                UserId = guest.Id,
                User = guest,
                Score = 5,
                Content = "非公開",
                PublicVisible = false,
                CreatedAtUtc = new DateTime(2026, 5, 8),
                UpdatedAtUtc = new DateTime(2026, 5, 8)
            }
        );

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetHomeAsync_PopularRoomsはレビュー件数降順_平均点降順_Id昇順で最大3件返す()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new HomeService(context);

        // Act
        var result = await service.GetHomeAsync();

        // Assert
        Assert.Equal(3, result.PopularRooms.Count);

        Assert.Equal(2, result.PopularRooms[0].Id); // レビュー3件
        Assert.Equal(3, result.PopularRooms[1].Id); // レビュー2件 平均5.0
        Assert.Equal(1, result.PopularRooms[2].Id); // レビュー2件 平均4.5
    }

    [Fact]
    public async Task GetHomeAsync_NewRoomsはId降順で最大4件返す()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new HomeService(context);

        // Act
        var result = await service.GetHomeAsync();

        // Assert
        Assert.Equal(4, result.NewRooms.Count);

        Assert.Equal(5, result.NewRooms[0].Id);
        Assert.Equal(4, result.NewRooms[1].Id);
        Assert.Equal(3, result.NewRooms[2].Id);
        Assert.Equal(2, result.NewRooms[3].Id);
    }

    [Fact]
    public async Task GetHomeAsync_非公開レビューは平均点と件数に含めない()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new HomeService(context);

        // Act
        var result = await service.GetHomeAsync();

        // Assert
        var room4 = result.NewRooms.Single(x => x.Id == 4);

        Assert.Equal(0, room4.ReviewCount);
        Assert.Null(room4.AverageScore);
    }
}
