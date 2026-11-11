namespace Studiobook_backend.Application.Batch;

/// <summary>
/// 1 日 1 回実行するバッチジョブ。
/// Web アプリ内の定期実行（Heroku）と、コマンド（VPS の cron など）の両方から同じ処理を呼ぶ。
/// 何度実行しても結果が同じになるよう（冪等に）作ること。
/// </summary>
public interface IBatchJob
{
    /// <summary>ジョブ名（英小文字と _。AppSettings のキーとロック名に使う）。</summary>
    string Name { get; }

    Task<BatchJobResult> RunAsync(CancellationToken cancellationToken);
}

/// <param name="AffectedRows">削除・更新した件数の合計</param>
/// <param name="Summary">結果の要約（AppSettings に保存するため 100 文字以内に収まる短さにする）</param>
public sealed record BatchJobResult(int AffectedRows, string Summary);
