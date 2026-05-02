using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Dtos.Admin;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Controllers;

public class AdminReservationsControllerTests
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

        var room = new Room
        {
            Id = 1,
            UserId = host.Id,
            User = host,
            Name = "青山フォトスタジオ",
            ImageName = "room01.jpg",
            Description = "テスト用スタジオ",
            Price = 3000,
            Capacity = 5,
            PostalCode = "107-0062",
            Address = "東京都港区南青山",
            CreatedAtUtc = new DateTime(2026, 5, 1),
            UpdatedAtUtc = new DateTime(2026, 5, 1)
        };

        context.Users.AddRange(host, guest);
        context.Rooms.Add(room);
        context.Reservations.Add(new Reservation
        {
            Id = 1,
            RoomId = room.Id,
            Room = room,
            UserId = guest.Id,
            User = guest,
            StartAt = new DateTime(2026, 5, 10, 9, 0, 0),
            EndAt = new DateTime(2026, 5, 10, 12, 0, 0),
            Amount = 9000,
            Status = "booked",
            CreatedAtUtc = new DateTime(2026, 5, 1),
            UpdatedAtUtc = new DateTime(2026, 5, 1)
        });

        await context.SaveChangesAsync();

        var service = new AdminReservationService(context);
        var controller = new AdminReservationsController(service);

        // Act
        var result = await controller.GetList(
            keyword: null,
            status: null,
            reservationId: null,
            roomId: null,
            startFrom: null,
            startTo: null,
            page: 1,
            pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AdminReservationListResponseDto>(okResult.Value);

        Assert.Single(body.Items);
        Assert.Equal(1, body.TotalCount);
        Assert.Equal("青山フォトスタジオ", body.Items[0].RoomName);
    }
}