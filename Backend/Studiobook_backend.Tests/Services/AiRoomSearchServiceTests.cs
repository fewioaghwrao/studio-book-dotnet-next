using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Services;

public class AiRoomSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_Queryが空の場合_ArgumentExceptionを投げる()
    {
        var service = new AiRoomSearchService(
            context: null!,
            openAiClient: null!);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SearchAsync("   "));

        Assert.Equal("検索文を入力してください。", ex.Message);
    }

    [Fact]
    public async Task SearchAsync_Queryが200文字を超える場合_ArgumentExceptionを投げる()
    {
        var service = new AiRoomSearchService(
            context: null!,
            openAiClient: null!);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SearchAsync(new string('あ', 201)));

        Assert.Equal("検索文は200文字以内で入力してください。", ex.Message);
    }
}