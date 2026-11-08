using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Infrastructure.Persistence;

namespace Studiobook_backend.Tests.Persistence;

/// <summary>
/// マイグレーションの抜け漏れを検出する。
/// エンティティや AppDbContext の設定を変えたのにマイグレーションを追加していないと、
/// 本番起動時の MigrateAsync で失敗するため、DB に接続せずモデルとスナップショットを比較する。
/// </summary>
public class MigrationSnapshotTests
{
    private static AppDbContext CreateMySqlContextWithoutConnection()
    {
        // 接続はしない（ServerVersion を明示して AutoDetect を避ける）
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                "Server=localhost;Database=studiobook_snapshot_test;User=test;Password=test;",
                new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void モデルとマイグレーションのスナップショットに差分がない()
    {
        using var context = CreateMySqlContextWithoutConnection();

        Assert.False(
            context.Database.HasPendingModelChanges(),
            "モデルの変更に対応するマイグレーションがありません。dotnet ef migrations add で追加してください。");
    }

    [Fact]
    public void 決済IDとレビュー一意制約のマイグレーションが含まれている()
    {
        using var context = CreateMySqlContextWithoutConnection();

        Assert.Contains(
            "20261109000000_AddPaymentIntentIdAndReviewUnique",
            context.Database.GetMigrations());
    }

    [Fact]
    public void 決済IDとレビューに一意インデックスがある()
    {
        using var context = CreateMySqlContextWithoutConnection();

        var reservation = context.Model.FindEntityType(typeof(Studiobook_backend.Domain.Entities.Reservation))!;
        var paymentIndex = Assert.Single(
            reservation.GetIndexes(),
            x => x.Properties.Select(p => p.Name).SequenceEqual(new[] { "PaymentIntentId" }));
        Assert.True(paymentIndex.IsUnique);

        var review = context.Model.FindEntityType(typeof(Studiobook_backend.Domain.Entities.Review))!;
        var reviewIndex = Assert.Single(
            review.GetIndexes(),
            x => x.Properties.Select(p => p.Name).SequenceEqual(new[] { "RoomId", "UserId" }));
        Assert.True(reviewIndex.IsUnique);
    }
}
