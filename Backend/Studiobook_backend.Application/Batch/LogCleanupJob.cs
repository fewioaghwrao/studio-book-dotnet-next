using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Application.Settings;

namespace Studiobook_backend.Application.Batch;

/// <summary>
/// 保持期間を過ぎたログを削除する（要件定義 13.4：AI 検索ログ 90 日、監査ログ 1 年）。
/// </summary>
public class LogCleanupJob : IBatchJob
{
    private readonly IAppDbContext _db;
    private readonly BatchSettings _settings;
    private readonly TimeProvider _timeProvider;

    public LogCleanupJob(
        IAppDbContext db,
        IOptions<BatchSettings> settings,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _settings = settings.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public string Name => "log_cleanup";

    public async Task<BatchJobResult> RunAsync(CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var aiSearchLogCutoff = nowUtc.AddDays(-_settings.AiSearchLogRetentionDays);
        var auditLogCutoff = nowUtc.AddDays(-_settings.AuditLogRetentionDays);

        var aiSearchLogs = await BatchDelete.InChunksAsync(
            _db,
            _db.AiSearchLogs.Where(x => x.CreatedAtUtc < aiSearchLogCutoff).OrderBy(x => x.Id),
            _db.AiSearchLogs,
            _settings.DeleteChunkSize,
            cancellationToken);

        var auditLogs = await BatchDelete.InChunksAsync(
            _db,
            _db.AuditLogs.Where(x => x.Ts < auditLogCutoff).OrderBy(x => x.Id),
            _db.AuditLogs,
            _settings.DeleteChunkSize,
            cancellationToken);

        return new BatchJobResult(
            aiSearchLogs + auditLogs,
            $"ai_search_logs={aiSearchLogs},audit_logs={auditLogs}");
    }
}
