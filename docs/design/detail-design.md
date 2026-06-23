# Studio Book 詳細設計書

## 1. 文書概要

### 1.1 文書目的

本書は、スタジオ時間貸し予約サービス「Studio Book」のバックエンド実装を対象として、API、認証・認可、業務ロジック、DTO、Entity、データベース、外部サービス連携、ログ、エラー処理および環境設定の詳細設計を定義する。

本書は、要件定義書および基本設計書を上位資料とし、ASP.NET Core Web APIのController、Service、Client、DTO、Entity、DbContext、Program.csおよびSeederの実装を根拠として作成する。

### 1.2 対象範囲

- ASP.NET Core Web API
- Entity Framework Core
- MySQL
- JWT認証・ロール認可
- 会員登録・メール認証・パスワード再設定
- スタジオ検索・詳細表示
- AI自然文スタジオ検索
- AIレビュー文補助
- 予約可否判定・料金計算
- Stripe Checkout・Webhook
- レビュー管理
- ホスト管理機能
- 管理者管理機能
- CSV・PDF出力
- 監査ログ・AI検索ログ
- Migration・Seed
- Swagger・Health Check

### 1.3 参照資料

| 資料 | 配置例 |
|---|---|
| 要件定義書 | `docs/design/requirements-definition.md` |
| 基本設計書 | `docs/design/basic-design.md` |
| 本詳細設計書 | `docs/design/detail-design.md` |
| アーキテクチャ資料 | `docs/ARCHITECTURE.md` |
| Backend README | `Backend/README.md` |
| Frontend README | `Frontend/README.md` |

### 1.4 設計方針

1. ControllerはHTTP入出力、認証・認可、ステータスコード変換を担当する。
2. Serviceは入力の正規化、業務検証、検索、集計、更新を担当する。
3. DTOはAPIの入出力と内部Entityを分離する。
4. Entityは永続化モデルとして使用する。
5. 外部API接続は専用ClientまたはServiceへ分離する。
6. 一覧取得は原則としてページングする。
7. 参照系クエリは原則として`AsNoTracking`を使用する。
8. 秘密情報は設定ファイルへ直書きせず、環境変数またはSecretsで管理する。

---

## 2. システム詳細構成

### 2.1 Backendレイヤー

```text
HTTP Request
    ↓
Controller
    ↓
Service / Interface
    ↓
DbContext / External Client
    ↓
MySQL / Stripe / OpenAI / SMTP
```

| 層 | 主な責務 |
|---|---|
| Controller | Routing、認証・認可、ModelState、HTTP応答 |
| Service | 業務ロジック、検証、検索、集計、更新 |
| Client | OpenAI等の外部HTTP通信 |
| DTO | Request/Responseデータ |
| Entity | DB永続化モデル |
| DbContext | テーブル、制約、関連、インデックス |
| Settings | 外部設定の型定義 |
| Seeder | デモデータ投入 |

### 2.2 主な外部サービス

| サービス | 用途 |
|---|---|
| Stripe | Checkout、決済、Webhook |
| OpenAI Responses API | 自然文検索条件解析、レビュー文補助 |
| SMTP / Mailtrap | 会員登録認証メール、パスワード再設定メール |
| QuestPDF | 売上明細PDF |
| MySQL | 永続データ |

### 2.3 DI登録

主なServiceはScopedで登録する。OpenAI Clientは`AddHttpClient`を使用する。

```text
IAuthService                 → AuthService
IJwtTokenService             → JwtTokenService
IPasswordHasherService       → PasswordHasherService
IVerificationTokenService    → VerificationTokenService
IEmailService                → EmailService
```

その他、公開、予約、レビュー、ホスト、管理者系ServiceもScopedとして登録する。

---

## 3. 共通方式設計

### 3.1 API形式

- URLプレフィックス：`/api`
- データ形式：JSON
- 文字コード：UTF-8
- 日時形式：ISO 8601
- 認証方式：JWT Bearer
- 一覧検索条件：Query String
- 登録・更新：JSON Body

### 3.2 HTTPステータス

| Status | 用途 |
|---:|---|
| 200 | 取得、更新、処理成功 |
| 201 | 新規登録成功 |
| 204 | 更新・削除成功、本文なし |
| 400 | 入力値または業務条件不正 |
| 401 | 未認証 |
| 403 | ロール・権限不足 |
| 404 | 対象なし |
| 409 | 重複・競合 |
| 429 | AI検索レート制限超過 |
| 500 | 外部APIまたは内部処理失敗 |

### 3.3 ページング

共通項目：

| 項目 | 説明 |
|---|---|
| page | 1始まり |
| pageSize | 1ページ件数 |
| totalCount | 全件数 |
| totalPages | 総ページ数 |
| items | 対象ページの一覧 |

`totalCount = 0`の場合、原則として`totalPages = 1`とする。

主な上限：

