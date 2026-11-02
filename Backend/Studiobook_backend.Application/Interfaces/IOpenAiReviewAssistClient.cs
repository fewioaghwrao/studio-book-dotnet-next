namespace Studiobook_backend.Application.Interfaces;

/// <summary>
/// レビュー本文の推敲を行う外部 AI クライアント。
/// </summary>
public interface IOpenAiReviewAssistClient
{
    Task<string> AssistAsync(string content, int score, string? roomName);
}
