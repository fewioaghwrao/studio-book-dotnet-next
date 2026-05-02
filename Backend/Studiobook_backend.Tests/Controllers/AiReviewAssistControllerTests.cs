using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Controllers;
using Studiobook_backend.Dtos.Ai;
using Studiobook_backend.Services;

namespace Studiobook_backend.Tests.Controllers;

public class AiReviewAssistControllerTests
{
    [Fact]
    public async Task Assist_Contentが空の場合_BadRequestを返す()
    {
        // Arrange
        var service = new AiReviewAssistService(openAiClient: null!);
        var controller = new AiReviewAssistController(service);

        var request = new AiReviewAssistRequest
        {
            Content = "   ",
            Score = 4,
            RoomName = "A Studio"
        };

        // Act
        var result = await controller.Assist(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task Assist_Contentが1000文字を超える場合_BadRequestを返す()
    {
        // Arrange
        var service = new AiReviewAssistService(openAiClient: null!);
        var controller = new AiReviewAssistController(service);

        var request = new AiReviewAssistRequest
        {
            Content = new string('あ', 1001),
            Score = 4,
            RoomName = "A Studio"
        };

        // Act
        var result = await controller.Assist(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequest.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Assist_Scoreが1から5以外の場合_BadRequestを返す(int score)
    {
        // Arrange
        var service = new AiReviewAssistService(openAiClient: null!);
        var controller = new AiReviewAssistController(service);

        var request = new AiReviewAssistRequest
        {
            Content = "良いスタジオでした。",
            Score = score,
            RoomName = "A Studio"
        };

        // Act
        var result = await controller.Assist(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequest.Value);
    }
}