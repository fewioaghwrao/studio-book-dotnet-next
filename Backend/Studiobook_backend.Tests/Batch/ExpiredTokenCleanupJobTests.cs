using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Studiobook_backend.Application.Batch;
using Studiobook_backend.Application.Settings;
using Studiobook_backend.Domain.Entities;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Batch;

public class ExpiredTokenCleanupJobTests
{
    private static readonly DateTime NowUtc = new(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RunAsync_期限切れまたは使用済みから7日を過ぎたトークンだけを削除する()
    {
        await using var context = TestDbContextFactory.Create();

        context.VerificationTokens.AddRange(
            Verification(1, expiresAtUtc: NowUtc.AddDays(-8), usedAtUtc: null),             // 期限切れ 8 日 → 削除
            Verification(2, expiresAtUtc: NowUtc.AddDays(-6), usedAtUtc: null),             // 期限切れ 6 日 → 残す
            Verification(3, expiresAtUtc: NowUtc.AddHours(10), usedAtUtc: null),            // 有効 → 残す
            Verification(4, expiresAtUtc: NowUtc.AddDays(-5), usedAtUtc: NowUtc.AddDays(-9))); // 使用 9 日前 → 削除

        context.PasswordResetTokens.AddRange(
            PasswordReset(1, expiresAtUtc: NowUtc.AddDays(-30), usedAtUtc: null),           // 削除
            PasswordReset(2, expiresAtUtc: NowUtc.AddMinutes(30), usedAtUtc: null));        // 残す

        await context.SaveChangesAsync();

        var job = new ExpiredTokenCleanupJob(
            context,
            Options.Create(new BatchSettings()),
            new FixedTimeProvider(new DateTimeOffset(NowUtc)));

        var result = await job.RunAsync(CancellationToken.None);

        Assert.Equal(3, result.AffectedRows);
        Assert.Equal("verification_tokens=2,password_reset_tokens=1", result.Summary);

        Assert.Equal(new[] { 2, 3 }, await context.VerificationTokens.OrderBy(x => x.Id).Select(x => x.Id).ToListAsync());
        Assert.Equal(new[] { 2 }, await context.PasswordResetTokens.Select(x => x.Id).ToListAsync());
    }

    private static VerificationToken Verification(int id, DateTime expiresAtUtc, DateTime? usedAtUtc) => new()
    {
        Id = id,
        UserId = 1,
        Token = $"v{id}",
        ExpiresAtUtc = expiresAtUtc,
        UsedAtUtc = usedAtUtc,
        CreatedAtUtc = expiresAtUtc.AddDays(-1)
    };

    private static PasswordResetToken PasswordReset(int id, DateTime expiresAtUtc, DateTime? usedAtUtc) => new()
    {
        Id = id,
        UserId = 1,
        Token = $"p{id}",
        ExpiresAtUtc = expiresAtUtc,
        UsedAtUtc = usedAtUtc,
        CreatedAtUtc = expiresAtUtc.AddHours(-1)
    };
}
