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
