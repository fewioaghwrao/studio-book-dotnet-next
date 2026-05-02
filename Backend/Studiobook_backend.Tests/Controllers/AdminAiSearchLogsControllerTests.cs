using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Controllers;

public class AdminAiSearchLogsControllerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetList_正常時_Okを返す()
    {
        // Arrange
        await using var context = CreateDbContext();

        context.AiSearchLogs.Add(new AiSearchLog
        {
            Id = 1,
            CreatedAtUtc = new DateTime(2026, 5, 1),
            Query = "test query",
            Succeeded = true,
            ResultCount = 1
        });

        await context.SaveChangesAsync();

        var service = new AdminAiSearchLogService(context);
        var controller = new AdminAiSearchLogsController(service);

        // Act
        var result = await controller.GetList(
            q: null,
            userId: null,
            ipAddress: null,
            succeeded: null,
            model: null,
            from: null,
            to: null,
            page: 1,
            pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }
}