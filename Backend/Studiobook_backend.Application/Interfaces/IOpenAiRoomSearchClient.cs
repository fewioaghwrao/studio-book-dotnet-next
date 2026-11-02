using Studiobook_backend.Application.Dtos.Ai;

namespace Studiobook_backend.Application.Interfaces;

/// <summary>
/// 自然文の検索クエリを検索条件へ変換する外部 AI クライアント。
/// </summary>
public interface IOpenAiRoomSearchClient
{
    Task<AiRoomSearchConditionsDto> AnalyzeQueryAsync(string query);
}
