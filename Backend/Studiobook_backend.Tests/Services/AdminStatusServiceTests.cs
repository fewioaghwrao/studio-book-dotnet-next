using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Services;

public class AdminStatusServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task SeedBasicDataAsync(AppDbContext context)
    {
        var hostRole = new Role
        {
            Id = 1,
            Name = "Host"
        };

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
            Name = "予約花子",
            Kana = "ヨヤクハナコ",
            Email = "guest@example.com",
            PasswordHash = "hash",
            PostalCode = "100-0002",
            Address = "東京都中央区",
            PhoneNumber = "090-0000-0002",
            UsageType = "General",
            Enabled = true
        };

        context.Roles.Add(hostRole);
        context.Users.AddRange(host, guest);

        context.UserRoles.Add(new UserRole
        {
            Id = 1,
            UserId = host.Id,
            RoleId = hostRole.Id,
            User = host,
            Role = hostRole
        });

        var room = new Room
        {
            Id = 1,
            UserId = host.Id,
            User = host,
            Name = "A Studio",
            ImageName = "room01.jpg",
            Description = "テスト用スタジオ",
            Price = 3000,
            Capacity = 5,
            PostalCode = "100-0003",
            Address = "東京都港区",
            CreatedAtUtc = new DateTime(2026, 1, 1),
            UpdatedAtUtc = new DateTime(2026, 1, 1)
        };

        context.Rooms.Add(room);

        var reservationBooked = new Reservation
        {
            Id = 1,
            RoomId = room.Id,
            Room = room,
            UserId = guest.Id,
            User = guest,
            StartAt = new DateTime(2026, 3, 10, 10, 0, 0),
            EndAt = new DateTime(2026, 3, 10, 12, 0, 0),
            Amount = 6000,
            Status = "booked",
            CreatedAtUtc = new DateTime(2026, 3, 1),
            UpdatedAtUtc = new DateTime(2026, 3, 1)
        };

        var reservationPaid = new Reservation
        {
            Id = 2,
            RoomId = room.Id,
            Room = room,
            UserId = guest.Id,
            User = guest,
            StartAt = new DateTime(2026, 4, 10, 10, 0, 0),
            EndAt = new DateTime(2026, 4, 10, 12, 0, 0),
            Amount = 8000,
            Status = "paid",
            CreatedAtUtc = new DateTime(2026, 4, 1),
            UpdatedAtUtc = new DateTime(2026, 4, 1)
        };

        context.Reservations.AddRange(reservationBooked, reservationPaid);

        context.ReservationChargeItems.AddRange(
            new ReservationChargeItem
            {
                Id = 1,
                ReservationId = reservationBooked.Id,
                Reservation = reservationBooked,
                Kind = "platform_fee",
                Description = "管理手数料",
                SliceAmount = 600,
                SliceStart = reservationBooked.StartAt,
                SliceEnd = reservationBooked.EndAt,
                UnitRatePerHour = 300
            },
            new ReservationChargeItem
            {
                Id = 2,
                ReservationId = reservationPaid.Id,
                Reservation = reservationPaid,
                Kind = "platform_fee",
                Description = "管理手数料",
                SliceAmount = 800,
                SliceStart = reservationPaid.StartAt,
                SliceEnd = reservationPaid.EndAt,
                UnitRatePerHour = 400
            }
        );

        context.Reviews.AddRange(
            new Review
            {
                Id = 1,
                RoomId = room.Id,
                Room = room,
                UserId = guest.Id,
                User = guest,
                Score = 4,
                Content = "良かったです",
                PublicVisible = true,
                CreatedAtUtc = new DateTime(2026, 4, 1),
                UpdatedAtUtc = new DateTime(2026, 4, 1)
            },
            new Review
            {
                Id = 2,
                RoomId = room.Id,
                Room = room,
                UserId = guest.Id,
                User = guest,
                Score = 2,
                Content = "普通でした",
                PublicVisible = false,
                CreatedAtUtc = new DateTime(2026, 4, 2),
                UpdatedAtUtc = new DateTime(2026, 4, 2)
            }
        );

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAsync_存在しないRoomIdの場合_空集計を返す()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedBasicDataAsync(context);

        var service = new AdminStatusService(context);

        // Act
        var result = await service.GetAsync(
            roomId: 999,
            baseMonth: new DateTime(2026, 5, 1));

        // Assert
        Assert.Equal(new[] { "2026-03", "2026-04", "2026-05" }, result.Labels);
        Assert.Equal(new[] { 0, 0, 0 }, result.Booked);
        Assert.Equal(new[] { 0, 0, 0 }, result.Paid);

        Assert.Equal(3, result.UtilizationPercents.Count);
        Assert.All(result.UtilizationPercents, Assert.Null);

        Assert.Null(result.ReviewAvgAny);
        Assert.Null(result.ReviewAvgPublic);

        Assert.Equal(1, result.TotalRoomCount);
        Assert.Equal(1, result.TotalHostCount);
        Assert.Equal(2, result.TotalReservationCount);
    }

    [Fact]
    public async Task GetAsync_基準月指定の場合_直近3か月のラベルを返す()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedBasicDataAsync(context);

        var service = new AdminStatusService(context);

        // Act
        var result = await service.GetAsync(
            roomId: null,
            baseMonth: new DateTime(2026, 5, 1));

        // Assert
        Assert.Equal(new[] { "2026-03", "2026-04", "2026-05" }, result.Labels);
    }

    [Fact]
    public async Task GetAsync_platformFeeをbookedとpaidに月別集計する()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedBasicDataAsync(context);

        var service = new AdminStatusService(context);

        // Act
        var result = await service.GetAsync(
            roomId: null,
            baseMonth: new DateTime(2026, 5, 1));

        // Assert
        Assert.Equal(new[] { "2026-03", "2026-04", "2026-05" }, result.Labels);

        Assert.Equal(600, result.Booked[0]);
        Assert.Equal(0, result.Booked[1]);
        Assert.Equal(0, result.Booked[2]);

        Assert.Equal(0, result.Paid[0]);
        Assert.Equal(800, result.Paid[1]);
        Assert.Equal(0, result.Paid[2]);

        Assert.Equal(800, result.TotalPaidAmount);
    }

    [Fact]
    public async Task GetAsync_レビュー平均を返す()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedBasicDataAsync(context);

        var service = new AdminStatusService(context);

        // Act
        var result = await service.GetAsync(
            roomId: null,
            baseMonth: new DateTime(2026, 5, 1));

        // Assert
        Assert.Equal(3.0, result.ReviewAvgAny);
        Assert.Equal(4.0, result.ReviewAvgPublic);
    }
}
