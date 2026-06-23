# Studio Book 基本設計書

## 1. 文書概要

### 1.1 文書目的

本書は、Studio Book の要件定義書に基づき、システム全体の構成、画面、API、認証・認可、業務処理、データ、外部サービス連携、ログ、エラー処理、非機能およびデプロイ方式の基本設計を定義するものである。

本書は、詳細設計書、API仕様書、テーブル定義書、テスト仕様書を作成する際の基礎資料として使用する。

### 1.2 参照資料

| 資料 | パス |
|---|---|
| 要件定義書 | `docs/design/requirements-definition.md` |
| 画面一覧 | `docs/inventory/screen-inventory.md` |
| DB要件書 | `docs/inventory/database-requirements.md` |
| 非機能要件書 | `docs/inventory/non-functional-requirements.md` |
| アーキテクチャ資料 | `docs/ARCHITECTURE.md` |
| Backend README | `Backend/README.md` |
| Frontend README | `Frontend/README.md` |

### 1.3 設計対象

- Next.jsフロントエンド
- ASP.NET Core Web API
- MySQLデータベース
- Stripe決済連携
- OpenAI API連携
- メール送信連携
- PDF・CSV出力
- Azure Static Web Apps
- Heroku
- JawsDB MySQL

---

# 2. システム構成

## 2.1 全体構成

```text
[利用者ブラウザ]
        |
        | HTTPS
        v
[Next.js Frontend]
Azure Static Web Apps
        |
        | HTTPS / JSON
        v
[ASP.NET Core Web API]
Heroku
        |
        | EF Core
        v
[JawsDB MySQL]

[ASP.NET Core Web API]
   ├─ Stripe Checkout / Webhook
   ├─ OpenAI API
   ├─ メール送信サービス
   └─ QuestPDF
```

## 2.2 ローカル構成

```text
[Next.js Frontend]
http://localhost:3000
        |
        | HTTP
        v
[ASP.NET Core API]
http://localhost:5000
        |
        | TCP 3306
        v
[MySQL 8.4]
Docker Compose
```

## 2.3 構成要素

| 構成要素 | 技術 | 主な責務 |
|---|---|---|
| Frontend | Next.js / React / TypeScript | 画面表示、入力、API呼出し、画面遷移 |
| Backend API | ASP.NET Core Web API | 認証、認可、業務処理、外部連携 |
| Service Layer | C# Service群 | 料金計算、予約、売上、AI、ログ等 |
| ORM | Entity Framework Core | DBアクセス、Migration |
| Database | MySQL | 永続データ保存 |
| Payment | Stripe | Checkout、決済結果通知 |
| AI | OpenAI API | 自然文検索、レビュー文補助 |
| Mail | Mailtrap等 | 認証・再設定メール |
| PDF | QuestPDF | 売上・帳票PDF |
| CI | GitHub Actions | ビルド、テスト、デプロイ |
| Frontend Hosting | Azure Static Web Apps | Next.js静的配信 |
| Backend Hosting | Heroku | ASP.NET Core API |
| DB Hosting | JawsDB MySQL | 本番MySQL |

---

# 3. ソフトウェア構成

## 3.1 Backend構成

```text
Controllers
   ↓
Services / Service Interfaces
   ↓
DbContext / Entities
   ↓
MySQL
```

| 層 | 責務 |
|---|---|
| Controller | HTTP入出力、認証・認可、ステータスコード変換 |
| Service | 業務ロジック、検証、集計、トランザクション |
| Client | Stripe、OpenAI、メール等の外部接続 |
| DTO | API入出力モデル |
| Entity | 永続化モデル |
| DbContext | EF Core設定、関連、制約 |
| Settings | 外部設定の型定義 |
| Seeder | 初期データ・デモデータ投入 |

## 3.2 Frontend構成

