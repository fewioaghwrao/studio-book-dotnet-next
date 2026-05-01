using Studiobook_backend.Dtos.Ai;

namespace Studiobook_backend.Services;

public class AiReviewAssistService
{
    private readonly OpenAiReviewAssistClient _openAiClient;

    public AiReviewAssistService(OpenAiReviewAssistClient openAiClient)
    {
        _openAiClient = openAiClient;
    }

    public async Task<AiReviewAssistResponse> AssistAsync(
        AiReviewAssistRequest request)
    {
        var normalizedContent = request.Content.Trim();

        if (string.IsNullOrWhiteSpace(normalizedContent))
        {
            throw new ArgumentException("レビュー本文を入力してください。");
        }

        if (normalizedContent.Length > 1000)
        {
            throw new ArgumentException("レビュー本文は1000文字以内で入力してください。");
        }

        if (request.Score < 1 || request.Score > 5)
        {
            throw new ArgumentException("評価は1〜5で指定してください。");
        }

        var assistedContent = await _openAiClient.AssistAsync(
            normalizedContent,
            request.Score,
            request.RoomName
        );

        return new AiReviewAssistResponse
        {
            AssistedContent = assistedContent
        };
    }
}