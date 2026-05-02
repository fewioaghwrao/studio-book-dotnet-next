using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Dtos.Admin;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Controllers;

public class AdminAuditLogsControllerTests
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

        context.AuditLogs.Add(new AuditLog
        {
            Id = 1,
            Ts = new DateTime(2026, 5, 1),
            ActorId = 1,
            Action = "LOGIN",
            Entity = "User",
            EntityId = 1,
            Note = "ログインしました"
        });

        await context.SaveChangesAsync();

        var service = new AdminAuditLogService(context);
        var controller = new AdminAuditLogsController(service);

        // Act
        var result = await controller.GetList(
            q: null,
            actorId: null,
            action: null,
            entity: null,
            entityId: null,
            from: null,
            to: null,
            page: 1,
            pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AdminAuditLogListResponseDto>(okResult.Value);

        Assert.Single(body.Items);
        Assert.Equal(1, body.TotalCount);
        Assert.Equal(1, body.Items[0].Id);
    }
}