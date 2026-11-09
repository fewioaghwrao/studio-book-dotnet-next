using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Studiobook_backend.ErrorHandling;

namespace Studiobook_backend.Tests.ErrorHandling;

/// <summary>
/// Program.cs と同じ順序（例外ハンドラー → CORS）のパイプラインで、
/// 処理されなかった例外の応答に CORS ヘッダーと JSON の本文が付くことを確認する。
/// </summary>
public class GlobalExceptionHandlerTests
{
    private const string FrontendOrigin = "https://studio-book-dotnet-next.vercel.app";

    private static async Task<WebApplication> StartAppAsync(string environmentName = "Production")
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName
        });

        builder.WebHost.UseTestServer();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("FrontendPolicy", policy =>
            {
                policy.WithOrigins(FrontendOrigin)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        builder.Services.AddApiExceptionHandling();

        var app = builder.Build();

        app.UseApiExceptionHandling();
        app.UseCors("FrontendPolicy");

        app.MapGet("/boom", string () => throw new InvalidOperationException("内部の詳細メッセージ"));
        app.MapGet("/no-user", string () => throw new UnauthorizedAccessException("ログインユーザーを取得できません。"));
        app.MapGet("/ok", () => "ok");

        await app.StartAsync();
        return app;
    }

    private static HttpRequestMessage CreateRequest(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Origin", FrontendOrigin);
        return request;
    }

    [Fact]
    public async Task 処理されなかった例外は500のJSONで返し_CORSヘッダーを付ける()
    {
        await using var app = await StartAppAsync();
        var client = app.GetTestClient();

        var response = await client.SendAsync(CreateRequest("/boom"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        // これがないとブラウザは応答を捨て、画面に「通信エラー」と表示される
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Equal(FrontendOrigin, Assert.Single(origins!));

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(GlobalExceptionHandler.ServerErrorMessage, json.RootElement.GetProperty("message").GetString());
        Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("traceId").GetString()));

        // 本番では例外の内容を返さない
        Assert.False(json.RootElement.TryGetProperty("detail", out _));
        Assert.DoesNotContain("内部の詳細メッセージ", json.RootElement.GetRawText());
    }

    [Fact]
    public async Task ログインユーザーを特定できない例外は401で返す()
    {
        await using var app = await StartAppAsync();
        var client = app.GetTestClient();

        var response = await client.SendAsync(CreateRequest("/no-user"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(GlobalExceptionHandler.UnauthorizedMessage, json.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task 開発環境では例外の内容をdetailに含める()
    {
        await using var app = await StartAppAsync(Environments.Development);
        var client = app.GetTestClient();

        var response = await client.SendAsync(CreateRequest("/boom"));

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("内部の詳細メッセージ", json.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task 正常な応答は変わらない()
    {
        await using var app = await StartAppAsync();
        var client = app.GetTestClient();

        var response = await client.SendAsync(CreateRequest("/ok"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", await response.Content.ReadAsStringAsync());
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