| 機能 | pageSize上限 |
|---|---:|
| 公開スタジオ | 50 |
| レビュー | 50 |
| 一般ユーザー予約 | 50 |
| AI検索ログ | 50 |
| 管理者一覧 | 100 |
| ホスト一覧 | 実装上、明示的上限なしの箇所あり |

### 3.4 入力検証

Controllerでは`[ApiController]`およびDataAnnotationsによる検証を利用する。Serviceでも業務上必要な検証を再実施する。

主なDataAnnotations：

| DTO | 制約 |
|---|---|
| SignupRequestDto.Password | 8文字以上 |
| SignupRequestDto.Email | Required、EmailAddress、255文字以内 |
| AdminRoomUpsertRequestDto.Name | Required、200文字以内 |
| AdminRoomUpsertRequestDto.Price | 1～1,000,000 |
| AdminRoomUpsertRequestDto.Capacity | 1～10,000 |
| CreateReviewRequest.Score | 1～5 |
| CreateReviewRequest.Content | Required、1000文字以内 |
| AiReviewAssistRequest.Content | Required、1000文字以内 |
| UpdateAdminSettingsRequestDto | 税率・手数料率0～100 |

### 3.5 日時

- DB登録日時は原則UTCとする。
-`CreatedAtUtc`、`UpdatedAtUtc`、`ExpiresAtUtc`等を使用する。
- 一部の予約判定、画面用カレンダー、Controllerの既定月では`DateTime.Now`または`DateTime.Today`を使用する。
- 公開スタジオ詳細のカレンダーイベントはUTC+9の`DateTimeOffset`として返す。

### 3.6 曜日コード

システム内に2種類の曜日コードが存在する。

| 対象 | コード |
|---|---|
| BusinessHour.DayOfWeek | 月=1、火=2、…、日=7 |
| PriceRule.Weekday | 日=0、月=1、…、土=6、null=全曜日 |

Service内で.NETの`DayOfWeek`からBusinessHour用コードへ変換する。

---

## 4. 認証・認可詳細設計

### 4.1 JWT生成

`JwtTokenService.GenerateToken`でJWTを生成する。

#### Claim

| Claim | 値 |
|---|---|
| sub | User.Id |
| email | User.Email |
| name | User.Name |
| role | Userに付与された各Role |

#### 署名

- アルゴリズム：HMAC SHA-256
- Issuer：`Jwt:Issuer`
- Audience：`Jwt:Audience`
- 署名鍵：`Jwt:SigningKey`
- 有効期限：現在UTC＋`Jwt:AccessTokenMinutes`

### 4.2 JWT検証

- Issuer検証：有効
- Audience検証：有効
- 署名鍵検証：有効
- 有効期限検証：有効
- ClockSkew：0

Bearerヘッダーを正式方式とする。Middlewareには`auth_token` Cookieからトークンを取得する処理も残っている。

### 4.3 ロール

| Role | 用途 |
|---|---|
| GeneralUser | 予約、予約履歴、レビュー |
| Host | 所有スタジオ・予約・売上・レビュー管理 |
| Admin | 全体管理、設定、監査・AIログ |

### 4.4 ログイン

**Endpoint**：`POST /api/auth/login`

処理：

1. ModelState検証。
2. EmailをTrimし小文字化。
3. User、UserRole、Roleを取得。
4. User未存在時は認証失敗。
5. `Enabled = false`の場合は403。
6. PasswordHasherで照合。
7. Role一覧を取得。
8. Roleに応じて遷移先を決定。
9. JWTを生成。
10. ログイン監査ログを記録。
11. User、Token、ExpiresAt、RedirectToを返す。

遷移先：

| 条件 | RedirectTo |
|---|---|
| Adminを含む | `/admin` |
| Hostを含む | `/host` |
| その他 | `/` |

### 4.5 ログアウト

**Endpoint**：`POST /api/auth/logout`

- 認証必須。
- `auth_token` Cookieを削除する。
- Bearerトークン自体のサーバー失効処理は行わない。
- クライアント側で保存トークンを削除することを前提とする。

### 4.6 ログインユーザー情報

- `GET /api/auth/me`
- `PUT /api/auth/me`

ユーザーIDは`sub`または`NameIdentifier`から取得する。

更新時は、自分以外のUserとEmailが重複していないことを確認する。

### 4.7 会員登録

**Endpoint**：`POST /api/auth/signup`

処理：

1. ModelState検証。
2. EmailをTrim・小文字化。
3. Email重複確認。
4. PasswordとPasswordConfirmation一致確認。
5. `GeneralUser` Role取得。
6. Userを`Enabled = false`で登録。
7. UserRole登録。
8. 24時間有効のVerificationTokenを登録。
9. 認証URLを作成。
10. SMTPでメール送信。

### 4.8 メール認証

**Endpoint**：`GET /api/auth/verify?token=...`

有効条件：

- Token一致
- `UsedAtUtc == null`
- `ExpiresAtUtc > 現在UTC`

成功時：

