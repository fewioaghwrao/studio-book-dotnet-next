# Studio Book .NET 10 移行反映メモ

## 反映した変更

- バックエンドの表記を ASP.NET Core 8 Web API から ASP.NET Core 10 Web API へ更新
- ローカル開発要件を .NET 8 SDK から .NET 10 SDK へ更新
- ソリューションファイル表記を `Studiobook_backend.sln` から `Studiobook_backend.slnx` へ更新
- GitHub Actionsのrestore・build・test対象を `Backend/Studiobook_backend.slnx` として記載
- Dockerのビルドイメージを `mcr.microsoft.com/dotnet/sdk:10.0` として記載
- Dockerの実行イメージを `mcr.microsoft.com/dotnet/aspnet:10.0` として記載
- モノレポからHerokuへ送る例を `git subtree push --prefix Backend` に更新
- 非機能要件へ .NET 10、`.slnx`、CI、Dockerの現行構成を追記
- DB要件書へ実行基盤として .NET 10 / ASP.NET Core 10 を追記

## 内容を変更していない資料

- `functional-inventory.md`
- `screen-inventory.md`

上記2資料は機能・画面の棚卸しであり、今回のフレームワークおよびソリューション形式の変更による機能差分がないため、内容は維持しています。

## 要確認事項

EF Core は 9.0.22 に更新しました（ASP.NET Core 関連パッケージは 10.0.14）。
MySQL Provider の Pomelo.EntityFrameworkCore.MySql に EF Core 10 対応版がまだないため、EF Core 10 への更新は保留しています。
EF Core 9 は 2026年11月10日にサポート終了となるため、Pomelo の EF Core 10 対応版リリース後に EF Core 10 へ更新し、DB要件書のORM表記も合わせて変更してください。

Herokuの記載はBuildpack方式とContainer Registry方式の両方を考慮しています。
実際の運用方式が確定している場合は、使用しない方式の説明を削除できます。

## DBマイグレーション：AddPaymentIntentIdAndReviewUnique（B6/B7）

`20261109000000_AddPaymentIntentIdAndReviewUnique` で次を追加します。

起動時のマイグレーションは環境変数 `ENABLE_DB_MIGRATION=true` のときだけ実行されます。Heroku などで未設定の場合は自動では適用されず、新しいコードが存在しない列（`Reservations.PaymentIntentId`）を参照して、予約を読むAPIがエラーになります。デプロイと同時に、次のどちらかで適用してください。

- Heroku の Config Vars に `ENABLE_DB_MIGRATION=true` を設定して再起動する（適用後は `false` に戻してもよい）
- 下記「手動で適用する場合」のSQLを本番DBで実行する

- `Reservations.PaymentIntentId`（varchar(255)、NULL 可）と一意インデックス `IX_Reservations_PaymentIntentId`
- `Reviews` の一意インデックス `IX_Reviews_RoomId_UserId`（同一ユーザー・同一スタジオのレビューは 1 件）

### 適用前の確認

既存の `Reviews` に同じ `RoomId`・`UserId` の組み合わせが 2 件以上あると、一意インデックスの作成に失敗し、起動時のマイグレーションでエラーになります。デプロイ前に本番DBで次のSQLを実行し、0 件であることを確認してください。

```sql
SELECT RoomId, UserId, COUNT(*) AS Count
FROM Reviews
GROUP BY RoomId, UserId
HAVING COUNT(*) > 1;
```

重複がある場合は、残すレビューを決めてから不要な行を削除してください（自動では削除しません）。

既存の予約の `PaymentIntentId` は NULL のままです（一意インデックスは NULL の重複を許可します）。

### 手動で適用する場合

```sql
ALTER TABLE `Reservations` ADD `PaymentIntentId` varchar(255) CHARACTER SET utf8mb4 NULL;
CREATE UNIQUE INDEX `IX_Reservations_PaymentIntentId` ON `Reservations` (`PaymentIntentId`);
CREATE UNIQUE INDEX `IX_Reviews_RoomId_UserId` ON `Reviews` (`RoomId`, `UserId`);
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261109000000_AddPaymentIntentIdAndReviewUnique', '9.0.22');
```

最後の INSERT で適用済みとして記録するため、後で `ENABLE_DB_MIGRATION=true` にしても二重に実行されません。
