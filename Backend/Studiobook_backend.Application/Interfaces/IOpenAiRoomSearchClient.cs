using Studiobook_backend.Application.Dtos.Ai;

namespace Studiobook_backend.Application.Interfaces;

/// <summary>
/// 自然文の検索クエリを検索条件へ変換する外部 AI クライアント。
/// </summary>
public interface IOpenAiRoomSearchClient
{
    /// <param name="query">利用者が入力した検索文</param>
    /// <param name="nowJapan">
    /// 現在の日本時間。「明日」「来週土曜」などの相対的な日付を解決するために AI へ渡す。
    /// 省略時は現在時刻を使う。
    /// </param>
    Task<AiRoomSearchConditionsDto> AnalyzeQueryAsync(string query, DateTime? nowJapan = null);
}
