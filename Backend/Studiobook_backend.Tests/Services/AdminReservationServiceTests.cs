using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Services;

public class AdminReservationServiceTests
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

        var room1 = new Room
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

        var room2 = new Room
        {
            Id = 2,
            UserId = host.Id,
            User = host,
            Name = "池袋ダンスルーム",
            ImageName = "room02.jpg",
            Description = "テスト用ルーム",
            Price = 2500,
            Capacity = 8,
            PostalCode = "171-0021",
            Address = "東京都豊島区西池袋",
            CreatedAtUtc = new DateTime(2026, 5, 1),
            UpdatedAtUtc = new DateTime(2026, 5, 1)
        };

        context.Users.AddRange(host, guest);
        context.Rooms.AddRange(room1, room2);

        context.Reservations.AddRange(
            new Reservation
            {
                Id = 1,
                RoomId = room1.Id,
                Room = room1,
                UserId = guest.Id,
                User = guest,
                StartAt = new DateTime(2026, 5, 10, 9, 0, 0),
                EndAt = new DateTime(2026, 5, 10, 12, 0, 0),
                Amount = 9000,
                Status = "booked",
                CreatedAtUtc = new DateTime(2026, 5, 1),
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            },
            new Reservation
            {
                Id = 2,
                RoomId = room2.Id,
                Room = room2,
                UserId = guest.Id,
                User = guest,
                StartAt = new DateTime(2026, 5, 11, 9, 0, 0),
                EndAt = new DateTime(2026, 5, 11, 13, 0, 0),
                Amount = 10000,
                Status = "paid",
                CreatedAtUtc = new DateTime(2026, 5, 1),
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            }
        );

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetListAsync_条件なしの場合_StartAt降順で予約一覧を返す()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new AdminReservationService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: null,
            status: null,
            reservationId: null,
            roomId: null,
            startFrom: null,
            startTo: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);

        Assert.Equal(2, result.Items[0].ReservationId);
        Assert.Equal("池袋ダンスルーム", result.Items[0].RoomName);

        Assert.Equal(1, result.Items[1].ReservationId);
        Assert.Equal("青山フォトスタジオ", result.Items[1].RoomName);
    }

    [Fact]
    public async Task GetListAsync_keyword指定の場合_部屋名ホスト名ゲスト名で部分一致検索する()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new AdminReservationService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: "青山",
            status: null,
            reservationId: null,
            roomId: null,
            startFrom: null,
            startTo: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].ReservationId);
        Assert.Equal("青山フォトスタジオ", result.Items[0].RoomName);
    }

    [Fact]
    public async Task GetListAsync_status指定の場合_Statusで完全一致検索する()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new AdminReservationService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: null,
            status: "paid",
            reservationId: null,
            roomId: null,
            startFrom: null,
            startTo: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].ReservationId);
        Assert.Equal("paid", result.Items[0].Status);
    }

    [Fact]
    public async Task GetListAsync_startFromとstartTo指定の場合_指定日範囲の予約のみ返す()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new AdminReservationService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: null,
            status: null,
            reservationId: null,
            roomId: null,
            startFrom: new DateTime(2026, 5, 10),
            startTo: new DateTime(2026, 5, 10),
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].ReservationId);
        Assert.Equal(new DateTime(2026, 5, 10, 9, 0, 0), result.Items[0].StartAt);
    }

    [Fact]
    public async Task GetListAsync_pageSizeが100を超える場合_100に丸める()
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
            Name = "大量テスト用スタジオ",
            ImageName = "room01.jpg",
            Description = "テスト用",
            Price = 3000,
            Capacity = 5,
            PostalCode = "107-0062",
            Address = "東京都港区",
            CreatedAtUtc = new DateTime(2026, 5, 1),
            UpdatedAtUtc = new DateTime(2026, 5, 1)
        };

        context.Users.AddRange(host, guest);
        context.Rooms.Add(room);

        for (var i = 1; i <= 120; i++)
        {
            context.Reservations.Add(new Reservation
            {
                Id = i,
                RoomId = room.Id,
                Room = room,
                UserId = guest.Id,
                User = guest,
                StartAt = new DateTime(2026, 5, 1).AddHours(i),
                EndAt = new DateTime(2026, 5, 1).AddHours(i + 1),
                Amount = 3000,
                Status = "booked",
                CreatedAtUtc = new DateTime(2026, 5, 1),
                UpdatedAtUtc = new DateTime(2026, 5, 1)
            });
        }

        await context.SaveChangesAsync();

        var service = new AdminReservationService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: null,
            status: null,
            reservationId: null,
            roomId: null,
            startFrom: null,
            startTo: null,
            page: 1,
            pageSize: 999);

        // Assert
        Assert.Equal(120, result.TotalCount);
        Assert.Equal(100, result.PageSize);
        Assert.Equal(100, result.Items.Count);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetListAsync_RoomOptionsは部屋名昇順で返す()
    {
        // Arrange
        await using var context = CreateDbContext();
        await SeedAsync(context);

        var service = new AdminReservationService(context);

        // Act
        var result = await service.GetListAsync(
            keyword: null,
            status: null,
            reservationId: null,
            roomId: null,
            startFrom: null,
            startTo: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.Equal(2, result.RoomOptions.Count);
        Assert.Equal("青山フォトスタジオ", result.RoomOptions[0].Name);
        Assert.Equal("池袋ダンスルーム", result.RoomOptions[1].Name);
    }
}