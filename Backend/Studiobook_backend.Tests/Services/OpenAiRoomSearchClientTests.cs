using System.Net;
using Microsoft.Extensions.Options;
using Studiobook_backend.Dtos.Ai;
using Studiobook_backend.Services;
using Studiobook_backend.Settings;
using System.Text.Json;

namespace Studiobook_backend.Tests.Services;

public class OpenAiRoomSearchClientTests
{
    [Fact]
    public async Task AnalyzeQueryAsync_ReturnsConditions_WhenResponseHasOutputText()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "output_text": "{\"keyword\":\"ダンス\",\"area\":\"新宿区\",\"price\":5000,\"capacity\":5,\"purpose\":\"ダンス練習\",\"atmosphere\":\"明るい\",\"timePreference\":\"夜\",\"capacityCondition\":\"min\",\"keywords\":[\"ダンス\",\"新宿\",\"夜\"]}"
                }
                """)
            });

        var client = CreateClient(handler);

        // Act
        var result = await client.AnalyzeQueryAsync("新宿で5人くらいで夜に使える明るいダンススタジオ");

        // Assert
        Assert.Equal("ダンス", result.Keyword);
        Assert.Equal("新宿区", result.Area);
        Assert.Equal(5000, result.Price);
        Assert.Equal(5, result.Capacity);
        Assert.Equal("ダンス練習", result.Purpose);
        Assert.Equal("明るい", result.Atmosphere);
        Assert.Equal("夜", result.TimePreference);
        Assert.Equal("min", result.CapacityCondition);

        Assert.Equal(3, result.Keywords.Count);
        Assert.Contains("ダンス", result.Keywords);
        Assert.Contains("新宿", result.Keywords);
        Assert.Contains("夜", result.Keywords);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal("https://api.openai.com/v1/responses", handler.LastRequest.RequestUri!.ToString());

        Assert.NotNull(handler.LastRequest.Headers.Authorization);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("test-api-key", handler.LastRequest.Headers.Authorization.Parameter);

        Assert.NotNull(handler.LastRequestBody);

        Assert.Equal("gpt-test", GetModel(handler.LastRequestBody));

        var userContent = GetInputContent(handler.LastRequestBody, "user");
        Assert.Contains("新宿で5人くらいで夜に使える明るいダンススタジオ", userContent);

        var root = GetRoot(handler.LastRequestBody);
        var format = root
            .GetProperty("text")
            .GetProperty("format");

        Assert.Equal("json_schema", format.GetProperty("type").GetString());
        Assert.Equal("room_search_conditions", format.GetProperty("name").GetString());
    }

    [Fact]
    public async Task AnalyzeQueryAsync_ReturnsConditions_WhenResponseHasOutputArray()
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
                          "text": "{\"keyword\":\"撮影\",\"area\":\"渋谷区\",\"price\":null,\"capacity\":2,\"purpose\":\"撮影\",\"atmosphere\":\"おしゃれ\",\"timePreference\":null,\"capacityCondition\":\"min\",\"keywords\":[\"撮影\",\"渋谷\",\"おしゃれ\"]}"
                        }
                      ]
                    }
                  ]
                }
                """)
            });

        var client = CreateClient(handler);

        // Act
        var result = await client.AnalyzeQueryAsync("渋谷で2人で撮影できるおしゃれなスタジオ");

        // Assert
        Assert.Equal("撮影", result.Keyword);
        Assert.Equal("渋谷区", result.Area);
        Assert.Null(result.Price);
        Assert.Equal(2, result.Capacity);
        Assert.Equal("撮影", result.Purpose);
        Assert.Equal("おしゃれ", result.Atmosphere);
        Assert.Null(result.TimePreference);
        Assert.Equal("min", result.CapacityCondition);

        Assert.Equal(3, result.Keywords.Count);
        Assert.Contains("撮影", result.Keywords);
        Assert.Contains("渋谷", result.Keywords);
        Assert.Contains("おしゃれ", result.Keywords);
    }

    [Fact]
    public async Task AnalyzeQueryAsync_ReturnsEmptyConditions_WhenOutputTextIsMissing()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "id": "resp_test"
                }
                """)
            });

        var client = CreateClient(handler);

        // Act
        var result = await client.AnalyzeQueryAsync("条件なし");

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Keyword);
        Assert.Null(result.Area);
        Assert.Null(result.Price);
        Assert.Null(result.Capacity);
        Assert.Null(result.Purpose);
        Assert.Null(result.Atmosphere);
        Assert.Null(result.TimePreference);
        Assert.Null(result.CapacityCondition);
        Assert.Empty(result.Keywords);
    }

    [Fact]
    public async Task AnalyzeQueryAsync_ThrowsInvalidOperationException_WhenApiKeyIsEmpty()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"output_text":"{}"}""")
            });

        var httpClient = new HttpClient(handler);

        var client = new OpenAiRoomSearchClient(
            httpClient,
            Options.Create(new OpenAiSettings
            {
                ApiKey = "",
                Model = "gpt-test"
            }));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.AnalyzeQueryAsync("新宿のスタジオ"));

        Assert.Equal("OpenAI APIキーが設定されていません。", ex.Message);
    }

    [Fact]
    public async Task AnalyzeQueryAsync_ThrowsInvalidOperationException_WhenApiReturnsError()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("""
                {
                  "error": {
                    "message": "bad request"
                  }
                }
                """)
            });

        var client = CreateClient(handler);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.AnalyzeQueryAsync("新宿のスタジオ"));

        Assert.Contains("OpenAI API呼び出しに失敗しました。Status: 400", ex.Message);
        Assert.Contains("bad request", ex.Message);
    }

    [Fact]
    public async Task AnalyzeQueryAsync_ThrowsJsonException_WhenOutputTextIsInvalidJson()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "output_text": "これはJSONではありません"
                }
                """)
            });
;
        var client = CreateClient(handler);

        // Act & Assert
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() =>
            client.AnalyzeQueryAsync("新宿のスタジオ"));
    }

    [Fact]
    public async Task AnalyzeQueryAsync_SendsExpectedRequestBody()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "output_text": "{\"keyword\":null,\"area\":null,\"price\":null,\"capacity\":null,\"purpose\":null,\"atmosphere\":null,\"timePreference\":null,\"capacityCondition\":null,\"keywords\":[]}"
                }
                """)
            });

        var client = CreateClient(handler);

        // Act
        await client.AnalyzeQueryAsync("安めで少人数向けのスタジオ");

        // Assert
        Assert.NotNull(handler.LastRequestBody);


        Assert.Equal("gpt-test", GetModel(handler.LastRequestBody));

        var systemContent = GetInputContent(handler.LastRequestBody, "system");
        var userContent = GetInputContent(handler.LastRequestBody, "user");

        Assert.Contains("スタジオ予約サービスの検索条件解析AI", systemContent);
        Assert.Contains("安めで少人数向けのスタジオ", userContent);

        var root = GetRoot(handler.LastRequestBody);

        var format = root
            .GetProperty("text")
            .GetProperty("format");

        Assert.Equal("json_schema", format.GetProperty("type").GetString());
        Assert.Equal("room_search_conditions", format.GetProperty("name").GetString());

        var properties = format
            .GetProperty("schema")
            .GetProperty("properties");

        Assert.True(properties.TryGetProperty("capacityCondition", out _));
        Assert.True(properties.TryGetProperty("keywords", out _));
    }

    private static OpenAiRoomSearchClient CreateClient(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);

        return new OpenAiRoomSearchClient(
            httpClient,
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

    private static JsonElement GetRoot(string requestBody)
    {
        using var document = JsonDocument.Parse(requestBody);
        return document.RootElement.Clone();
    }
}