```text
src/app
├─ 共通・公開画面
├─ auth
├─ user
├─ rooms
├─ reservations
├─ host
├─ admin
└─ error

src/components
├─ layout
├─ errors
└─ 共通UI

src/lib
└─ apiFetch
```

| 要素 | 責務 |
|---|---|
| Page | 画面単位の状態・API呼出し |
| Component | 共通表示・共通操作 |
| Header / Footer | 共通レイアウト |
| AuthNav | ロール別メニュー |
| apiFetch | API通信共通処理 |
| layout.tsx | 全画面共通構成 |
| loading.tsx | 共通ローディング |
| global-error.tsx | 未処理例外表示 |

---

# 4. 利用者・権限設計

## 4.1 ロール

| ロール | 権限概要 |
|---|---|
| GeneralUser | 検索、予約、決済、予約履歴、レビュー |
| Host | 所有スタジオ、営業時間、休館日、料金、予約、レビュー、売上 |
| Admin | ユーザー、スタジオ、予約、設定、ログ、全体統計 |

## 4.2 権限判定

1. API到達時にJWTを検証する。
2. ControllerまたはAuthorization属性でロールを検証する。
3. Host系ではログインユーザーが対象Roomの所有者であることをService側で検証する。
4. GeneralUser系では対象予約のUserIdがログインユーザーと一致することを確認する。
5. 権限不足時は403を返す。
6. 未認証時は401を返す。

## 4.3 ロール別メニュー

### GeneralUser

- 会員情報
- 予約一覧・履歴
- ログアウト

### Host

- 会員情報
- スタジオ一覧
- 予約一覧
- レビュー一覧
- 売上一覧
- 統計一覧
- ログアウト

### Admin

- 会員情報
- データ一覧
- 会員一覧
- スタジオ一覧
- 予約一覧
- ログ一覧
- 管理設定
- AI検索ログ
- ログアウト

---

# 5. 機能構成

## 5.1 機能一覧

| 機能分類 | 主な機能 |
|---|---|
| 認証 | 会員登録、メール認証、ログイン、ログアウト、パスワード再設定 |
| 公開 | トップ、スタジオ検索、スタジオ詳細、規約 |
| 一般ユーザー | 予約、決済、予約履歴、レビュー |
| ホスト | スタジオ管理、営業時間、休館日、料金、予約、レビュー、売上、統計 |
| 管理者 | ユーザー、スタジオ、予約、設定、監査、AIログ、統計 |
| AI | 自然文検索、レビュー補助 |
| 外部連携 | Stripe、OpenAI、メール |
| 帳票 | CSV、PDF |
| 運用 | Health、Swagger、Migration、Seed |

---

# 6. 画面設計概要

## 6.1 共通画面構成

```text
RootLayout
├─ Header
│  ├─ Logo
│  └─ AuthNav
├─ Main
│  └─ 各Page
└─ Footer
```

## 6.2 共通画面部品

| 部品 | 設計概要 |
|---|---|
| Header | ロゴ、認証メニューを表示 |
| AuthNav | 認証状態とロールに応じてリンクを切替 |
| Footer | 利用規約、プライバシー、特商法リンク |
| Loading | 全画面オーバーレイで読込状態を表示 |
| ErrorPageTemplate | エラーコード、説明、操作ボタンを共通表示 |
| GlobalError | 未処理例外の再試行・ホーム遷移 |

## 6.3 画面遷移概要

### 未認証

```text
トップ
├─ スタジオ一覧
│  ├─ AI検索
│  └─ スタジオ詳細
├─ ログイン
├─ 会員登録
├─ パスワード再設定
├─ 利用規約
├─ プライバシーポリシー
└─ 特定商取引法表記
```

### 一般ユーザー

```text
スタジオ詳細
  ↓
予約入力
  ↓
予約確認
  ↓
Stripe Checkout
  ↓
予約一覧
  ↓
レビュー投稿
```

### ホスト

