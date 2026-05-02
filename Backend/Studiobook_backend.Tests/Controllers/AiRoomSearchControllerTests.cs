using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Studiobook_backend.Controllers;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Ai;
using Studiobook_backend.Services;
using Studiobook_backend.Settings;

namespace Studiobook_backend.Tests.Controllers;

public class AiRoomSearchControllerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AiSearchLogService CreateLogService(AppDbContext context)
    {
        return new AiSearchLogService(
            context,
            Options.Create(new OpenAiSettings
            {
                Model = "gpt-test"
            }));
    }

    [Fact]
    public async Task Search_Queryが空の場合_BadRequestを返し失敗ログを保存する()
    {
        await using var context = CreateDbContext();

        var searchService = new AiRoomSearchService(
            context: null!,
            openAiClient: null!);

        var logService = CreateLogService(context);

        var controller = new AiRoomSearchController(
            searchService,
            logService);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        controller.HttpContext.Connection.RemoteIpAddress =
            System.Net.IPAddress.Parse("127.0.0.1");

        var request = new AiRoomSearchRequest
        {
            Query = "   "
        };

        var result = await controller.Search(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequest.Value);

        var log = await context.AiSearchLogs.SingleAsync();

        Assert.Equal("", log.Query);
        Assert.Equal("127.0.0.1", log.IpAddress);
        Assert.Equal("gpt-test", log.Model);
        Assert.False(log.Succeeded);
        Assert.Equal(0, log.ResultCount);
        Assert.Equal("検索文を入力してください。", log.ErrorMessage);
    }

    [Fact]
    public async Task Search_Queryが200文字を超える場合_BadRequestを返し失敗ログを保存する()
    {
        await using var context = CreateDbContext();

        var searchService = new AiRoomSearchService(
            context: null!,
            openAiClient: null!);

        var logService = CreateLogService(context);

        var controller = new AiRoomSearchController(
            searchService,
            logService);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var request = new AiRoomSearchRequest
        {
            Query = new string('あ', 201)
        };

        var result = await controller.Search(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequest.Value);

        var log = await context.AiSearchLogs.SingleAsync();

        Assert.False(log.Succeeded);
        Assert.Equal(0, log.ResultCount);
        Assert.Equal("検索文は200文字以内で入力してください。", log.ErrorMessage);
    }
}
