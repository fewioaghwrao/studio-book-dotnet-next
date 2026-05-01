# Studio Book .NET + Next.js

Next.js フロントエンドと ASP.NET Core Web API を組み合わせた、スタジオ予約サービスのフルスタックポートフォリオアプリです。

一般ユーザーはスタジオ検索・予約・決済・レビュー投稿を行い、スタジオ提供者（ホスト）はスタジオ情報、営業時間、休館日、予約、売上、レビューを管理できます。  
管理者はユーザー・スタジオ・予約・システム設定・ログを確認、管理できます。

本リポジトリは、既存の Java / Spring Boot 版 Studio Book を参考にしつつ、ASP.NET Core Web API + Next.js 構成として再設計したものです。

---

## 概要

### このアプリで示したいこと

- Next.js + ASP.NET Core Web API によるフルスタック構成
- 一般ユーザー / ホスト / 管理者の3ロール設計
- JWT Cookie 認証とロール別アクセス制御
- スタジオ検索、予約、決済、レビュー投稿の一連の業務フロー
- ホスト向けの営業時間・休館日・料金ルール・売上管理
- 管理者向けのユーザー管理、スタジオ管理、予約管理、設定管理、ログ確認
- OpenAI API を利用したAI補助機能
- Stripe Checkout / Webhook を想定した決済連携
- EF Core Migration によるDBスキーマ管理

---

## 主な機能

### 共通

- 会員登録
- メール認証
- ログイン / ログアウト
- JWT Cookie 認証
- パスワードリセット
- 会員情報編集
- ロール別メニュー表示
- エラー画面
- レスポンシブ対応

### 一般ユーザー機能

- スタジオ一覧表示
- スタジオ検索
- AI自然文スタジオ検索
- スタジオ詳細表示
- 営業時間・料金ルール・レビュー確認
- 予約入力
- 予約確認
- Stripe Checkout による決済
- 予約履歴確認
- レビュー投稿
- AIレビュー文補助

### ホスト機能

- ホスト用マイページ
- 自分のスタジオ一覧
- スタジオ詳細確認
- 営業時間管理
- 休館日管理
- 料金ルール管理
- 予約一覧
- 予約承認 / キャンセル
- レビュー一覧
- 売上一覧
- 売上詳細
- 売上CSV出力
- 統計表示

### 管理者機能

- 管理者用マイページ
- ユーザー一覧 / 詳細
- スタジオ一覧 / 詳細 / 登録 / 編集
- 予約一覧
- システム設定管理
- ステータス確認
- 監査ログ一覧
- AI検索ログ一覧

---

## AI機能

### AI自然文スタジオ検索

ユーザーが自然文で希望条件を入力すると、OpenAI API を用いて検索条件へ変換し、条件に近いスタジオを表示します。

**入力例：**

```
落ち着いた雰囲気で、夜に使える撮影向けのスタジオを探したい
```

AIが以下のような条件を解釈します。

- 用途
- 雰囲気
- エリア
- 予算
- 人数
- 時間帯
- キーワード

また、AI検索APIには連打対策として `RateLimiter` を設定し、検索ログを `AiSearchLogs` に保存します。

### AIレビュー文補助

レビュー投稿画面で、ユーザーが入力した感想文をAIが自然なレビュー文に整えます。

AIが生成した文章はそのまま投稿されるのではなく、ユーザーが確認・修正してから投稿する設計です。

---

## 技術スタック

### Backend

