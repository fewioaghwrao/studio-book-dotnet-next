using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Application.Interfaces;

namespace Studiobook_backend.Infrastructure.Persistence;

/// <summary>
/// MySQL の名前付きロック（GET_LOCK / RELEASE_LOCK）によるスタジオ単位の予約登録ロック。
/// ロックは DB 接続に紐づくため、取得から解放まで AppDbContext の接続を開いたままにする。
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
        if (!_db.Database.IsRelational())
        {
            return NoopLock.Instance;
        }

        // 名前は 64 文字以内
        var lockName = $"studiobook:room-reservation:{roomId}";

        await _db.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            var result = await ExecuteScalarAsync(
                "SELECT GET_LOCK(@name, @timeout)",
                lockName,
                TimeoutSeconds,
                cancellationToken);

            if (result is null || result is DBNull || Convert.ToInt64(result) != 1)
            {
                throw new TimeoutException(
                    $"スタジオの予約登録ロックを取得できませんでした。roomId={roomId}");
            }

            return new Releaser(this, lockName);
        }
        catch
        {
            await _db.Database.CloseConnectionAsync();
            throw;
        }
    }

    private async Task<object?> ExecuteScalarAsync(
        string sql,
        string lockName,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        DbConnection connection = _db.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var nameParameter = command.CreateParameter();
        nameParameter.ParameterName = "@name";
        nameParameter.Value = lockName;
        command.Parameters.Add(nameParameter);

        if (timeoutSeconds != null)
        {
            var timeoutParameter = command.CreateParameter();
            timeoutParameter.ParameterName = "@timeout";
            timeoutParameter.Value = timeoutSeconds.Value;
            command.Parameters.Add(timeoutParameter);
        }

        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private sealed class Releaser : IAsyncDisposable
    {
        private readonly MySqlRoomReservationLock _owner;
        private readonly string _lockName;
        private bool _released;

        public Releaser(MySqlRoomReservationLock owner, string lockName)
        {
            _owner = owner;
            _lockName = lockName;
        }

        public async ValueTask DisposeAsync()
        {
            if (_released)
            {
                return;
            }

            _released = true;

            try
            {
                await _owner.ExecuteScalarAsync(
                    "SELECT RELEASE_LOCK(@name)",
                    _lockName,
                    timeoutSeconds: null,
                    CancellationToken.None);
            }
            finally
            {
                // 接続を閉じればロックも解放される
                await _owner._db.Database.CloseConnectionAsync();
            }
        }
    }

    private sealed class NoopLock : IAsyncDisposable
    {
        public static readonly NoopLock Instance = new();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
