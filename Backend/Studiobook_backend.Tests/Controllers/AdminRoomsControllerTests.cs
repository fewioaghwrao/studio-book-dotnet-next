using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Controllers;

public class AdminRoomsControllerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetDetail_存在しないRoomIdの場合_NotFoundを返す()
    {
        await using var context = CreateDbContext();

        var service = new AdminRoomService(context);
        var controller = new AdminRoomsController(
            service,
            auditLogService: null!);

        var result = await controller.GetDetail(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_ModelStateが不正な場合_BadRequestを返す()
    {
        await using var context = CreateDbContext();

        var service = new AdminRoomService(context);
        var controller = new AdminRoomsController(
            service,
            auditLogService: null!);

        controller.ModelState.AddModelError("Name", "スタジオ名は必須です。");

        var request = new Studiobook_backend.Dtos.Admin.AdminRoomUpsertRequestDto();

        var result = await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}