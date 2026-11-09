using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Studiobook_backend.Application.Common;
using Studiobook_backend.Application.Dtos.Ai;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Application.Settings;

namespace Studiobook_backend.Infrastructure.Services
{
    public class OpenAiRoomSearchClient : IOpenAiRoomSearchClient
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

        public async Task<AiRoomSearchConditionsDto> AnalyzeQueryAsync(string query, DateTime? nowJapan = null)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                throw new InvalidOperationException("OpenAI APIキーが設定されていません。");
            }

            var now = nowJapan ?? JapanTime.Now(TimeProvider.System);
            var today = now.ToString("yyyy-MM-dd（ddd） HH:mm", CultureInfo.GetCultureInfo("ja-JP"));

            var requestBody = new
            {
                model = _settings.Model,
                input = new object[]
                {
new
{
    role = "system",
    content = $$"""
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
    日時・曜日・時間帯を表す語（「夜」「土曜」「来週」など）は keywords に入れないでください。

    利用日時は次のルールで設定してください。現在の日本時間は {{today}} です。
    - date は利用日を yyyy-MM-dd で設定してください。「明日」「今週末」「来週土曜」などは現在日時から解決してください。日付が分からない場合は null
    - startTime は開始時刻を HH:mm（24時間表記）で設定してください。「18時から」「午後3時」のように明示された場合のみ。「夜」「午後」だけの場合は null
    - endTime は終了時刻が明示された場合のみ HH:mm で設定してください
    - durationMinutes は利用時間を分で設定してください（「2時間」なら 120）。不明な場合は null
    - timePreference には「夜」「午前中」などの曖昧な時間帯の希望をそのまま入れてください
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
                                date = new
                                {
                                    type = new[] { "string", "null" }
                                },
                                startTime = new
                                {
                                    type = new[] { "string", "null" }
                                },
                                endTime = new
                                {
                                    type = new[] { "string", "null" }
                                },
                                durationMinutes = new
                                {
                                    type = new[] { "integer", "null" }
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
                                "date",
                                "startTime",
                                "endTime",
                                "durationMinutes",
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