| 技術 | 用途 |
|------|------|
| ASP.NET Core Web API (C#) | APIサーバー |
| Entity Framework Core | ORM / DBアクセス |
| SQL Server / LocalDB | データベース |
| JWT Bearer Authentication | 認証 |
| Cookie Authentication | セッション管理 |
| ASP.NET Core Authorization | 認可 |
| ASP.NET Core RateLimiter | レート制限 |
| Stripe API | 決済連携 |
| QuestPDF | PDF生成 |
| OpenAI API | AI機能 |

### Frontend

| 技術 | 用途 |
|------|------|
| Next.js / React / TypeScript | UIフレームワーク |
| Tailwind CSS | スタイリング |
| FullCalendar | カレンダーUI |
| Recharts | グラフ・統計表示 |
| Stripe.js | 決済UI |

### Database

- SQL Server LocalDB
- EF Core Migrations

---

## アーキテクチャ概要

```
[Browser]
   |
   v
[Next.js Frontend]
   |
   v
[ASP.NET Core Web API]
   |
   v
[Service Layer]
   |
   v
[Entity Framework Core]
   |
   v
[SQL Server]
```

**外部サービス連携：**

```
ASP.NET Core Web API
   ├─ Stripe Checkout / Webhook
   └─ OpenAI API
```

---

## ディレクトリ構成

```
studio-book-dotnet-next
├─ Backend
│  ├─ Studiobook_backend.sln
│  └─ Studiobook_backend
│     ├─ Controllers
│     ├─ Data
│     ├─ Dtos
│     ├─ Entities
│     ├─ Migrations
│     ├─ Seeders
│     ├─ Services
│     ├─ Settings
│     ├─ Program.cs
│     └─ appsettings.json
│
└─ Frontend
   ├─ public
   ├─ src
   │  ├─ app
   │  └─ components
   ├─ package.json
   └─ next.config.ts
```

---

## ローカル開発環境

### 前提

- .NET 8 SDK
- Node.js / npm
- SQL Server LocalDB
- Git

### Backend 起動手順

```bash
cd Backend/Studiobook_backend
dotnet restore
dotnet ef database update
dotnet run
```

APIは以下で起動します。

- API: `https://localhost:7226`
- Swagger（開発環境のみ）: `https://localhost:7226/swagger`

### Frontend 起動手順

```bash
cd Frontend
npm install
npm run dev
```

フロントエンドは以下で起動します。

- `http://localhost:3000`

> Turbopack で不安定な場合は、Webpack で起動してください。
>
> ```bash
> npm run dev -- --webpack
> ```

---

## 環境変数・シークレット管理

APIキーや接続情報は Git 管理しません。

### Backend

本物の値は User Secrets または環境変数で管理します。

```bash
dotnet user-secrets set "Jwt:SigningKey" "your-real-jwt-signing-key"
dotnet user-secrets set "OpenAI:ApiKey" "your-openai-api-key"
dotnet user-secrets set "Stripe:SecretKey" "your-stripe-secret-key"
```

### Frontend

`Frontend/.env.local` を作成します（Git管理対象外）。

```
NEXT_PUBLIC_API_BASE_URL=https://localhost:7226
```

---

## 認証・認可

JWT を HttpOnly Cookie に保存し、ASP.NET Core 側で認証・認可を行います。

**主なロール：**

| ロール | 説明 |
|--------|------|
| `GeneralUser` | 一般ユーザー |
| `Host` | スタジオ提供者 |
| `Admin` | 管理者 |

- Host系API：`Host` ロールに加え、自分のスタジオのみ操作できるよう所有者チェックを実施
- Admin系API：`Admin` ロールを持つユーザーのみアクセス可能

---

## ログ管理

### 監査ログ

管理操作や重要なイベントを `AuditLogs` に記録します。

記録対象の例：

- ログイン
- 管理者操作
- 予約操作
- 設定変更

### AI検索ログ

AI自然文検索の利用状況を `AiSearchLogs` に記録します。

記録内容：

- 検索文
- IPアドレス
- ユーザーID
- 使用モデル
- 成功 / 失敗
- 結果件数
- エラー内容

---

## デモサイトについて

現時点では、公開デモサイトは未用意です。ローカル環境での起動を前提としています。

---

## 画面イメージについて

現時点ではスクリーンショットや構成図は未掲載です。今後、以下を追加予定です。

- トップページ
- スタジオ一覧
- スタジオ詳細
- AI自然文検索
- 予約画面
- ホスト管理画面
- 管理者画面
- AI検索ログ画面

---

## 今後の改善予定

- [ ] READMEへの画面スクリーンショット追加
- [ ] ER図・構成図の追加
- [ ] GitHub Actions CI の追加
- [ ] テストコードの追加
- [ ] デモ環境の公開
- [ ] OpenAI API利用量の可視化
- [ ] Stripe Webhook の冪等制御強化
- [ ] 管理者向けログ検索機能の拡張

---

## 注意事項

- このアプリは個人ポートフォリオ用途を想定した学習・実装用アプリです。実運用には、セキュリティ、決済、個人情報保護、監査ログ、運用監視などの追加検討が必要です。
- AI機能は補助機能であり、AIによる条件解釈や文章生成には不正確な内容が含まれる可能性があります。

---

## ライセンス

個人ポートフォリオ用途を想定しています。再利用や公開範囲については、必要に応じてライセンスを追加してください。