```text
ホストトップ
├─ スタジオ一覧
│  └─ スタジオ詳細
│     ├─ 営業時間
│     ├─ 休館日
│     └─ 料金ルール
├─ 予約一覧
├─ レビュー一覧
├─ 売上一覧
│  └─ 売上詳細
└─ 統計一覧
```

### 管理者

```text
管理者トップ
├─ データ一覧
├─ 会員一覧
│  └─ 会員詳細
├─ スタジオ一覧
│  ├─ 登録
│  └─ 詳細
│     └─ 編集
├─ 予約一覧
├─ 監査ログ
├─ AI検索ログ
└─ 管理設定
```

---

# 7. API設計概要

## 7.1 基本方針

1. JSONを使用する。
2. API URLは `/api` 配下とする。
3. 認証APIは `/api/auth` 配下とする。
4. 公開スタジオAPIは `/api/rooms` 配下とする。
5. ホストAPIは `/api/host` 配下とする。
6. 管理者APIは `/api/admin` 配下とする。
7. AI APIは `/api/ai` 配下とする。
8. 一覧APIはページング可能とする。
9. 検索条件はQuery Stringで受け取る。
10. 登録・更新はJSON Bodyで受け取る。

## 7.2 HTTPステータス

| ステータス | 用途 |
|---|---|
| 200 | 正常取得・更新 |
| 201 | 登録成功 |
| 204 | 削除成功・本文なし |
| 400 | 入力不正 |
| 401 | 未認証 |
| 403 | 権限不足 |
| 404 | 対象なし |
| 409 | 競合 |
| 429 | レート制限超過 |
| 500 | 内部エラー |
| 503 | 一時利用不可 |

## 7.3 API分類

| 分類 | 代表Endpoint |
|---|---|
| 認証 | `/api/auth/signup`、`/login`、`/logout`、`/me` |
| 公開 | `/api/home`、`/api/rooms` |
| 予約 | `/api/reservations/*` |
| レビュー | `/api/rooms/{id}/reviews*` |
| AI | `/api/ai/room-search`、`/api/ai/review-assist` |
| ホスト | `/api/host/*` |
| 管理者 | `/api/admin/*` |

---

# 8. 認証・認可設計

## 8.1 認証方式

JWT Bearer認証を使用する。

検証項目：

- Issuer
- Audience
- 署名鍵
- 有効期限
- ClockSkew = 0

## 8.2 トークン取得方法

Backendは以下を受け付ける。

1. Authorizationヘッダー
2. `auth_token` Cookie

現行FrontendはlocalStorageからトークンを取得し、Authorizationヘッダーへ設定する。

## 8.3 ログイン処理

```text
メール・パスワード入力
  ↓
ユーザー取得
  ↓
Enabled確認
  ↓
メール認証状態確認
  ↓
パスワード検証
  ↓
ロール取得
  ↓
JWT生成
  ↓
クライアント返却
```

## 8.4 ログアウト処理

```text
POST /api/auth/logout
  ↓
クライアント側token削除
  ↓
認証状態初期化
  ↓
トップへ遷移
```

## 8.5 要確認

- 正式方式をBearerとCookieのどちらに統一するか
- HttpOnly Cookieを採用する場合のCSRF対策
- AccessTokenMinutesの正式値

---

# 9. 会員登録・メール認証設計

## 9.1 会員登録処理

```text
入力検証
  ↓
Email重複確認
  ↓
PasswordHash生成
  ↓
Users登録
  ↓
GeneralUserロール付与
  ↓
VerificationToken発行
  ↓
認証メール送信
```

## 9.2 メール認証処理

```text
Token受信
  ↓
Token存在確認
  ↓
有効期限確認
  ↓
未使用確認
  ↓
Users.EmailVerifiedAtUtc更新
  ↓
VerificationTokens.UsedAtUtc更新
```

## 9.3 パスワード再設定