- User.Enabled = true
- User.EmailVerifiedAtUtc = 現在UTC
- Token.UsedAtUtc = 現在UTC
- Frontendの認証完了画面へRedirect

### 4.9 パスワード再設定

#### 再設定申請

**Endpoint**：`POST /api/auth/forgot-password`

1. EmailをTrim。
2. 有効ユーザーを検索。
3. Userが存在しない場合も正常終了する。
4. 既存未使用・未期限切れTokenを使用済みにする。
5. 暗号学的乱数32バイトからTokenを発行。
6. 有効期限を1時間に設定。
7. Frontend URLを作成してメール送信。

#### パスワード更新

**Endpoint**：`POST /api/auth/reset-password`

1. ModelState検証。
2. NewPasswordとConfirmPassword一致確認。
3. 未使用・期限内Tokenを取得。
4. User.Enabled確認。
5. 新しいPasswordHashを保存。
6. Tokenを使用済みにする。

### 4.10 パスワードハッシュ

ASP.NET Core Identityの`PasswordHasher<User>`を使用する。

照合結果が以下の場合に成功とする。

- Success
- SuccessRehashNeeded

---

## 5. 公開・スタジオ機能詳細設計

### 5.1 トップページ

**Endpoint**：`GET /api/home`

#### 人気スタジオ

1. 公開レビュー数の降順。
2. 平均評価の降順。
3. Room.Idの昇順。
4. 上位3件。

#### 新着スタジオ

- Room.Id降順で上位4件。

### 5.2 スタジオ一覧

**Endpoint**：`GET /api/rooms`

検索条件：

| Query | 内容 |
|---|---|
| keyword | Name、Description、PostalCode、Addressの部分一致 |
| area | Addressの部分一致 |
| price | Price以下 |
| order | `priceAsc`または作成日降順 |
| page | ページ |
| pageSize | 最大50 |

公開レビューのみを平均評価・件数の対象とする。

### 5.3 スタジオ詳細

**Endpoint**：`GET /api/rooms/{roomId}`

取得内容：

- Room基本情報
- Host名
- 営業時間
- 料金ルール
- 公開レビュー直近5件
- 非公開レビューのうちホスト返信あり直近5件
- 公開レビュー平均・件数
- カレンダーイベント

### 5.4 カレンダーイベント

対象期間：

- 今日の7日前から60日後

イベント種別：

| Type | 内容 |
|---|---|
| open | 営業時間 |
| closure | 定休日・臨時休館 |
| reservation | キャンセル以外の予約 |

---

## 6. AI機能詳細設計

### 6.1 AIスタジオ検索

**Endpoint**：`POST /api/ai/room-search`

- 未認証利用可。
- IP単位のレート制限を適用。
- QueryはTrim後、空文字不可、200文字以内。

処理：

1. OpenAIへ自然文を送信。
2. JSON Schema形式で検索条件を取得。
3. Area、Price、Capacity、Keyword等をRoom検索へ反映。
4. 公開レビュー平均の降順、料金昇順、ID降順で最大5件取得。
5. 各Roomへ候補理由を付与。
6. 成功または失敗をAiSearchLogsへ記録。

抽出条件：

- Keyword
- Area
- Price
- Capacity
- CapacityCondition
- Purpose
- Atmosphere
- TimePreference
- Keywords

### 6.2 レート制限

| 項目 | 値 |
|---|---|
| Policy | AiSearchPolicy |
| Partition | IPアドレス |
| 方式 | Fixed Window |
| 上限 | 1分5回 |
| QueueLimit | 0 |
| 超過応答 | 429 |

429時もAI検索ログへ失敗として記録する。

### 6.3 AIレビュー文補助

**Endpoint**：`POST /api/ai/review-assist`

- 認証必須。
- 本文1000文字以内。
- 評価1～5。
- OpenAIの出力はレビュー本文のみ。
- 意味を変更せず、事実を追加せず、敬体へ整形する。
- 空の応答はエラーとする。

### 6.4 OpenAI通信

- Endpoint：`https://api.openai.com/v1/responses`
- Authorization：Bearer API Key
- Model：`OpenAI:Model`
- 自然文検索はStructured OutputsのJSON Schemaを使用する。

---

## 7. 予約・料金計算詳細設計

### 7.1 予約確認

**Endpoint**：`POST /api/reservations/confirm`

- 認証必須。
- `GeneralUser`限定。

入力：

- RoomId
- StartAt
- EndAt

処理順：

1. StartAt < EndAt確認。
2. StartAtが現在時刻より後であることを確認。
3. RoomとBusinessHours、Closures、PriceRulesを取得。
4. 同日内であることを確認。
5. 営業時間内であることを確認。
6. 休館期間と重複しないことを確認。
7. 既存の非キャンセル予約と重複しないことを確認。
8. 料金明細を計算。
9. 税率・手数料率を取得。
10. 合計金額を計算。
11. Stripe Checkout Sessionを作成。
12. SessionId、CheckoutUrl等を返す。

