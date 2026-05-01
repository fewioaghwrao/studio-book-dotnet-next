using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Studiobook_backend.Dtos.Ai;
using Studiobook_backend.Settings;

namespace Studiobook_backend.Services
{
    public class OpenAiRoomSearchClient
    {
        private readonly HttpClient _httpClient;
        private readonly OpenAiSettings _settings;

        public OpenAiRoomSearchClient(
            HttpClient httpClient,
            IOptions<OpenAiSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<AiRoomSearchConditionsDto> AnalyzeQueryAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                throw new InvalidOperationException("OpenAI APIキーが設定されていません。");
            }

            var requestBody = new
            {
                model = _settings.Model,
                input = new object[]
                {
new
{
    role = "system",
    content = """
    あなたはスタジオ予約サービスの検索条件解析AIです。
    ユーザーの自然文から、スタジオ検索に使える条件をJSONで抽出してください。
    不明な項目は null にしてください。

    area は東京23区名が含まれる場合のみ設定してください。
    price は「5000円以内」「安め」などがある場合のみ推定してください。

    capacity は人数に関する数値を設定してください。
    capacityCondition は次のルールで設定してください。
    - 「5人で使える」「5人くらい」「5人入れる」「5人以上」「2人のスタジオ」は min
    - 「5人以下」「少人数向け」「小さめ」「コンパクト」は max
    - 「5人用」「5人ぴったり」は exact
    - 人数条件が不明な場合は null

    keywords には検索に使える短い日本語キーワードを最大5個入れてください。
    """
},
                    new
                    {
                        role = "user",
                        content = query
                    }
                },
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "room_search_conditions",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            additionalProperties = false,
                            properties = new
                            {
                                keyword = new
                                {
                                    type = new[] { "string", "null" }
                                },
                                area = new
                                {
                                    type = new[] { "string", "null" }
                                },
                                price = new
                                {
                                    type = new[] { "integer", "null" }
                                },
                                capacity = new
                                {
                                    type = new[] { "integer", "null" }
                                },
                                purpose = new
                                {
                                    type = new[] { "string", "null" }
                                },
                                atmosphere = new
                                {
                                    type = new[] { "string", "null" }
                                },
                                timePreference = new
                                {
                                    type = new[] { "string", "null" }
                                },
                                capacityCondition = new
                                {
                                    type = new[] { "string", "null" },
                                    @enum = new object[] { "min", "max", "exact", null }
                                },
                                keywords = new
                                {
                                    type = "array",
                                    items = new
                                    {
                                        type = "string"
                                    }
                                }
                            },
                            required = new[]
                            {
                                "keyword",
                                "area",
                                "price",
                                "capacity",
                                "purpose",
                                "atmosphere",
                                "timePreference",
                                "capacityCondition",
                                "keywords"
                            }
                        }
                    }
                }
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

            var jsonText = ExtractOutputText(responseText);

            var conditions = JsonSerializer.Deserialize<AiRoomSearchConditionsDto>(
                jsonText,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return conditions ?? new AiRoomSearchConditionsDto();
        }

        private static string ExtractOutputText(string responseText)
        {
            using var document = JsonDocument.Parse(responseText);
            var root = document.RootElement;

            if (root.TryGetProperty("output_text", out var outputTextElement))
            {
                return outputTextElement.GetString() ?? "{}";
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
                            return textElement.GetString() ?? "{}";
                        }
                    }
                }
            }

            return "{}";
        }
    }
}