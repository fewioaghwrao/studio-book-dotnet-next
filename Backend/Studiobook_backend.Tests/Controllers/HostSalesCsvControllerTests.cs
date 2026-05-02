using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Controllers;

public class HostSalesCsvControllerTests
{
    [Fact]
    public async Task ExportSalesCsv_ReturnsCsvFile_WithOnlyLoginHostSales()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedHostUserAsync(context, userId: 99, name: "ホストB");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "ホストAのスタジオ");
        await SeedRoomAsync(context, roomId: 2, hostUserId: 99, name: "ホストBのスタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "paid",
            amount: 6000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 2,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "paid",
            amount: 7000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.ExportSalesCsv(
            roomId: null,
            onlyWithItems: true);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);

        Assert.Equal("text/csv; charset=utf-8", fileResult.ContentType);
        Assert.StartsWith("host-sales-", fileResult.FileDownloadName);
        Assert.EndsWith(".csv", fileResult.FileDownloadName);

        var csv = Encoding.UTF8.GetString(fileResult.FileContents);

        Assert.StartsWith('\uFEFF'.ToString(), csv);

        Assert.Contains("\"予約ID\",\"スタジオ名\",\"予約者\",\"予約開始日時\",\"予約終了日時\",\"総額(円)\",\"状態\"", csv);
        Assert.Contains("\"1\",\"ホストAのスタジオ\",\"ゲストA\",\"2026-05-01 10:00\",\"2026-05-01 12:00\",\"6000\",\"利用済み\"", csv);

        Assert.DoesNotContain("ホストBのスタジオ", csv);
        Assert.DoesNotContain("\"2\",", csv);
    }

    [Fact]
    public async Task ExportSalesCsv_FiltersByRoomId()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");
        await SeedRoomAsync(context, roomId: 2, hostUserId: 10, name: "渋谷スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "paid",
            amount: 6000);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 2,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "booked",
            amount: 7000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.ExportSalesCsv(
            roomId: 2,
            onlyWithItems: true);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        var csv = Encoding.UTF8.GetString(fileResult.FileContents);

        Assert.Contains("渋谷スタジオ", csv);
        Assert.Contains("\"2\",\"渋谷スタジオ\",\"ゲストA\",\"2026-05-02 10:00\",\"2026-05-02 12:00\",\"7000\",\"予約済み\"", csv);

        Assert.DoesNotContain("新宿スタジオ", csv);
    }

    [Fact]
    public async Task ExportSalesCsv_ExcludesZeroAmount_WhenOnlyWithItemsIsTrue()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "booked",
            amount: 0);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "paid",
            amount: 6000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.ExportSalesCsv(
            roomId: null,
            onlyWithItems: true);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        var csv = Encoding.UTF8.GetString(fileResult.FileContents);

        Assert.Contains("\"2\",\"新宿スタジオ\"", csv);
        Assert.DoesNotContain("\"1\",\"新宿スタジオ\"", csv);
        Assert.DoesNotContain("\"0\",\"予約済み\"", csv);
    }

    [Fact]
    public async Task ExportSalesCsv_IncludesZeroAmount_WhenOnlyWithItemsIsFalse()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "booked",
            amount: 0);

        await SeedReservationAsync(
            context,
            reservationId: 2,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 2, 10, 0, 0),
            endAt: new DateTime(2026, 5, 2, 12, 0, 0),
            status: "paid",
            amount: 6000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.ExportSalesCsv(
            roomId: null,
            onlyWithItems: false);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        var csv = Encoding.UTF8.GetString(fileResult.FileContents);

        Assert.Contains("\"1\",\"新宿スタジオ\",\"ゲストA\",\"2026-05-01 10:00\",\"2026-05-01 12:00\",\"0\",\"予約済み\"", csv);
        Assert.Contains("\"2\",\"新宿スタジオ\",\"ゲストA\",\"2026-05-02 10:00\",\"2026-05-02 12:00\",\"6000\",\"利用済み\"", csv);
    }

    [Fact]
    public async Task ExportSalesCsv_EscapesDoubleQuotesAndCommas()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲスト, \"A\"");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿, \"特別\" スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 10, 0, 0),
            endAt: new DateTime(2026, 5, 1, 12, 0, 0),
            status: "canceled",
            amount: 6000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.ExportSalesCsv(
            roomId: null,
            onlyWithItems: true);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        var csv = Encoding.UTF8.GetString(fileResult.FileContents);

        Assert.Contains("\"新宿, \"\"特別\"\" スタジオ\"", csv);
        Assert.Contains("\"ゲスト, \"\"A\"\"\"", csv);
        Assert.Contains("\"キャンセル済み\"", csv);
    }

    [Fact]
    public async Task ExportSalesCsv_ThrowsUnauthorizedAccessException_WhenUserIdClaimIsMissing()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var controller = new HostSalesCsvController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            controller.ExportSalesCsv(
                roomId: null,
                onlyWithItems: true));
    }

    private static HostSalesCsvController CreateController(
        AppDbContext context,
        int userId)
    {
        var controller = new HostSalesCsvController(context);

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

    private static async Task SeedHostUserAsync(
        AppDbContext context,
        int userId,
        string name)
    {
        context.Users.Add(new User
        {
            Id = userId,
            Name = name,
            Kana = "テストホスト",
            Email = $"host{userId}@example.com",
            PasswordHash = "dummy_hash",
            PostalCode = "100-0001",
            Address = "東京都千代田区テスト",
            PhoneNumber = "090-0000-0000",
            UsageType = "host",
            Enabled = true
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedGuestUserAsync(
        AppDbContext context,
        int userId,
        string name)
    {
        context.Users.Add(new User
        {
            Id = userId,
            Name = name,
            Kana = "テストゲスト",
            Email = $"guest{userId}@example.com",
            PasswordHash = "dummy_hash",
            PostalCode = "100-0001",
            Address = "東京都千代田区テスト",
            PhoneNumber = "090-0000-0000",
            UsageType = "general",
            Enabled = true
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedRoomAsync(
        AppDbContext context,
        int roomId,
        int hostUserId,
        string name)
    {
        context.Rooms.Add(new Room
        {
            Id = roomId,
            UserId = hostUserId,
            Name = name,
            ImageName = $"room{roomId:00}.jpg",
            Description = "テスト用スタジオ",
            Price = 3000,
            Capacity = 5,
            PostalCode = "100-0001",
            Address = $"東京都テスト区サンプル{roomId}",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedReservationAsync(
        AppDbContext context,
        int reservationId,
        int roomId,
        int guestUserId,
        DateTime startAt,
        DateTime endAt,
        string status,
        int amount)
    {
        context.Reservations.Add(new Reservation
        {
            Id = reservationId,
            RoomId = roomId,
            UserId = guestUserId,
            StartAt = startAt,
            EndAt = endAt,
            Status = status,
            Amount = amount,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }
}