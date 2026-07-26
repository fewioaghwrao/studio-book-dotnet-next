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

DB要件書のORM表記 `Entity Framework Core 9.0.1` は、添付資料で確認できる現行値を維持しています。
NuGetパッケージをEF Core 10へ更新した場合は、実際の `.csproj` に合わせて `Entity Framework Core 10.x` へ変更してください。

Herokuの記載はBuildpack方式とContainer Registry方式の両方を考慮しています。
実際の運用方式が確定している場合は、使用しない方式の説明を削除できます。