### 7.2 予約重複条件

```text
RoomIdが一致
AND Status != canceled
AND 既存StartAt < 新規EndAt
AND 新規StartAt < 既存EndAt
```

### 7.3 営業時間

予約日の曜日に対応するBusinessHourを取得する。

エラー条件：

- BusinessHourなし
- IsHoliday = true
- StartTimeまたはEndTime未設定
- 予約開始が営業時間より前
- 予約終了が営業時間より後

### 7.4 休館期間

以下の条件で重複とする。

```text
Closure.StartAt < Reservation.EndAt
AND Reservation.StartAt < Closure.EndAt
```

### 7.5 料金明細

StartAtからEndAtまでを最大1時間単位に分割する。

各区間：

```text
適用単価 × 区間時間
```

端数は`MidpointRounding.AwayFromZero`で円単位へ丸める。

連続する同一単価・同一Labelの明細は結合する。

### 7.6 倍率ルール

対象条件：

- RuleType = multiplier
- Multiplier != null
- Weekdayがnullまたは対象曜日
- StartHourがnullまたは対象時刻以上
- EndHourがnullまたは対象時刻未満

複数候補時：

1. 曜日指定ありを優先。
2. 開始時刻指定ありを優先。

```text
適用単価 = Room.Price × Multiplier
```

### 7.7 税・プラットフォーム手数料

AppSettingsから取得する。

| Key | 用途 | 既定値 |
|---|---|---:|
| tax_rate | 税率 | 0.10 |
| admin_fee_rate | 手数料率 | 0.15 |

```text
税額 = Round(小計 × 税率)
手数料 = Round(小計 × 手数料率)
合計 = 小計 + 税額 + 手数料
```

### 7.8 Stripe Checkout

- Mode：payment
- Currency：JPY
- Quantity：1
- UnitAmount：予約合計金額
- PaymentMethodTypes：card
- SuccessUrl：設定値
- CancelUrl：`{ROOM_ID}`を置換

PaymentIntent metadata：

- userId
- roomId
- startAt
- endAt
- subtotal
- taxRatePercent
- tax
- platformFeeRatePercent
- platformFee
- amount
- hourlyPrice
- hours

### 7.9 Stripe Webhook

**Endpoint**：`POST /api/stripe/webhook`

処理：

1. Request Bodyを取得。
2. `Stripe-Signature`とWebhookSecretで署名検証。
3. `checkout.session.completed`のみ処理。
4. SessionをPaymentIntent付きで再取得。
5. PaymentIntent metadataを取得。
6. 予約完了Serviceを呼び出す。

署名不正・解析失敗は400、処理失敗は500。

### 7.10 予約確定

1. metadata必須値を取得。
2. Stripe支払金額とmetadata amountを比較。
3. 同一Room/User/時間/金額の非キャンセル予約が存在する場合は終了。
4. 予約条件・料金を再計算。
5. metadata金額と再計算金額を比較。
6. Status=`paid`でReservation登録。
7. 料金明細、税、手数料をReservationChargeItemsへ登録。

### 7.11 予約履歴

**Endpoint**：`GET /api/reservations`

- 認証必須。
- `GeneralUser`限定。
- 自分の予約のみ。
- StartAt降順、Id降順。
- pageSize最大50。

---

## 8. レビュー詳細設計

### 8.1 レビュー画面用取得

**Endpoint**：`GET /api/rooms/{roomId}/reviews/new`

取得内容：

- Room情報
- 公開レビュー一覧
- 平均評価
- 投稿済み判定
- 投稿可能判定
- ページング情報

### 8.2 投稿条件

- 認証済み。
- Scoreは1～5。
- Content必須、1000文字以内。
- Roomが存在する。
- 同一User・Roomのレビューが未投稿。
- 対象Roomに`paid`予約が存在する。
- ReservationId指定時はその予約が条件を満たす。

### 8.3 登録内容

- PublicVisible = true
- HiddenReason = null
- HostReply = null
- HostReplyAt = null
- CreatedAtUtc / UpdatedAtUtc = 現在UTC

---

## 9. ホスト機能詳細設計

### 9.1 共通権限

- `[Authorize(Roles = "Host")]`
- Room.UserIdとログインユーザーIDを照合する。
- 他ホストのデータは取得・更新できない。

### 9.2 所有スタジオ

- `GET /api/host/rooms`
- `GET /api/host/rooms/{id}`

一覧はName部分一致、ページング対応。詳細は所有Roomのみ取得する。

### 9.3 営業時間

- `GET /api/host/rooms/{roomId}/business-hours`
- `PUT /api/host/rooms/{roomId}/business-hours`

更新条件：

1. 7曜日すべてを指定。
2. DayOfWeekが1～7。
3. 営業日のStartTime、EndTime必須。
4. 15分単位。
5. EndTime > StartTime。
6. 休業日は時刻をnullにする。

既存行は更新、存在しない曜日は追加する。

