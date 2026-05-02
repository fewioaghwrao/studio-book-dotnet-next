using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Studiobook_backend.Data;
using Studiobook_backend.Services;
using Studiobook_backend.Settings;

namespace Studiobook_backend.Tests.Services;

public class AiSearchLogServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AiSearchLogService CreateService(
        AppDbContext context,
        string? model = "gpt-test")
    {
        var options = Options.Create(new OpenAiSettings
        {
            Model = model
        });

        return new AiSearchLogService(context, options);
    }

    [Fact]
    public async Task WriteAsync_正常時_AiSearchLogを保存する()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);

        await service.WriteAsync(
            query: "落ち着いたスタジオ",
            ipAddress: "127.0.0.1",
            userId: 1,
            succeeded: true,
            resultCount: 3);

        var log = await context.AiSearchLogs.SingleAsync();

        Assert.Equal("落ち着いたスタジオ", log.Query);
        Assert.Equal("127.0.0.1", log.IpAddress);
        Assert.Equal(1, log.UserId);
        Assert.Equal("gpt-test", log.Model);
        Assert.True(log.Succeeded);
        Assert.Equal(3, log.ResultCount);
        Assert.Null(log.ErrorMessage);
    }

    [Fact]
    public async Task WriteAsync_長い値の場合_最大文字数で切り詰める()
    {
        await using var context = CreateDbContext();
        var service = CreateService(
            context,
            model: new string('m', 101));

        await service.WriteAsync(
            query: new string('q', 501),
            ipAddress: new string('1', 101),
            userId: null,
            succeeded: false,
            resultCount: 0,
            errorMessage: new string('e', 1001));

        var log = await context.AiSearchLogs.SingleAsync();

        Assert.Equal(500, log.Query.Length);
        Assert.Equal(100, log.IpAddress!.Length);
        Assert.Equal(100, log.Model!.Length);
        Assert.Equal(1000, log.ErrorMessage!.Length);
    }

    [Fact]
    public async Task WriteAsync_空白値の場合_IpAddressModelErrorMessageはnullになる()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context, model: "   ");

        await service.WriteAsync(
            query: "  test query  ",
            ipAddress: "   ",
            userId: null,
            succeeded: false,
            resultCount: 0,
            errorMessage: "   ");

        var log = await context.AiSearchLogs.SingleAsync();

        Assert.Equal("test query", log.Query);
        Assert.Null(log.IpAddress);
        Assert.Null(log.Model);
        Assert.Null(log.ErrorMessage);
    }
}
