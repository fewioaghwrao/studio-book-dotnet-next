using Studiobook_backend.Application.Interfaces;

namespace Studiobook_backend.Tests.Helpers;

/// <summary>返金の呼び出しを記録するテスト用の実装（Stripe には接続しない）。</summary>
public sealed class FakePaymentRefundService : IPaymentRefundService
{
    public List<(string PaymentIntentId, string Reason)> Refunds { get; } = new();

    /// <summary>設定すると RefundAsync がこの例外を投げる（返金 API の障害を想定）。</summary>
    public Exception? ExceptionToThrow { get; set; }

    public Task RefundAsync(string paymentIntentId, string reason, CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        Refunds.Add((paymentIntentId, reason));
        return Task.CompletedTask;
    }
}

/// <summary>ロックの取得・解放を記録するテスト用の実装。</summary>
public sealed class FakeRoomReservationLock : IRoomReservationLock
{
    public List<int> AcquiredRoomIds { get; } = new();

    public int ReleasedCount { get; private set; }

    public Task<IAsyncDisposable> AcquireAsync(int roomId, CancellationToken cancellationToken = default)
    {
        AcquiredRoomIds.Add(roomId);
        return Task.FromResult<IAsyncDisposable>(new Releaser(this));
    }

    private sealed class Releaser : IAsyncDisposable
    {
        private readonly FakeRoomReservationLock _owner;

        public Releaser(FakeRoomReservationLock owner) => _owner = owner;

        public ValueTask DisposeAsync()
        {
            _owner.ReleasedCount++;
            return ValueTask.CompletedTask;
        }
    }
}