### 9.4 休館期間

- 一覧
- カレンダーイベント
- 登録
- 削除

登録時は`EndAt > StartAt`を確認する。

### 9.5 料金ルール

- `multiplier`
- `flat_fee`

#### multiplier

- Multiplier必須、0より大きい。
- FlatFeeは指定不可。
- StartHour、EndHour必須。
- 15分単位。
- EndHour > StartHour。

#### flat_fee

- FlatFee必須、0以上。
- 時刻・Multiplier指定不可。
- 同一曜日のflat_fee重複不可。

### 9.6 予約管理

**一覧条件**

- keyword：Room名またはGuest名
- status
- reservationId
- roomId
- startFrom
- startTo
- page / pageSize

**承認**

`booked`の場合、`paid`へ変更する。

**キャンセル**

`booked`の場合、`canceled`へ変更する。

承認・キャンセル後に監査ログを記録する。

### 9.7 レビュー管理

- Room、星数、非公開のみで検索。
- ホスト返信保存。
- 公開・非公開切替。
- 非公開時の理由保存。

### 9.8 売上一覧・明細

一覧は所有RoomのReservationを対象とする。

`onlyWithItems = true`の場合、Amount > 0を対象とする。

詳細ではReservationChargeItemsを取得する。

### 9.9 CSV

#### 売上一覧CSV

`GET /api/host/sales.csv`

- UTF-8 BOM付き。
- 全項目をダブルクォートで囲む。
- ダブルクォートは二重化する。

#### 売上明細CSV

`GET /api/host/sales/{reservationId}/items.csv`

- 基本情報行
- 料金明細行
- UTF-8 BOM付き

### 9.10 PDF

`GET /api/host/sales/{reservationId}/invoice.pdf`

- A4
- QuestPDF
- NotoSansJP
- 予約情報、料金明細、合計を出力
- Content-Type：`application/pdf`

### 9.11 ホスト統計

**Endpoint**：`GET /api/host/status`

対象：

- ログインホスト所有Room
- RoomId指定可
- 基準月を含む直近3か月

算出値：

- booked金額
- paid金額
- 稼働率
- 全レビュー平均
- 公開レビュー平均
- Room選択肢

```text
稼働率 = 予約時間 / 営業可能時間 × 100
```

100%を超える場合は100%へ丸める。

---

## 10. 管理者機能詳細設計

### 10.1 共通権限

すべて`[Authorize(Roles = "Admin")]`を使用する。

### 10.2 ユーザー一覧・詳細

- `GET /api/admin/users`
- `GET /api/admin/users/{userId}`

検索対象：

- Name
- Kana
- Email

主ロール優先順位：

1. Admin
2. Host
3. GeneralUser

表示ラベル：

| Role | Label |
|---|---|
| Admin | 管理者 |
| Host | スタジオ提供者 |
| GeneralUser | 会員 |
| その他 | 未設定 |

### 10.3 スタジオ管理

- 一覧
- Host選択肢
- 詳細
- 登録
- 更新

登録・更新時：

1. 対象Userが有効なHostであることを確認。
2. Name、AddressをTrim。
3. Name＋Addressの重複確認。
4. 登録・更新。
5. 監査ログ記録。

### 10.4 予約一覧

検索条件：

- keyword：Room名、Host名、Guest名
- status
- reservationId
- roomId
- startFrom
- startTo
- page / pageSize

### 10.5 管理設定

- `GET /api/admin/settings`
- `PUT /api/admin/settings`

DB値は小数率で保存し、APIでは百分率で返す。

例：

```text
DB: 0.10
API: 10.00
```

更新後は監査ログを記録する。

### 10.6 管理者統計

基準月を含む直近3か月を対象とする。

- booked・paid：`platform_fee`明細額を集計
- 稼働率：予約時間÷営業時間
- レビュー平均
- 全Room数
- Host数
- 全Reservation数
- paidのplatform_fee合計

Roomが0件の場合のTotalPaidAmountのみReservation.Amount合計を使用しており、通常経路との差異がある。

### 10.7 監査ログ

**Endpoint**：`GET /api/admin/logs`

検索条件：

- q：Action、Entity、Note
- actorId
- action
- entity
- entityId
- from
- to
- page / pageSize

並び順：

- Ts降順
- Id降順

### 10.8 AI検索ログ

**Endpoint**：`GET /api/admin/ai-search-logs`

検索条件：

- q：QueryまたはErrorMessage
- userId
- ipAddress
- succeeded
- model
- from
- to
- page / pageSize

---

## 11. API一覧

### 11.1 認証・公開

