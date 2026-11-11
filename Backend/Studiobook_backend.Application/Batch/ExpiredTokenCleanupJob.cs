using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Application.Settings;

namespace Studiobook_backend.Application.Batch;

/// <summary>
/// 期限切れ・使用済みのメール認証トークンとパスワード再設定トークンを削除する。
/// トークンの検証は「未使用かつ期限内」だけを有効とするため、削除しても動作は変わらない。
/// 問い合わせ調査のため、期限切れ・使用から一定日数（既定 7 日）は残す。
/// </summary>
public class ExpiredTokenCleanupJob : IBatchJob
{
    private readonly IAppDbContext _db;
    private readonly BatchSettings _settings;
    private readonly TimeProvider _timeProvider;

    public ExpiredTokenCleanupJob(
        IAppDbContext db,
        IOptions<BatchSettings> settings,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _settings = settings.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public string Name => "token_cleanup";

    public async Task<BatchJobResult> RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-_settings.TokenRetentionDays);

        var verificationTokens = await BatchDelete.InChunksAsync(
            _db,
            _db.VerificationTokens
                .Where(x => x.ExpiresAtUtc < cutoff || (x.UsedAtUtc != null && x.UsedAtUtc < cutoff))
                .OrderBy(x => x.Id),
            _db.VerificationTokens,
            _settings.DeleteChunkSize,
            cancellationToken);

        var passwordResetTokens = await BatchDelete.InChunksAsync(
            _db,
            _db.PasswordResetTokens
                .Where(x => x.ExpiresAtUtc < cutoff || (x.UsedAtUtc != null && x.UsedAtUtc < cutoff))
                .OrderBy(x => x.Id),
            _db.PasswordResetTokens,
            _settings.DeleteChunkSize,
            cancellationToken);

        return new BatchJobResult(
            verificationTokens + passwordResetTokens,
            $"verification_tokens={verificationTokens},password_reset_tokens={passwordResetTokens}");
    }
}
