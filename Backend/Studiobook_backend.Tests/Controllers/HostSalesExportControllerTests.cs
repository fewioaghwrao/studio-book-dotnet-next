using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Tests.Helpers;
using QuestPDF.Infrastructure;

namespace Studiobook_backend.Tests.Controllers;

public class HostSalesExportControllerTests
{
    [Fact]
    public async Task ExportItemsCsv_ReturnsCsvFile_WhenReservationBelongsToHost()
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
            startAt: new DateTime(2026, 5, 1, 9, 0, 0),
            endAt: new DateTime(2026, 5, 1, 20, 0, 0),
            status: "paid",
            amount: 36300);

        await SeedChargeItemAsync(
            context,
            id: 1,
            reservationId: 1,
            kind: "base",
            description: "通常料金",
            sliceStart: new DateTime(2026, 5, 1, 9, 0, 0),
            sliceEnd: new DateTime(2026, 5, 1, 19, 0, 0),
            unitRatePerHour: 3000,
            sliceAmount: 30000);

        await SeedChargeItemAsync(
            context,
            id: 2,
            reservationId: 1,
            kind: "multiplier",
            description: "夜間料金",
            sliceStart: new DateTime(2026, 5, 1, 19, 0, 0),
            sliceEnd: new DateTime(2026, 5, 1, 20, 0, 0),
            unitRatePerHour: 4500,
            sliceAmount: 4500);

        await SeedChargeItemAsync(
            context,
            id: 3,
            reservationId: 1,
            kind: "tax",
            description: "消費税",
            sliceStart: null,
            sliceEnd: null,
            unitRatePerHour: null,
            sliceAmount: 1800);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.ExportItemsCsv(reservationId: 1);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);

        Assert.Equal("text/csv; charset=utf-8", fileResult.ContentType);
        Assert.Equal("reservation-1-items.csv", fileResult.FileDownloadName);

        var csv = Encoding.UTF8.GetString(fileResult.FileContents);

        Assert.StartsWith('\uFEFF'.ToString(), csv);

        Assert.Contains("# 予約ID,\"1\"", csv);
        Assert.Contains("# スタジオ名,\"新宿スタジオ\"", csv);
        Assert.Contains("# 予約者,\"ゲストA\"", csv);
        Assert.Contains("# 期間,\"2026/05/01 09:00 〜 2026/05/01 20:00\"", csv);
        Assert.Contains("# 総額(円),\"36300\"", csv);

        Assert.Contains("\"区分\",\"明細内容\",\"開始\",\"終了\",\"1時間当たりの値段\",\"金額(円)\"", csv);

        Assert.Contains("\"基本料金\",\"通常料金\",\"2026/05/01 09:00\",\"2026/05/01 19:00\",\"3000\",\"30000\"", csv);
        Assert.Contains("\"加算料金\",\"夜間料金\",\"2026/05/01 19:00\",\"2026/05/01 20:00\",\"4500\",\"4500\"", csv);
        Assert.Contains("\"消費税\",\"消費税\",\"\",\"\",\"\",\"1800\"", csv);
    }

    [Fact]
    public async Task ExportItemsCsv_ReturnsNotFound_WhenReservationDoesNotBelongToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedHostUserAsync(context, userId: 99, name: "ホストB");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 99, name: "他ホストのスタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 9, 0, 0),
            endAt: new DateTime(2026, 5, 1, 20, 0, 0),
            status: "paid",
            amount: 10000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.ExportItemsCsv(reservationId: 1);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task ExportItemsCsv_ReturnsNotFound_WhenReservationDoesNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.ExportItemsCsv(reservationId: 999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task ExportItemsCsv_ThrowsUnauthorizedAccessException_WhenUserIdClaimIsMissing()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var controller = new HostSalesExportController(context)
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
            controller.ExportItemsCsv(reservationId: 1));
    }

    [Fact]
    public async Task ExportInvoicePdf_ReturnsPdfFile_WhenReservationBelongsToHost()
    {
        // Arrange
        QuestPDF.Settings.License = LicenseType.Community;

        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");
        await SeedRoomAsync(context, roomId: 1, hostUserId: 10, name: "新宿スタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 9, 0, 0),
            endAt: new DateTime(2026, 5, 1, 20, 0, 0),
            status: "paid",
            amount: 36300);

        await SeedChargeItemAsync(
            context,
            id: 1,
            reservationId: 1,
            kind: "base",
            description: "通常料金",
            sliceStart: new DateTime(2026, 5, 1, 9, 0, 0),
            sliceEnd: new DateTime(2026, 5, 1, 19, 0, 0),
            unitRatePerHour: 3000,
            sliceAmount: 30000);

        await SeedChargeItemAsync(
            context,
            id: 2,
            reservationId: 1,
            kind: "tax",
            description: "消費税",
            sliceStart: null,
            sliceEnd: null,
            unitRatePerHour: null,
            sliceAmount: 6300);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.ExportInvoicePdf(reservationId: 1);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);

        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal("reservation-1-invoice.pdf", fileResult.FileDownloadName);
        Assert.NotEmpty(fileResult.FileContents);

        var header = Encoding.ASCII.GetString(fileResult.FileContents.Take(4).ToArray());
        Assert.Equal("%PDF", header);
    }

    [Fact]
    public async Task ExportInvoicePdf_ReturnsNotFound_WhenReservationDoesNotBelongToHost()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedHostUserAsync(context, userId: 10, name: "ホストA");
        await SeedHostUserAsync(context, userId: 99, name: "ホストB");
        await SeedGuestUserAsync(context, userId: 20, name: "ゲストA");

        await SeedRoomAsync(context, roomId: 1, hostUserId: 99, name: "他ホストのスタジオ");

        await SeedReservationAsync(
            context,
            reservationId: 1,
            roomId: 1,
            guestUserId: 20,
            startAt: new DateTime(2026, 5, 1, 9, 0, 0),
            endAt: new DateTime(2026, 5, 1, 20, 0, 0),
            status: "paid",
            amount: 10000);

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.ExportInvoicePdf(reservationId: 1);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task ExportInvoicePdf_ReturnsNotFound_WhenReservationDoesNotExist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var controller = CreateController(context, userId: 10);

        // Act
        var result = await controller.ExportInvoicePdf(reservationId: 999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task ExportInvoicePdf_ThrowsUnauthorizedAccessException_WhenUserIdClaimIsMissing()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var controller = new HostSalesExportController(context)
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
            controller.ExportInvoicePdf(reservationId: 1));
    }

    private static HostSalesExportController CreateController(
        AppDbContext context,
        int userId)
    {
        var controller = new HostSalesExportController(context);

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

    private static async Task SeedChargeItemAsync(
        AppDbContext context,
        int id,
        int reservationId,
        string kind,
        string? description,
        DateTime? sliceStart,
        DateTime? sliceEnd,
        int? unitRatePerHour,
        int sliceAmount)
    {
        context.ReservationChargeItems.Add(new ReservationChargeItem
        {
            Id = id,
            ReservationId = reservationId,
            Kind = kind,
            Description = description,
            SliceStart = sliceStart,
            SliceEnd = sliceEnd,
            UnitRatePerHour = unitRatePerHour,
            SliceAmount = sliceAmount
        });

        await context.SaveChangesAsync();
    }
}