using System.Net;
using Microsoft.Extensions.Options;
using Studiobook_backend.Services;
using Studiobook_backend.Settings;
using System.Text.Json;

namespace Studiobook_backend.Tests.Services;

public class OpenAiReviewAssistClientTests
{
    [Fact]
    public async Task AssistAsync_ReturnsOutputText_WhenResponseHasOutputText()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "output_text": "とても使いやすいスタジオでした。"
                }
                """)
            });

        var httpClient = new HttpClient(handler);

        var client = new OpenAiReviewAssistClient(
            httpClient,
            Options.Create(new OpenAiSettings
            {
                ApiKey = "test-api-key",
                Model = "gpt-test"
            }));

        // Act
        var result = await client.AssistAsync(
            content: "よかった",
            score: 5,
            roomName: "新宿スタジオ");

        // Assert
        Assert.Equal("とても使いやすいスタジオでした。", result);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://api.openai.com/v1/responses", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("test-api-key", handler.LastRequest.Headers.Authorization.Parameter);

        Assert.NotNull(handler.LastRequestBody);

        Assert.Equal("gpt-test", GetModel(handler.LastRequestBody));

        var userContent = GetInputContent(handler.LastRequestBody, "user");

        Assert.Contains("新宿スタジオ", userContent);
        Assert.Contains("よかった", userContent);
        Assert.Contains("評価: 5 / 5", userContent);
    }

    [Fact]
    public async Task AssistAsync_ThrowsInvalidOperationException_WhenApiKeyIsEmpty()
    {
        // Arrange
        var client = new OpenAiReviewAssistClient(
            new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))),
            Options.Create(new OpenAiSettings
            {
                ApiKey = "",
                Model = "gpt-test"
            }));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.AssistAsync("よかった", 5, "新宿スタジオ"));
    }

    [Fact]
    public async Task AssistAsync_ThrowsArgumentException_WhenContentIsEmpty()
    {
        // Arrange
        var client = CreateClientWithSuccessResponse("unused");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            client.AssistAsync("   ", 5, "新宿スタジオ"));

        Assert.Equal("レビュー本文を入力してください。", ex.Message);
    }

    [Fact]
    public async Task AssistAsync_ThrowsArgumentException_WhenContentIsTooLong()
    {
        // Arrange
        var client = CreateClientWithSuccessResponse("unused");
        var content = new string('あ', 1001);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            client.AssistAsync(content, 5, "新宿スタジオ"));

        Assert.Equal("レビュー本文は1000文字以内で入力してください。", ex.Message);
    }

    [Fact]
    public async Task AssistAsync_ThrowsArgumentException_WhenScoreIsInvalid()
    {
        // Arrange
        var client = CreateClientWithSuccessResponse("unused");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            client.AssistAsync("よかった", 6, "新宿スタジオ"));

        Assert.Equal("評価は1〜5で指定してください。", ex.Message);
    }

    [Fact]
    public async Task AssistAsync_ThrowsInvalidOperationException_WhenApiReturnsError()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("""{"error":{"message":"bad request"}}""")
            });

        var client = new OpenAiReviewAssistClient(
            new HttpClient(handler),
            Options.Create(new OpenAiSettings
            {
                ApiKey = "test-api-key",
                Model = "gpt-test"
            }));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.AssistAsync("よかった", 5, "新宿スタジオ"));

        Assert.Contains("OpenAI API呼び出しに失敗しました。Status: 400", ex.Message);
    }

    [Fact]
    public async Task AssistAsync_ReturnsText_WhenResponseHasOutputArray()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "output": [
                    {
                      "content": [
                        {
                          "text": "自然で読みやすいレビューです。"
                        }
                      ]
                    }
                  ]
                }
                """)
            });

        var client = new OpenAiReviewAssistClient(
            new HttpClient(handler),
            Options.Create(new OpenAiSettings
            {
                ApiKey = "test-api-key",
                Model = "gpt-test"
            }));

        // Act
        var result = await client.AssistAsync("良い", 5, null);

        // Assert
        Assert.Equal("自然で読みやすいレビューです。", result);
    }

    [Fact]
    public async Task AssistAsync_TruncatesResult_WhenAiTextIsLongerThan1000()
    {
        // Arrange
        var longText = new string('あ', 1001);
        var client = CreateClientWithSuccessResponse(longText);

        // Act
        var result = await client.AssistAsync("良い", 5, null);

        // Assert
        Assert.Equal(1000, result.Length);
    }

    private static OpenAiReviewAssistClient CreateClientWithSuccessResponse(string outputText)
    {
        var escaped = System.Text.Json.JsonSerializer.Serialize(outputText);

        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""
                {
                  "output_text": {{escaped}}
                }
                """)
            });

        return new OpenAiReviewAssistClient(
            new HttpClient(handler),
            Options.Create(new OpenAiSettings
            {
                ApiKey = "test-api-key",
                Model = "gpt-test"
            }));
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return _handler(request);
        }
    }
    private static string GetInputContent(string requestBody, string role)
    {
        using var document = JsonDocument.Parse(requestBody);

        var input = document.RootElement.GetProperty("input");

        foreach (var item in input.EnumerateArray())
        {
            if (item.GetProperty("role").GetString() == role)
            {
                return item.GetProperty("content").GetString() ?? "";
            }
        }

        return "";
    }

    private static string GetModel(string requestBody)
    {
        using var document = JsonDocument.Parse(requestBody);
        return document.RootElement.GetProperty("model").GetString() ?? "";
    }
}