```text
メール入力
  ↓
ユーザー確認
  ↓
PasswordResetToken発行
  ↓
メール送信
  ↓
Token検証
  ↓
PasswordHash更新
  ↓
UsedAtUtc更新
```

---

# 10. スタジオ検索設計

## 10.1 通常検索

### 検索条件

- キーワード
- エリア
- 上限価格
- 並び順
- ページ
- ページサイズ

### 検索対象

- Rooms.Name
- Rooms.Address
- Rooms.Price
- Reviews平均評価
- 作成日時

### 出力項目

- スタジオID
- 名称
- 画像
- 説明
- 郵便番号
- 住所
- 料金
- 平均評価
- レビュー件数

## 10.2 スタジオ詳細

表示項目：

- 基本情報
- 所有ホスト名
- 料金
- 定員
- 住所
- 営業時間
- 料金ルール
- レビュー
- カレンダーイベント
- 予約・休館情報

---

# 11. AIスタジオ検索設計

## 11.1 処理フロー

```text
自然文入力
  ↓
入力検証
  ↓
RateLimiter判定
  ↓
OpenAI API呼出し
  ↓
検索条件へ変換
  ↓
Rooms検索
  ↓
候補理由生成
  ↓
レスポンス返却
  ↓
AiSearchLogs記録
```

## 11.2 レート制限

| 項目 | 設計 |
|---|---|
| ポリシー | AiSearchPolicy |
| 単位 | IPアドレス |
| 方式 | Fixed Window |
| 上限 | 1分5回 |
| Queue | なし |
| 超過時 | 429 |
| ログ | AiSearchLogsへ失敗記録 |

## 11.3 入力

- 最大200文字を基本とする
- 空文字不可
- 検索文をOpenAIへ送信する

## 11.4 出力

- 元の検索文
- 解釈した条件
- 候補スタジオ
- 候補理由

## 11.5 ログ

- CreatedAtUtc
- Query
- IpAddress
- UserId
- Model
- Succeeded
- ResultCount
- ErrorMessage

---

# 12. 予約設計

## 12.1 予約入力

入力項目：

- RoomId
- 開始日
- 開始時刻
- 終了日
- 終了時刻

## 12.2 予約可否判定

以下を確認する。

1. Roomが存在する
2. 利用者がGeneralUserである
3. 開始日時 < 終了日時
4. 過去日時でない
5. 営業時間内
6. 休館期間外
7. 既存の有効予約と重複しない

## 12.3 予約重複判定

対象条件：

```text
同一RoomId
かつ
既存StartAt < 新規EndAt
かつ
既存EndAt > 新規StartAt
かつ
Status != canceled
```

DB一意制約ではなくService側で判定する。

## 12.4 予約状態

| 状態 | 意味 |
|---|---|
| booked | 予約受付・決済待ちまたは仮予約 |
| paid | 決済完了 |
| canceled | キャンセル済み |

`booked`の正式な業務定義は要確認とする。

---

# 13. 料金計算設計

## 13.1 基本方針

1. Room.Priceを時間単価の基準とする。
2. BusinessHoursで営業時間を判定する。
3. PriceRulesを適用する。
4. AppSettingsから税率・管理手数料率を取得する。
5. 明細をReservationChargeItemsへ保存する。

## 13.2 料金ルール

### multiplier

```text
基本料金 × 倍率 × 対象時間
```

### flat_fee

```text
対象条件に該当した場合に固定額加算
```

## 13.3 明細種別

- base
- multiplier
- tax
- platform_fee

## 13.4 計算結果

- 時間数
- 小計
- 税率
- 税額
- 管理手数料率
- 管理手数料
- 合計
- 明細一覧

## 13.5 保存方針

予約確定時点の計算結果をReservationChargeItemsへ保存し、将来の料金ルール変更の影響を受けないようにする。

---

# 14. Stripe決済設計

## 14.1 Checkout作成

```text
予約確認情報
  ↓
Stripe Checkout Session作成
  ↓
PublishableKey / SessionId / CheckoutUrl返却
```

