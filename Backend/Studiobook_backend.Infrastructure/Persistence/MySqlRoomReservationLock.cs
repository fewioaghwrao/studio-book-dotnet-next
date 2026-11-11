using Studiobook_backend.Application.Interfaces;

namespace Studiobook_backend.Infrastructure.Persistence;

/// <summary>
/// スタジオ単位の予約登録ロック（MySQL の名前付きロック）。
/// 一定時間内に取得できない場合は TimeoutException（Webhook は 500 を返し、Stripe の再送で再試行）。
/// テスト（InMemory）のように DB がリレーショナルでない場合は何もしない。
/// </summary>
public sealed class MySqlRoomReservationLock : IRoomReservationLock
{
    private const int TimeoutSeconds = 10;

    private readonly AppDbContext _db;

    public MySqlRoomReservationLock(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IAsyncDisposable> AcquireAsync(int roomId, CancellationToken cancellationToken = default)
    {
        // 名前は 64 文字以内
        var lockName = $"studiobook:room-reservation:{roomId}";

        var acquired = await MySqlNamedLock.TryAcquireAsync(_db, lockName, TimeoutSeconds, cancellationToken);

        return acquired ?? throw new TimeoutException(
            $"スタジオの予約登録ロックを取得できませんでした。roomId={roomId}");
    }
}
