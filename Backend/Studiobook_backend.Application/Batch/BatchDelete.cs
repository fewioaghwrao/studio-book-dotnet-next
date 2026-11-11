using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Application.Interfaces;

namespace Studiobook_backend.Application.Batch;

internal static class BatchDelete
{
    /// <summary>
    /// 条件に合う行を chunkSize 件ずつ削除する（1 回のトランザクションを短くし、ほかの処理を待たせない）。
    /// ExecuteDelete はテスト用の InMemory プロバイダで使えないため、読み込んでから削除する。
    /// </summary>
    public static async Task<int> InChunksAsync<T>(
        IAppDbContext db,
        IQueryable<T> query,
        DbSet<T> set,
        int chunkSize,
        CancellationToken cancellationToken)
        where T : class
    {
        var size = Math.Max(1, chunkSize);
        var total = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunk = await query.Take(size).ToListAsync(cancellationToken);

            if (chunk.Count == 0)
            {
                return total;
            }

            set.RemoveRange(chunk);
            await db.SaveChangesAsync(cancellationToken);

            total += chunk.Count;

            if (chunk.Count < size)
            {
                return total;
            }
        }
    }
}