## 14.2 Webhook処理

```text
Stripe Webhook受信
  ↓
署名検証
  ↓
イベント種別確認
  ↓
対象予約特定
  ↓
重複処理確認
  ↓
Reservations.Status更新
  ↓
監査ログ記録
```

## 14.3 設定

- SecretKey
- PublishableKey
- WebhookSecret
- SuccessUrl
- CancelUrl

## 14.4 設計要件

1. カード情報を保存しない。
2. Webhook署名を検証する。
3. Webhook再送に対し冪等に処理する。
4. 画面リダイレクトだけでpaidへ更新しない。
5. Stripe障害時は決済完了扱いにしない。

---

# 15. レビュー設計

## 15.1 投稿条件

- ログイン済み
- GeneralUser
- 対象スタジオの利用実績あり
- 未投稿であること

## 15.2 入力

- Score：1～5
- Content：必須
- ReservationId：画面Queryとして受領するがDBには保持しない

## 15.3 AIレビュー補助

```text
下書き入力
  ↓
入力検証
  ↓
OpenAI API
  ↓
整形済み文章返却
  ↓
ユーザー確認・編集
  ↓
投稿
```

## 15.4 ホスト返信

- HostReply
- HostReplyAt
- PublicVisible
- HiddenReason

## 15.5 要確認

ReviewsにReservationIdがないため、1予約1レビューをDBで保証できない。Service側判定を確認する。

---

# 16. ホスト機能設計

## 16.1 所有スタジオ管理

1. ログインホストのUserIdでRoomsを絞り込む。
2. 他ホストのRoomIdを指定された場合は403または404とする。
3. スタジオ詳細では営業時間、料金ルール、休館日をまとめて表示する。

## 16.2 営業時間管理

- RoomId + DayOfWeekは一意
- 月=1～日=7
- 休業日はStartTime・EndTimeをNULL可
- 営業日は時刻必須

## 16.3 休館日管理

- StartAt
- EndAt
- Reason
- カレンダーイベント表示
- 予約済み時間との重複方針はServiceで制御

## 16.4 料金ルール管理

- multiplier
- flat_fee
- Weekday
- StartHour
- EndHour
- Multiplier
- FlatFee
- Note

## 16.5 予約管理

- キーワード
- 予約ID
- 状態
- スタジオ
- 期間
- 承認
- キャンセル

## 16.6 レビュー管理

- 一覧
- 評価絞り込み
- 非表示状態
- 返信
- 公開・非公開変更

## 16.7 売上管理

- 売上一覧
- 予約単位明細
- CSV出力
- PDF出力
- 月別集計
- 稼働率
- 平均評価

---

# 17. 管理者機能設計

## 17.1 ユーザー管理

- 一覧検索
- ページング
- 詳細表示
- ロール表示
- Enabled表示
- 将来的な無効化操作

## 17.2 スタジオ管理

- 一覧
- 詳細
- 登録
- 編集
- ホスト選択
- 画像選択
- 価格・定員・住所設定

## 17.3 予約管理

- 全予約検索
- 予約ID
- 状態
- スタジオ
- 期間
- ユーザー情報表示

## 17.4 設定管理

AppSettingsを使用して以下を管理する。

- 税率
- 管理手数料率

更新時はAuditLogsへ記録する。

## 17.5 監査ログ

検索条件：

- 操作者
- Action
- Entity
- EntityId
- 期間

## 17.6 AI検索ログ

検索条件：

- Query
- UserId
- IpAddress
- Model
- Succeeded
- 期間

## 17.7 全体統計

- スタジオ数
- ホスト数
- 予約数
- 売上
- 管理手数料
- 月別推移
- 稼働率
- レビュー平均

---

# 18. データベース設計概要

## 18.1 テーブル構成

