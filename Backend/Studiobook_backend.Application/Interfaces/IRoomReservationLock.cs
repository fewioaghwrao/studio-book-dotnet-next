namespace Studiobook_backend.Application.Interfaces;

/// <summary>
/// スタジオ単位の予約登録ロック。
/// 「重複予約の確認 → 登録」の間に同じスタジオの別の登録が割り込まないようにする
/// （時間帯の重なりは DB の一意制約では防げないため）。
/// 複数サーバー（dyno）でも有効なよう、DB のロックで実装する。
/// </summary>
public interface IRoomReservationLock
{
    /// <summary>
    /// ロックを取得する。戻り値を Dispose するとロックを解放する。
    /// 一定時間内に取得できない場合は <see cref="TimeoutException"/> を投げる。
    /// </summary>
    Task<IAsyncDisposable> AcquireAsync(int roomId, CancellationToken cancellationToken = default);
}