| Method | Path | Auth | 概要 |
|---|---|---|---|
| POST | `/api/auth/login` | 不要 | ログイン |
| GET | `/api/auth/me` | 必要 | ログイン情報 |
| PUT | `/api/auth/me` | 必要 | 会員情報更新 |
| POST | `/api/auth/logout` | 必要 | ログアウト |
| POST | `/api/auth/signup` | 不要 | 会員登録 |
| GET | `/api/auth/verify` | 不要 | メール認証 |
| POST | `/api/auth/forgot-password` | 不要 | 再設定申請 |
| POST | `/api/auth/reset-password` | 不要 | パスワード更新 |
| GET | `/api/home` | 不要 | トップ |
| GET | `/api/rooms` | 不要 | スタジオ一覧 |
| GET | `/api/rooms/{roomId}` | 不要 | スタジオ詳細 |

### 11.2 AI・予約・レビュー

| Method | Path | Auth/Role | 概要 |
|---|---|---|---|
| POST | `/api/ai/room-search` | 不要 | AI検索 |
| POST | `/api/ai/review-assist` | 必要 | AIレビュー補助 |
| POST | `/api/reservations/confirm` | GeneralUser | 予約確認・Checkout |
| GET | `/api/reservations` | GeneralUser | 自分の予約 |
| POST | `/api/stripe/webhook` | Stripe署名 | Webhook |
| GET | `/api/rooms/{roomId}/reviews/new` | 必要 | レビュー画面情報 |
| POST | `/api/rooms/{roomId}/reviews` | 必要 | レビュー登録 |

### 11.3 ホスト

| Method | Path | 概要 |
|---|---|---|
| GET | `/api/host/rooms` | 所有Room一覧 |
| GET | `/api/host/rooms/{id}` | 所有Room詳細 |
| GET/PUT | `/api/host/rooms/{roomId}/business-hours` | 営業時間 |
| GET/POST | `/api/host/rooms/{roomId}/closures` | 休館期間 |
| GET | `/api/host/rooms/{roomId}/closures/events` | 休館イベント |
| DELETE | `/api/host/rooms/{roomId}/closures/{closureId}` | 休館削除 |
| GET/POST | `/api/host/rooms/{roomId}/price-rules` | 料金ルール |
| DELETE | `/api/host/rooms/{roomId}/price-rules/{ruleId}` | 料金ルール削除 |
| GET | `/api/host/reservations` | 予約一覧 |
| POST | `/api/host/reservations/{id}/approve` | 予約承認 |
| POST | `/api/host/reservations/{id}/cancel` | 予約取消 |
| GET | `/api/host/reviews` | レビュー一覧 |
| POST | `/api/host/reviews/{id}/reply` | 返信 |
| POST | `/api/host/reviews/{id}/visibility` | 公開状態 |
| GET | `/api/host/sales` | 売上一覧 |
| GET | `/api/host/sales/{id}` | 売上明細 |
| GET | `/api/host/sales.csv` | 一覧CSV |
| GET | `/api/host/sales/{id}/items.csv` | 明細CSV |
| GET | `/api/host/sales/{id}/invoice.pdf` | PDF |
| GET | `/api/host/status` | 統計 |

### 11.4 管理者

| Method | Path | 概要 |
|---|---|---|
| GET | `/api/admin/users` | ユーザー一覧 |
| GET | `/api/admin/users/{id}` | ユーザー詳細 |
| GET | `/api/admin/rooms` | Room一覧 |
| GET | `/api/admin/rooms/host-options` | Host選択肢 |
| GET | `/api/admin/rooms/{id}` | Room詳細 |
| POST | `/api/admin/rooms` | Room登録 |
| PUT | `/api/admin/rooms/{id}` | Room更新 |
| GET | `/api/admin/reservations` | 予約一覧 |
| GET/PUT | `/api/admin/settings` | 管理設定 |
| GET | `/api/admin/status` | 統計 |
| GET | `/api/admin/logs` | 監査ログ |
| GET | `/api/admin/ai-search-logs` | AI検索ログ |

---

## 12. データベース詳細設計

### 12.1 テーブル一覧

| テーブル | 用途 |
|---|---|
| Users | ユーザー |
| Roles | ロール |
| UserRoles | ユーザー・ロール中間 |
| VerificationTokens | メール認証 |
| PasswordResetTokens | パスワード再設定 |
| Rooms | スタジオ |
| BusinessHours | 営業時間 |
| Closures | 臨時休館 |
| PriceRules | 料金ルール |
| Reservations | 予約 |
| ReservationChargeItems | 予約料金明細 |
| Reviews | レビュー |
| AppSettings | 税率・手数料 |
| AuditLogs | 監査ログ |
| AiSearchLogs | AI検索ログ |

### 12.2 Users

| Column | Type/Length | Null | 備考 |
|---|---|---|---|
| Id | int | No | PK |
| Name | varchar(100) | No | |
| Kana | varchar(100) | No | |
| Email | varchar(255) | No | Unique |
| PasswordHash | varchar(500) | No | |
| PostalCode | varchar(20) | No | |
| Address | varchar(300) | No | |
| PhoneNumber | varchar(30) | No | |
| UsageType | varchar(50) | No | |
| Enabled | bool | No | |
| EmailVerifiedAtUtc | datetime | Yes | |

