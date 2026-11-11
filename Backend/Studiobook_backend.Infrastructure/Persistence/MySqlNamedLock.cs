using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Studiobook_backend.Infrastructure.Persistence;

/// <summary>
/// MySQL の名前付きロック（GET_LOCK / RELEASE_LOCK）。
/// ロックは DB 接続に紐づくため、取得から解放まで AppDbContext の接続を開いたままにする。
/// 予約登録（スタジオ単位）とバッチジョブ（ジョブ単位）の排他に使う。
/// </summary>
internal static class MySqlNamedLock
{
    /// <summary>
    /// ロックを取得する。取得できなければ null（接続は閉じる）。
    /// DB がリレーショナルでない場合（テストの InMemory）は何もしないロックを返す。
    /// </summary>
    /// <param name="lockName">64 文字以内</param>
    /// <param name="timeoutSeconds">待つ秒数。0 なら待たない</param>
    public static async Task<IAsyncDisposable?> TryAcquireAsync(
        AppDbContext db,
        string lockName,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        if (!db.Database.IsRelational())
        {
            return NoopLock.Instance;
        }

        await db.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            var result = await ExecuteScalarAsync(
                db,
                "SELECT GET_LOCK(@name, @timeout)",
                lockName,
                timeoutSeconds,
                cancellationToken);

            if (result is null || result is DBNull || Convert.ToInt64(result) != 1)
            {
                await db.Database.CloseConnectionAsync();
                return null;
            }

            return new Releaser(db, lockName);
        }
        catch
        {
            await db.Database.CloseConnectionAsync();
            throw;
        }
    }

    private static async Task<object?> ExecuteScalarAsync(
        AppDbContext db,
        string sql,
        string lockName,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        DbConnection connection = db.Database.GetDbConnection();

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
        private readonly AppDbContext _db;
        private readonly string _lockName;
        private bool _released;

        public Releaser(AppDbContext db, string lockName)
        {
            _db = db;
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
                await ExecuteScalarAsync(
                    _db,
                    "SELECT RELEASE_LOCK(@name)",
                    _lockName,
                    timeoutSeconds: null,
                    CancellationToken.None);
            }
            finally
            {
                // 接続を閉じればロックも解放される
                await _db.Database.CloseConnectionAsync();
            }
        }
    }

    private sealed class NoopLock : IAsyncDisposable
    {
        public static readonly NoopLock Instance = new();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
