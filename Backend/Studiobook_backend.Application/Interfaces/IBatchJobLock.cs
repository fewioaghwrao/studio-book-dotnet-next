namespace Studiobook_backend.Application.Interfaces;

/// <summary>
/// 同じバッチジョブが同時に動かないようにするロック（複数サーバーでも有効な DB のロック）。
/// </summary>
public interface IBatchJobLock
{
    /// <summary>ロックを取得する。ほかで実行中なら待たずに null を返す。</summary>
    Task<IAsyncDisposable?> TryAcquireAsync(string jobName, CancellationToken cancellationToken = default);
}
