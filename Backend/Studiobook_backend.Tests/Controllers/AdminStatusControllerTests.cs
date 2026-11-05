using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Controllers;
using Studiobook_backend.Infrastructure.Persistence;
using Studiobook_backend.Application.Dtos.Admin;
using Studiobook_backend.Application.Services;
using Studiobook_backend.Infrastructure.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Controllers;

public class AdminStatusControllerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Get_yearMonth指定ありの場合_Okを返す()
    {
        // Arrange
        await using var context = CreateDbContext();

        var service = new AdminStatusService(context);
        var controller = new AdminStatusController(service);

        // Act
        var result = await controller.Get(
            roomId: null,
            year: 2026,
            month: 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AdminStatusResponseDto>(okResult.Value);

        Assert.Equal(new[] { "2026-03", "2026-04", "2026-05" }, body.Labels);
    }

    [Fact]
    public async Task Get_年月未指定の場合_日本時間の今月を基準にする()
    {
        // Arrange
        await using var context = CreateDbContext();

        var service = new AdminStatusService(context);

        // UTC 10/31 15:30 ＝ 日本時間 11/1 0:30
        var controller = new AdminStatusController(
            service,
            FixedTimeProvider.AtUtc(2026, 10, 31, 15, 30));

        // Act
        var result = await controller.Get(
            roomId: null,
            year: null,
            month: null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AdminStatusResponseDto>(okResult.Value);

        Assert.Equal(new[] { "2026-09", "2026-10", "2026-11" }, body.Labels);
    }

    [Fact]
    public async Task Get_roomIdが0の場合_null扱いでOkを返す()
    {
        // Arrange
        await using var context = CreateDbContext();

        var service = new AdminStatusService(context);
        var controller = new AdminStatusController(service);

        // Act
        var result = await controller.Get(
            roomId: 0,
            year: 2026,
            month: 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AdminStatusResponseDto>(okResult.Value);

        Assert.Equal(new[] { "2026-03", "2026-04", "2026-05" }, body.Labels);
    }
}