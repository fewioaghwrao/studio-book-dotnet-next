using Studiobook_backend.Dtos.Ai;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Services;

public class AiReviewAssistServiceTests
{
    [Fact]
    public async Task AssistAsync_Contentが空の場合_ArgumentExceptionを投げる()
    {
        // Arrange
        var service = new AiReviewAssistService(openAiClient: null!);

        var request = new AiReviewAssistRequest
        {
            Content = "   ",
            Score = 4,
            RoomName = "A Studio"
        };

        // Act
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.AssistAsync(request));

        // Assert
        Assert.Equal("レビュー本文を入力してください。", ex.Message);
    }

    [Fact]
    public async Task AssistAsync_Contentが1000文字を超える場合_ArgumentExceptionを投げる()
    {
        // Arrange
        var service = new AiReviewAssistService(openAiClient: null!);

        var request = new AiReviewAssistRequest
        {
            Content = new string('あ', 1001),
            Score = 4,
            RoomName = "A Studio"
        };

        // Act
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.AssistAsync(request));

        // Assert
        Assert.Equal("レビュー本文は1000文字以内で入力してください。", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task AssistAsync_Scoreが1から5以外の場合_ArgumentExceptionを投げる(int score)
    {
        // Arrange
        var service = new AiReviewAssistService(openAiClient: null!);

        var request = new AiReviewAssistRequest
        {
            Content = "良いスタジオでした。",
            Score = score,
            RoomName = "A Studio"
        };

        // Act
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.AssistAsync(request));

        // Assert
        Assert.Equal("評価は1〜5で指定してください。", ex.Message);
    }
}