### 12.3 Roles / UserRoles

Roles.Nameは50文字以内・一意。

UserRolesは`UserId + RoleId`を一意とし、UserまたはRole削除時はCascade。

### 12.4 Tokens

VerificationTokens、PasswordResetTokensともTokenは200文字以内・一意。

User削除時はCascade。

### 12.5 Rooms

| Column | 制約 |
|---|---|
| Name | 200文字、必須 |
| ImageName | 255文字、任意 |
| Description | 2000文字、任意 |
| Price | 必須 |
| Capacity | 必須 |
| PostalCode | 20文字、必須 |
| Address | 300文字、必須 |

`Name + Address`を一意とする。User削除時はCascade。

### 12.6 BusinessHours

- `RoomId + DayOfWeek`を一意。
- StartTime、EndTimeはMySQLの`time`。
- Room削除時はCascade。

### 12.7 PriceRules

| Column | 制約 |
|---|---|
| RuleType | 50文字、必須 |
| Weekday | 任意 |
| StartHour / EndHour | time、任意 |
| Multiplier | decimal(5,2)、任意 |
| FlatFee | int、任意 |
| Note | 500文字、任意 |

`RoomId + RuleType + Weekday`に非一意インデックス。

### 12.8 Reservations

- Room削除時：Restrict
- User削除時：Restrict
- RoomId、UserId、Status、StartAtにインデックス
- Status最大50文字

### 12.9 ReservationChargeItems

- Reservation削除時：Cascade
- ReservationId、SliceStartにインデックス
- Kind最大50文字
- Description最大500文字

### 12.10 Reviews

- Room削除時：Restrict
- User削除時：Restrict
- RoomId、UserId、Score、PublicVisible、CreatedAtUtcにインデックス
- Content最大2000文字
- HiddenReason最大500文字
- HostReply最大2000文字

### 12.11 AppSettings

- Key最大100文字、一意
- Value最大100文字

### 12.12 AuditLogs

- Action、Entity最大100文字
- Note最大2000文字
- Ts、ActorId、Action、Entity、EntityIdにインデックス

### 12.13 AiSearchLogs

- Query最大500文字
- IpAddress最大100文字
- Model最大100文字
- ErrorMessage最大1000文字
- CreatedAtUtc、IpAddress、UserId、Succeededにインデックス

---

## 13. ログ設計

### 13.1 監査ログ

保存項目：

- Ts
- ActorId
- Action
- Entity
- EntityId
- Note

主なAction：

- LOGIN
- CREATE
- UPDATE
- SETTING_UPDATE
- APPROVE
- CANCEL

監査ログ保存失敗はWarningを出力し、本処理は継続する。

### 13.2 AI検索ログ

成功・入力エラー・OpenAIエラー・RateLimit超過を記録する。

Query、IP、UserId、Model、成功可否、結果件数、エラーを保存する。

### 13.3 Stripeログ

- Webhook Body長
- Signature有無
- WebhookSecret長
- EventType
- EventId
- 署名失敗
- 予約完了処理失敗

秘密値そのものはログへ出力しない。

---

## 14. 設定・環境変数

### 14.1 主な設定

| Key | 用途 |
|---|---|
| ConnectionStrings:DefaultConnection | MySQL接続 |
| Jwt:Issuer | JWT Issuer |
| Jwt:Audience | JWT Audience |
| Jwt:SigningKey | JWT署名鍵 |
| Jwt:AccessTokenMinutes | 有効期限 |
| Cors:AllowedOrigins | Frontend Origin |
| Frontend:BaseUrl | メールURL |
| Stripe:SecretKey | Stripe秘密鍵 |
| Stripe:PublishableKey | 公開鍵 |
| Stripe:WebhookSecret | Webhook署名 |
| Stripe:SuccessUrl | 成功URL |
| Stripe:CancelUrl | キャンセルURL |
| OpenAI:ApiKey | OpenAI APIキー |
| OpenAI:Model | モデル |
| Mailtrap:Host | SMTP Host |
| Mailtrap:Port | SMTP Port |
| Mailtrap:UserName | SMTP User |
| Mailtrap:Password | SMTP Password |
| Mailtrap:From | From |
| ENABLE_SWAGGER | Swagger公開 |
| ENABLE_DB_MIGRATION | 起動時Migration |
| ENABLE_DB_SEED | 起動時Seed |

### 14.2 秘密情報

以下はリポジトリへコミットしない。

- DB Password
- Jwt SigningKey
- Stripe SecretKey
- Stripe WebhookSecret
- OpenAI ApiKey
- SMTP UserName / Password

---

## 15. 起動・運用設計

### 15.1 Middleware順序

```text
HTTPS Redirection
CORS
Authentication
RateLimiter
Authorization
Controller
```

### 15.2 Swagger

Development環境または`ENABLE_SWAGGER = true`で有効化する。

### 15.3 Health Check

`GET /health`