| 分類 | テーブル |
|---|---|
| 会員 | Users、Roles、UserRoles |
| 認証 | VerificationTokens、PasswordResetTokens |
| スタジオ | Rooms、BusinessHours、Closures、PriceRules |
| 予約 | Reservations、ReservationChargeItems |
| レビュー | Reviews |
| 設定 | AppSettings |
| ログ | AuditLogs、AiSearchLogs |

## 18.2 主な関連

```text
Users
├─ UserRoles ─ Roles
├─ VerificationTokens
├─ PasswordResetTokens
├─ Rooms
├─ Reservations
└─ Reviews

Rooms
├─ BusinessHours
├─ Closures
├─ PriceRules
├─ Reservations
└─ Reviews

Reservations
└─ ReservationChargeItems
```

## 18.3 削除方針

| 親 | 子 | 動作 |
|---|---|---|
| Users | UserRoles | Cascade |
| Users | Tokens | Cascade |
| Users | Rooms | Cascade |
| Rooms | BusinessHours等 | Cascade |
| Rooms | Reservations | Restrict |
| Rooms | Reviews | Restrict |
| Users | Reservations | Restrict |
| Users | Reviews | Restrict |
| Reservations | ChargeItems | Cascade |

## 18.4 論理削除

Users.Enabledを用いる。

将来的にRoomsにも公開・無効状態の追加を検討する。

---

# 19. ログ・監査設計

## 19.1 アプリケーションログ

ILoggerを使用する。

記録対象：

- 起動
- Migration
- Seed
- 例外
- 外部サービス失敗
- 重要業務処理

## 19.2 AuditLogs

| 項目 | 内容 |
|---|---|
| Ts | 操作日時 |
| ActorId | 操作者 |
| Action | 操作 |
| Entity | 対象種別 |
| EntityId | 対象ID |
| Note | 補足 |

## 19.3 AiSearchLogs

AI検索の成功・失敗を記録する。

## 19.4 禁止情報

- パスワード
- JWT
- Stripe SecretKey
- Stripe WebhookSecret
- OpenAI API Key
- カード情報

---

# 20. エラー処理設計

## 20.1 APIエラー

| 状況 | Status |
|---|---|
| 入力不正 | 400 |
| 未認証 | 401 |
| 権限不足 | 403 |
| 対象なし | 404 |
| 重複・競合 | 409 |
| AI利用超過 | 429 |
| 内部例外 | 500 |
| 一時停止 | 503 |

## 20.2 Frontendエラー

- 401：ログインへ遷移
- 403：権限エラー
- 404：NotFound
- 500：GlobalError
- 503：Service Unavailable
- 通信例外：画面内メッセージ

## 20.3 エラーメッセージ

1. 利用者向けメッセージは日本語とする。
2. 内部例外詳細をそのまま公開しない。
3. 調査用情報はサーバーログへ記録する。
4. 一部ソースの文字化けメッセージをUTF-8へ修正する。

---

# 21. PDF・CSV設計

## 21.1 PDF

- QuestPDF
- Community License
- NotoSansJP-Regular
- NotoSansJP-Bold
- フォント存在時に登録

### 要件

1. 日本語文字化けを防止する。
2. 金額・日時・明細を整形する。
3. フォント未配置時の検知を追加検討する。
4. PDF出力失敗時は500系を返す。

## 21.2 CSV

1. UTF-8を基本とする。
2. カンマ、改行、引用符をエスケープする。
3. Excel利用を考慮する。
4. 数式インジェクションを必要に応じ防止する。

---

# 22. 非機能設計概要

## 22.1 性能

- 通常API：95%以上を3秒以内
- AI：30秒以内
- PDF・CSV：10秒以内
- 同時利用者：20ユーザー程度

## 22.2 可用性

- `/health`
- 月間99.0%以上
- RTO 4時間
- RPO 24時間

## 22.3 バックアップ

- 日次
- 7世代以上
- 半年に1回復元確認

## 22.4 監視

