using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Admin;
using Studiobook_backend.Services;

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