### 15.4 Migration・Seed

起動時に以下の設定で制御する。

- `ENABLE_DB_MIGRATION`
- `ENABLE_DB_SEED`

SeederはRole、User、Room、BusinessHour、PriceRule、Reservation、ChargeItem、Review、AuditLogを投入する。

Role、UserまたはUserRoleが既に存在する場合はSeed全体をスキップする。

---

## 16. エラー処理

### 16.1 主な業務エラー

| Code/例外 | HTTP | 内容 |
|---|---:|---|
| VALIDATION_ERROR | 400 | 入力不正 |
| AUTH_INVALID_CREDENTIALS | 401 | 認証失敗 |
| AUTH_USER_DISABLED | 403 | 無効User |
| EMAIL_ALREADY_REGISTERED | 409 | Email重複 |
| PASSWORD_MISMATCH | 400 | Password不一致 |
| HOST_NOT_FOUND | 400 | 有効Hostなし |
| ROOM_DUPLICATED | 409 | Room名＋住所重複 |
| KeyNotFoundException | 404 | 対象なし |
| ArgumentException | 400 | 入力・形式不正 |
| InvalidOperationException | 400/500 | 業務条件または外部処理失敗 |

### 16.2 例外応答の現状

エラーDTOは一部で`ApiErrorResponseDto`を使用するが、匿名型`{ message }`を返すControllerもある。現状仕様として記録し、将来的な統一対象とする。

---

## 17. 既知の課題・要確認事項

### 17.1 BearerとCookieの混在

正式なログイン方式はBearerだが、JWT Middlewareは`auth_token` Cookieも読み、LogoutもCookie削除を行う。正式方式へ統一する必要がある。

### 17.2 Claim取得方法のばらつき

Controllerごとに`NameIdentifier`、`sub`、`userId`、`id`の参照方法が異なる。共通ヘルパーまたはUserContext Serviceへの統一を推奨する。

### 17.3 flat_fee未適用

HostPriceRuleServiceでは`flat_fee`を登録できるが、ReservationConfirmServiceの料金計算は`multiplier`のみを適用する。固定費を正式仕様とする場合は計算処理の追加が必要である。

### 17.4 予約状態

Stripe決済完了時は直接`paid`で登録する一方、HostのApproveも`booked → paid`を行う。`booked`、`paid`、利用完了の意味を整理する必要がある。

### 17.5 Webhook冪等性

PaymentIntentIdまたはStripe EventIdをDBへ保存していない。現在はRoom、User、日時、金額で重複判定しているため、専用IDの一意保存を推奨する。

### 17.6 AdminStatusの集計差異

Roomが存在する通常経路ではplatform_feeを集計するが、対象Roomなしの経路ではReservation.Amountを集計する。TotalPaidAmountの定義を統一する必要がある。

### 17.7 Reviewの一意性

Serviceでは同一User・Roomにつき1レビューに制限するが、DB一意制約は存在しない。同時実行対策として複合一意制約を検討する。

### 17.8 予約重複の同時実行

Serviceの事前検索のみで重複を防いでいる。高負荷・商用運用ではトランザクション、ロック、予約枠テーブル等を検討する。

### 17.9 日時・タイムゾーン

UTC保存とローカル時刻判定が混在する。API境界、DB、Frontendでの日時基準を明確化する必要がある。

### 17.10 Controller内DBアクセス

HostRooms、CSV/PDF出力、AuthControllerの一部でControllerからDbContextを直接利用している。保守性向上のためServiceへ移す余地がある。

### 17.11 Seederと本実装の差異

Seederの手数料率は10%固定だが、AdminSettingsServiceの既定手数料率は15%である。デモデータと実計算の差異を許容するか整理する。

---

## 18. テスト観点

### 18.1 認証

- 正常ログイン
- Password不一致
- 無効User
- JWT期限切れ
- Role別認可
- Email重複
- Token期限切れ・再利用

### 18.2 予約

- 同日内
- 過去日時
- 営業時間外
- 休館期間重複
- 予約重複
- 通常料金
- 倍率料金
- 税・手数料
- Stripe金額不一致
- Webhook再送

### 18.3 ホスト権限

- 所有Roomの取得・更新
- 他Host Roomへのアクセス拒否
- 予約承認・取消
- CSV・PDFの所有権

### 18.4 管理者

- Admin以外の拒否
- Room重複
- 無効Host指定
- 統計集計
- 監査ログ検索
- AIログ検索

### 18.5 AI

- 空文字
- 文字数超過
- API Keyなし
- OpenAIエラー
- JSON解析
- RateLimit 429
- 成功・失敗ログ

---

## 19. 変更管理

本書と実装に差異が発生した場合は、以下の優先順位で整合を取る。

1. 確定した業務要件
2. 要件定義書
3. 基本設計書
4. 詳細設計書
5. 実装

ただし、現行動作を記録する目的では実装を根拠とし、上位設計との差異を「既知の課題」として明示する。