- 5分間隔でHealth確認
- 3回連続失敗で障害扱い
- 将来的にDB Health追加

## 22.5 セキュリティ

- HTTPS
- Secrets管理
- CORS制限
- ロール認可
- 入力検証
- レート制限

---

# 23. 設定設計

## 23.1 Backend設定

| 設定 | 用途 |
|---|---|
| ConnectionStrings:DefaultConnection | DB接続 |
| Jwt:Issuer | JWT発行者 |
| Jwt:Audience | JWT利用者 |
| Jwt:SigningKey | 署名鍵 |
| Jwt:AccessTokenMinutes | 有効期限 |
| Stripe:* | Stripe |
| OpenAI:* | OpenAI |
| Cors:AllowedOrigins | CORS |
| ENABLE_SWAGGER | Swagger |
| ENABLE_DB_MIGRATION | Migration |
| ENABLE_DB_SEED | Seed |

## 23.2 Frontend設定

| 設定 | 用途 |
|---|---|
| NEXT_PUBLIC_API_BASE_URL | API URL |

## 23.3 秘密情報

環境変数、User Secrets、ホスティングSecretsで管理する。

---

# 24. デプロイ・運用設計

## 24.1 本番

```text
Azure Static Web Apps
        ↓
Heroku API
        ↓
JawsDB MySQL
```

## 24.2 起動処理

```text
アプリ起動
  ↓
設定読込
  ↓
サービス登録
  ↓
必要に応じMigration
  ↓
必要に応じSeed
  ↓
API開始
```

## 24.3 Swagger

- DevelopmentまたはENABLE_SWAGGER=true
- 本番は原則無効

## 24.4 Migration

- ENABLE_DB_MIGRATIONで制御
- 失敗時は起動停止
- 本番適用前にバックアップ

## 24.5 Seed

- ENABLE_DB_SEEDで制御
- 本番原則無効

---

# 25. テスト方針

## 25.1 Backend

- Controller
- Service
- Auth
- Room
- Reservation
- Review
- Stripe
- AI
- Host
- Admin

## 25.2 Frontend

- Page
- Header
- Footer
- AuthNav
- Form入力
- API成功・失敗
- ロール表示
- エラー画面

## 25.3 統合

- JWT認証
- ロール認可
- DB
- 予約重複
- Stripe Webhook
- OpenAI異常
- CSV・PDF

## 25.4 CI

- restore
- build
- test
- frontend install
- frontend test
- deploy

---

# 26. 制約・要確認事項

1. JWTの正式保存方式をBearerまたはHttpOnly Cookieへ統一する。
2. `/login` と `/auth/login` を統一する。
3. `booked`の業務上の意味を確定する。
4. Stripe Webhookの冪等性実装を確認・強化する。
5. ReviewsとReservationsの関連方法を確定する。
6. 予約重複時のトランザクション・ロック方式を確定する。
7. BusinessHoursとPriceRulesの曜日体系差を共通化する。
8. DB付きHealthCheckを追加検討する。
9. Program.csの文字化けを修正する。
10. SwaggerのBearer認証設定を追加検討する。
11. EF Core 9.0.1と.NET 8の正式な組合せを確認する。
12. 本番ログ保存先と保持期間を確定する。
13. 本番バックアップ方法を確定する。
14. OpenAI、Stripe、メールのタイムアウト値を確定する。
15. Roomsの論理削除・公開状態を追加検討する。

---

# 27. 詳細設計への引継ぎ事項

詳細設計書では以下を具体化する。

- 全画面の項目・入力・表示・遷移
- 全APIのRequest・Response
- Controller・Service単位の処理
- 料金計算アルゴリズム
- 予約重複判定
- Stripe Webhookイベント処理
- AI Prompt・Response構造
- メール本文・URL
- 全テーブル・全カラム
- トランザクション境界
- 例外・エラーコード
- CSV・PDFレイアウト
- ログ出力項目
- テストケース
