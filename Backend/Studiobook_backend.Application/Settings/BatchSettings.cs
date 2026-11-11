namespace Studiobook_backend.Application.Settings;

/// <summary>
/// 定期バッチの設定。設定キー：Batch:*（環境変数では Batch__Enabled など）
/// </summary>
public class BatchSettings
{
    public const string SectionName = "Batch";

    /// <summary>Web アプリ内で定期実行するか。false でもコマンド（batch run）からは実行できる。</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>1 日 1 回の実行時刻（日本時間、HH:mm）。</summary>
    public string RunAtJapanTime { get; set; } = "04:00";

    /// <summary>実行時刻を過ぎたかを確認する間隔（分）。</summary>
    public int CheckIntervalMinutes { get; set; } = 15;

    /// <summary>起動してから最初に確認するまでの待ち時間（秒）。起動直後の負荷を避ける。</summary>
    public int InitialDelaySeconds { get; set; } = 60;

    /// <summary>AI 検索ログの保持日数（要件定義 13.4：90 日）。</summary>
    public int AiSearchLogRetentionDays { get; set; } = 90;

    /// <summary>監査ログの保持日数（要件定義 13.4：1 年）。</summary>
    public int AuditLogRetentionDays { get; set; } = 365;

    /// <summary>期限切れ・使用済みのトークンを残す日数（問い合わせ調査用の猶予）。</summary>
    public int TokenRetentionDays { get; set; } = 7;

    /// <summary>1 回の SaveChanges で削除する件数。API の応答を遅くしないよう小分けにする。</summary>
    public int DeleteChunkSize { get; set; } = 500;

    public TimeOnly GetRunAtJapanTime()
    {
        return TimeOnly.TryParseExact(RunAtJapanTime, "HH:mm", out var time)
            ? time
            : new TimeOnly(4, 0);
    }
}
