using Studiobook_backend.Application.Interfaces;

namespace Studiobook_backend.Infrastructure.Persistence;

/// <summary>
/// バッチジョブ単位のロック（MySQL の名前付きロック）。ほかで実行中なら待たずに null を返す。
/// </summary>
public sealed class MySqlBatchJobLock : IBatchJobLock
{
    private readonly AppDbContext _db;

    public MySqlBatchJobLock(AppDbContext db)
    {
        _db = db;
    }

    public Task<IAsyncDisposable?> TryAcquireAsync(string jobName, CancellationToken cancellationToken = default)
    {
        // 名前は 64 文字以内
        var lockName = $"studiobook:batch:{jobName}";

        return MySqlNamedLock.TryAcquireAsync(_db, lockName, timeoutSeconds: 0, cancellationToken);
    }
}
