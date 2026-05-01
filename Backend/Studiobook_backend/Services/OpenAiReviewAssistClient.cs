using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Studiobook_backend.Settings;

namespace Studiobook_backend.Services;

public class OpenAiReviewAssistClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiSettings _settings;

    public OpenAiReviewAssistClient(
        HttpClient httpClient,
        IOptions<OpenAiSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task<string> AssistAsync(
        string content,
        int score,
        string? roomName)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException("OpenAI APIキーが設定されていません。");
        }

        var normalizedContent = content.Trim();

        if (string.IsNullOrWhiteSpace(normalizedContent))
        {
            throw new ArgumentException("レビュー本文を入力してください。");
        }

        if (normalizedContent.Length > 1000)
        {
            throw new ArgumentException("レビュー本文は1000文字以内で入力してください。");
        }

        if (score < 1 || score > 5)
        {
            throw new ArgumentException("評価は1〜5で指定してください。");
        }

        var roomNameText = string.IsNullOrWhiteSpace(roomName)
            ? "指定なし"
            : roomName.Trim();

        var requestBody = new
        {
            model = _settings.Model,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = """
                    あなたはスタジオ予約サービスのレビュー文補助AIです。
                    ユーザーが入力したレビュー文を、自然で読みやすい日本語に整えてください。

                    ルール:
                    - ユーザーの感想の意味を変えないでください。
                    - 事実として書かれていない内容を追加しないでください。
                    - 評価点と矛盾する過度な表現にしないでください。
                    - 誹謗中傷、個人情報、断定的すぎる表現はやわらげてください。
                    - 予約サービスのレビューとして自然な敬体にしてください。
                    - 出力はレビュー本文のみ返してください。
                    - 前置き、説明、引用符、箇条書きは不要です。
                    - 400文字以内にしてください。
                    """
                },
                new
                {
                    role = "user",
                    content = $"""
                    スタジオ名: {roomNameText}
                    評価: {score} / 5
                    元のレビュー:
                    {normalizedContent}
                    """
                }
            },
            max_output_tokens = 500
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/responses");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request);

        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI API呼び出しに失敗しました。Status: {(int)response.StatusCode}, Body: {responseText}");
        }

        var assistedContent = ExtractOutputText(responseText).Trim();

        if (string.IsNullOrWhiteSpace(assistedContent))
        {
            throw new InvalidOperationException("AIレビュー文補助の結果が空でした。");
        }

        if (assistedContent.Length > 1000)
        {
            assistedContent = assistedContent[..1000];
        }

        return assistedContent;
    }

    private static string ExtractOutputText(string responseText)
    {
        using var document = JsonDocument.Parse(responseText);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var outputTextElement))
        {
            return outputTextElement.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("output", out var outputElement))
        {
            foreach (var outputItem in outputElement.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out var contentElement))
                {
                    continue;
                }

                foreach (var contentItem in contentElement.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("text", out var textElement))
                    {
                        return textElement.GetString() ?? string.Empty;
                    }
                }
            }
        }

        return string.Empty;
    }
}