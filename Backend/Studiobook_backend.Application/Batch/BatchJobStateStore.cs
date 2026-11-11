using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Domain.Entities;

namespace Studiobook_backend.Application.Batch;

/// <summary>
/// バッチの実行状況を AppSettings（キーと値）に保存する（専用テーブルを作らず、マイグレーション不要）。
///
/// キー：batch:{ジョブ名}:last_succeeded_at_utc / last_failed_at_utc / last_result
/// 値は最大 100 文字のため、日時と短い要約だけを保存する（エラーの詳細はログに出す）。
/// </summary>
public class BatchJobStateStore
{
    private const int ValueMaxLength = 100;

    private readonly IAppDbContext _db;

    public BatchJobStateStore(IAppDbContext db)
    {
        _db = db;
    }

    public static string SucceededKey(string jobName) => $"batch:{jobName}:last_succeeded_at_utc";
    public static string FailedKey(string jobName) => $"batch:{jobName}:last_failed_at_utc";
    public static string ResultKey(string jobName) => $"batch:{jobName}:last_result";

    public async Task<DateTime?> GetLastSucceededUtcAsync(string jobName, CancellationToken cancellationToken = default)
    {
        var value = await _db.AppSettings
            .AsNoTracking()
            .Where(x => x.Key == SucceededKey(jobName))
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out var parsed)
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
            : null;
    }

    public async Task RecordSucceededAsync(string jobName, DateTime nowUtc, string summary, CancellationToken cancellationToken = default)
    {
        await UpsertAsync(SucceededKey(jobName), FormatUtc(nowUtc), nowUtc, cancellationToken);
        await UpsertAsync(ResultKey(jobName), $"ok:{summary}", nowUtc, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordFailedAsync(string jobName, DateTime nowUtc, string errorType, CancellationToken cancellationToken = default)
    {
        await UpsertAsync(FailedKey(jobName), FormatUtc(nowUtc), nowUtc, cancellationToken);
        await UpsertAsync(ResultKey(jobName), $"failed:{errorType}", nowUtc, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertAsync(string key, string value, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var trimmed = value.Length <= ValueMaxLength ? value : value[..ValueMaxLength];

        var setting = await _db.AppSettings.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);

        if (setting == null)
        {
            _db.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = trimmed,
                UpdatedAtUtc = nowUtc
            });

            return;
        }

        setting.Value = trimmed;
        setting.UpdatedAtUtc = nowUtc;
    }

    private static string FormatUtc(DateTime nowUtc)
    {
        return DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc)
            .ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }
}
