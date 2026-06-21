# Studio Book DB要件書

## 1. 文書概要

本書は、Studio Book の `AppDbContext`、Entity定義、および `AppDbContextModelSnapshot` を基に、データベース構成、テーブル、カラム、制約、インデックス、リレーション、削除ルールおよび業務上のデータ要件を整理したものである。

要件定義書、基本設計書、詳細設計書、ER図、テーブル定義書を作成する際の基礎資料として使用する。

---

## 2. データベース概要

| 項目 | 内容 |
|---|---|
| DBMS | MySQL系 |
| ORM | Entity Framework Core 9.0.1 |
| Provider特性 | MySQL AUTO_INCREMENT、`tinyint(1)`、`datetime(6)`、`time` |
| 主キー型 | `int` |
| 採番方式 | AUTO_INCREMENT |
| 日時型 | 原則 `datetime(6)` |
| 時刻型 | `time` |
| 真偽値 | `tinyint(1)` |
| 金額型 | 原則 `int` |
| 倍率型 | `decimal(5,2)` |
| 最大識別子長 | 64文字 |
| テーブル数 | 15 |

---

## 3. 基本方針

### 3.1 主キー

全テーブルは `Id` を主キーとし、`int` のAUTO_INCREMENTで採番する。

### 3.2 日時管理

原則として日時項目はUTCで管理する。

主に以下の命名規則を使用する。

- `CreatedAtUtc`
- `UpdatedAtUtc`
- `ExpiresAtUtc`
- `UsedAtUtc`
- `EmailVerifiedAtUtc`
- `HostReplyAt`
- `Ts`

ただし、`Closures` の `StartAt`、`EndAt`、`CreatedAt` は名称に `Utc` を含まないため、実際の保存基準をアプリケーション仕様として統一する必要がある。

### 3.3 金額管理

日本円を前提とし、小数を使用せず `int` で管理する。

対象例：

- スタジオ基本料金
- 予約金額
- 固定費
- 予約料金明細金額
- 時間単価

料金倍率のみ `decimal(5,2)` とする。

### 3.4 論理無効化

ユーザーは `Users.Enabled` により有効・無効を管理する。

予約やレビューが存在するユーザーは外部キー制約により物理削除できない可能性があるため、原則として物理削除ではなく論理無効化を使用する。

### 3.5 監査・検索ログ

`AuditLogs.ActorId`、`AuditLogs.EntityId`、`AiSearchLogs.UserId` には外部キー制約を設定しない。

これは、元データ削除後もログを保持できるようにするためである。

---

# 4. テーブル一覧

| No. | テーブル名 | 分類 | 主な用途 |
|---:|---|---|---|
| 1 | Users | 会員・認証 | 一般ユーザー、ホスト、管理者の共通ユーザー情報 |
| 2 | Roles | 権限 | ロールマスタ |
| 3 | UserRoles | 権限 | ユーザーとロールの関連 |
| 4 | VerificationTokens | 認証 | メール認証トークン |
| 5 | PasswordResetTokens | 認証 | パスワード再設定トークン |
| 6 | Rooms | スタジオ | スタジオ基本情報 |
| 7 | BusinessHours | スタジオ | 曜日別営業時間 |
| 8 | Closures | スタジオ | 臨時休館・利用不可期間 |
| 9 | PriceRules | 料金 | 曜日・時間帯別料金ルール |
| 10 | Reservations | 予約 | 予約情報と予約状態 |
| 11 | ReservationChargeItems | 売上 | 予約料金の内訳 |
| 12 | Reviews | レビュー | 評価、コメント、公開状態、ホスト返信 |
| 13 | AppSettings | 設定 | 税率、管理手数料などのアプリ設定 |
| 14 | AuditLogs | ログ | 管理操作、ログインなどの監査ログ |
| 15 | AiSearchLogs | ログ | AIスタジオ検索の実行履歴 |

---

# 5. リレーション概要

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

| 親 | 子 | 多重度 |
|---|---|---|
| Users | UserRoles | 1対多 |
| Roles | UserRoles | 1対多 |
| Users | VerificationTokens | 1対多 |
| Users | PasswordResetTokens | 1対多 |
| Users | Rooms | 1対多 |
| Users | Reservations | 1対多 |
| Users | Reviews | 1対多 |
| Rooms | BusinessHours | 1対多 |
| Rooms | Closures | 1対多 |
| Rooms | PriceRules | 1対多 |
| Rooms | Reservations | 1対多 |
| Rooms | Reviews | 1対多 |
| Reservations | ReservationChargeItems | 1対多 |

---

# 6. テーブル定義

## 6.1 Users

ユーザーの基本情報を管理する。一般ユーザー、ホスト、管理者は同一テーブルで管理し、ロールは `UserRoles` 経由で関連付ける。

| カラム | DB型 | NULL | 制約・説明 |
|---|---|---:|---|
| Id | int | 不可 | PK、AUTO_INCREMENT |
| Name | varchar(100) | 不可 | 氏名 |
| Kana | varchar(100) | 不可 | フリガナ |
| Email | varchar(255) | 不可 | 一意 |
| PasswordHash | varchar(500) | 不可 | ハッシュ化済みパスワード |
| PostalCode | varchar(20) | 不可 | 郵便番号 |
| Address | varchar(300) | 不可 | 住所 |
| PhoneNumber | varchar(30) | 不可 | 電話番号 |
| UsageType | varchar(50) | 不可 | 利用区分 |
| Enabled | tinyint(1) | 不可 | 有効・無効 |
| EmailVerifiedAtUtc | datetime(6) | 可 | メール認証日時 |

### インデックス

- `UNIQUE (Email)`

### 業務要件

- メールアドレスはユーザー間で重複不可とする。
- 無効ユーザーはログイン不可とする。
- 利用区分は原則 `personal` または `business` とする。
- パスワードは平文保存せず、ハッシュ化して保存する。

---

## 6.2 Roles

ロールマスタを管理する。

| カラム | DB型 | NULL | 制約・説明 |
|---|---|---:|---|
| Id | int | 不可 | PK、AUTO_INCREMENT |
| Name | varchar(50) | 不可 | ロール名、一意 |

### インデックス

- `UNIQUE (Name)`

### 想定値

- `Admin`
- `Host`
- `GeneralUser`

---

## 6.3 UserRoles

ユーザーとロールの多対多関連を管理する。

| カラム | DB型 | NULL | 制約・説明 |
|---|---|---:|---|
| Id | int | 不可 | PK、AUTO_INCREMENT |
| UserId | int | 不可 | FK → Users.Id |
| RoleId | int | 不可 | FK → Roles.Id |

### インデックス

- `INDEX (RoleId)`
- `UNIQUE (UserId, RoleId)`

### 削除ルール

- Users削除時：Cascade
- Roles削除時：Cascade

### 業務要件

- 同一ユーザーへ同一ロールを重複登録しない。
- 複数ロールを許可する構造である。
- アプリケーション上のロール優先順位は Admin、Host、GeneralUser の順とする。

---

## 6.4 VerificationTokens

メール認証用トークンを管理する。

| カラム | DB型 | NULL | 制約・説明 |
|---|---|---:|---|
| Id | int | 不可 | PK、AUTO_INCREMENT |
| UserId | int | 不可 | FK → Users.Id |
| Token | varchar(200) | 不可 | 一意 |
| ExpiresAtUtc | datetime(6) | 不可 | 有効期限 |
| UsedAtUtc | datetime(6) | 可 | 使用日時 |
| CreatedAtUtc | datetime(6) | 不可 | 作成日時 |

### インデックス

- `UNIQUE (Token)`
- `INDEX (UserId)`

### 削除ルール

- Users削除時：Cascade

### 業務要件

- トークンは有効期限内かつ未使用の場合のみ有効とする。
- 使用後は `UsedAtUtc` を記録する。
- 同一トークンの再利用を禁止する。

---

## 6.5 PasswordResetTokens

パスワード再設定用トークンを管理する。

| カラム | DB型 | NULL | 制約・説明 |
|---|---|---:|---|
| Id | int | 不可 | PK、AUTO_INCREMENT |
| UserId | int | 不可 | FK → Users.Id |
| Token | varchar(200) | 不可 | 一意 |
| ExpiresAtUtc | datetime(6) | 不可 | 有効期限 |
| UsedAtUtc | datetime(6) | 可 | 使用日時 |
| CreatedAtUtc | datetime(6) | 不可 | 作成日時 |

### インデックス

- `UNIQUE (Token)`
- `INDEX (UserId)`

### 削除ルール

- Users削除時：Cascade

### 業務要件

- トークンは有効期限内かつ未使用の場合のみ有効とする。
- パスワード再設定後は `UsedAtUtc` を記録する。
- 同一トークンの再利用を禁止する。

---

## 6.6 Rooms

スタジオの基本情報を管理する。

| カラム | DB型 | NULL | 制約・説明 |
|---|---|---:|---|
| Id | int | 不可 | PK、AUTO_INCREMENT |
| UserId | int | 不可 | FK → Users.Id、所有ホスト |
| Name | varchar(200) | 不可 | スタジオ名 |
| ImageName | varchar(255) | 可 | 画像ファイル名 |
| Description | varchar(2000) | 可 | 説明 |
| Price | int | 不可 | 基本料金 |
| Capacity | int | 不可 | 最大定員 |
| PostalCode | varchar(20) | 不可 | 郵便番号 |
| Address | varchar(300) | 不可 | 住所 |
| CreatedAtUtc | datetime(6) | 不可 | 作成日時 |
| UpdatedAtUtc | datetime(6) | 不可 | 更新日時 |

### インデックス

- `INDEX (UserId)`
- `UNIQUE (Name, Address)`

### 削除ルール

- Users削除時：Cascade

### 業務要件

- 基本料金は1円以上とする。
- 最大定員は1人以上とする。
- 同一名称かつ同一住所のスタジオは重複登録不可とする。
- スタジオ所有者はHostロールを持つユーザーとする。
- 予約またはレビューが存在するスタジオは、関連制約により物理削除できない場合がある。

---

## 6.7 BusinessHours

スタジオの曜日別営業時間を管理する。

| カラム | DB型 | NULL | 制約・説明 |
|---|---|---:|---|
| Id | int | 不可 | PK、AUTO_INCREMENT |
| RoomId | int | 不可 | FK → Rooms.Id |
| DayOfWeek | int | 不可 | 月=1～日=7 |
| StartTime | time | 可 | 開始時刻 |
| EndTime | time | 可 | 終了時刻 |
| IsHoliday | tinyint(1) | 不可 | 休業日フラグ |
| CreatedAtUtc | datetime(6) | 不可 | 作成日時 |
| UpdatedAtUtc | datetime(6) | 不可 | 更新日時 |

### インデックス

- `UNIQUE (RoomId, DayOfWeek)`

### 削除ルール

- Rooms削除時：Cascade

### 業務要件

- 1スタジオにつき同一曜日の設定は1件までとする。
- `IsHoliday = true` の場合、開始・終了時刻はNULLを許可する。
- 営業日の場合、開始・終了時刻を必須とする。
- 終了時刻は開始時刻より後とする。
- `DayOfWeek` は1～7の範囲とする。

---

## 6.8 Closures

スタジオの臨時休館・利用不可期間を管理する。

| カラム | DB型 | NULL | 制約・説明 |
|---|---|---:|---|
| Id | int | 不可 | PK、AUTO_INCREMENT |
| RoomId | int | 不可 | FK → Rooms.Id |
| StartAt | datetime(6) | 不可 | 開始日時 |
| EndAt | datetime(6) | 不可 | 終了日時 |
| Reason | varchar(255) | 可 | 休館理由 |
| CreatedAt | datetime(6) | 不可 | 作成日時 |

### インデックス

- `INDEX (RoomId)`

### 削除ルール

- Rooms削除時：Cascade

### 業務要件

- 終了日時は開始日時より後とする。
- 予約済み時間帯と重複する休館登録の扱いはアプリケーション側で制御する。
- 日時の保存基準がUTCかローカル時刻かを統一する。

---

## 6.9 PriceRules

曜日・時間帯別の料金ルールを管理する。

| カラム | DB型 | NULL | 制約・説明 |
|---|---|---:|---|
| Id | int | 不可 | PK、AUTO_INCREMENT |
| RoomId | int | 不可 | FK → Rooms.Id |
| RuleType | varchar(50) | 不可 | ルール種別 |
| Weekday | int | 可 | 日=0～土=6、NULL=全曜日 |
| StartHour | time | 可 | 適用開始時刻 |
| EndHour | time | 可 | 適用終了時刻 |
| Multiplier | decimal(5,2) | 可 | 料金倍率 |
| FlatFee | int | 可 | 固定加算額 |
| Note | varchar(500) | 可 | 備考 |
| CreatedAtUtc | datetime(6) | 不可 | 作成日時 |
| UpdatedAtUtc | datetime(6) | 不可 | 更新日時 |

### インデックス

- `INDEX (RoomId, RuleType, Weekday)`

### 削除ルール

- Rooms削除時：Cascade

### 想定値

- `multiplier`
- `flat_fee`

### 業務要件

- `multiplier` の場合、開始時刻、終了時刻、倍率を必須とする。
- `multiplier` の倍率は0より大きい値とする。
- `flat_fee` の場合、固定費を必須とする。
- 固定費は0円以上とする。
- `flat_fee` の場合、開始時刻、終了時刻、倍率は使用しない。
- `Weekday` はNULLまたは0～6とする。
- 同一時間帯に複数ルールが適用される場合の優先順位はサービス仕様で定義する。

---

## 6.10 Reservations

スタジオ予約を管理する。

| カラム | DB型 | NULL | 制約・説明 |
|---|---|---:|---|
| Id | int | 不可 | PK、AUTO_INCREMENT |
| RoomId | int | 不可 | FK → Rooms.Id |
| UserId | int | 不可 | FK → Users.Id、予約者 |
| StartAt | datetime(6) | 不可 | 利用開始日時 |
| EndAt | datetime(6) | 不可 | 利用終了日時 |
| Amount | int | 不可 | 最終予約金額 |
| Status | varchar(50) | 不可 | 予約状態 |
| CreatedAtUtc | datetime(6) | 不可 | 作成日時 |
| UpdatedAtUtc | datetime(6) | 不可 | 更新日時 |

### インデックス

- `INDEX (RoomId)`
- `INDEX (UserId)`
- `INDEX (Status)`
- `INDEX (StartAt)`

### 削除ルール

- Rooms削除時：Restrict
- Users削除時：Restrict

### 想定値

- `booked`
- `paid`
- `canceled`

### 業務要件

- 終了日時は開始日時より後とする。
- 予約金額は0円以上とする。
- 予約者はGeneralUserロールを持つユーザーとする。
- 同一スタジオの同一時間帯に有効な予約を重複させない。
- 予約重複判定はアプリケーション層で行う。
- 予約確定・決済完了処理はトランザクション内で行うことが望ましい。
- キャンセル済み予約を重複判定対象に含めるかは業務仕様で定義する。

---

## 6.11 ReservationChargeItems

予約料金の計算内訳を管理する。

| カラム | DB型 | NULL | 制約・説明 |
|---|---|---:|---|
| Id | int | 不可 | PK、AUTO_INCREMENT |
| ReservationId | int | 不可 | FK → Reservations.Id |
| Kind | varchar(50) | 不可 | 明細種別 |
| Description | varchar(500) | 可 | 説明 |
| SliceAmount | int | 不可 | 明細金額 |
| SliceStart | datetime(6) | 可 | 適用開始日時 |
| SliceEnd | datetime(6) | 可 | 適用終了日時 |
| UnitRatePerHour | int | 可 | 時間単価 |

### インデックス

- `INDEX (ReservationId)`
- `INDEX (SliceStart)`

### 削除ルール

- Reservations削除時：Cascade

### 想定値

- `base`
- `multiplier`
- `tax`
- `platform_fee`

### 業務要件

- 予約確定時点の料金計算結果を保存する。
- 料金ルール変更後も過去予約の金額内訳が変わらないこと。
- `SliceEnd` は `SliceStart` より後とする。
- 税・手数料明細では時間帯項目をNULLとすることを許可する。

---

## 6.12 Reviews

利用者レビューとホスト返信を管理する。

| カラム | DB型 | NULL | 制約・説明 |
|---|---|---:|---|
| Id | int | 不可 | PK、AUTO_INCREMENT |
| RoomId | int | 不可 | FK → Rooms.Id |
| UserId | int | 不可 | FK → Users.Id |
| Score | int | 不可 | 評価 |
| Content | varchar(2000) | 不可 | コメント |
| PublicVisible | tinyint(1) | 不可 | 公開状態 |
| HiddenReason | varchar(500) | 可 | 非公開理由 |
| HostReply | varchar(2000) | 可 | ホスト返信 |
| HostReplyAt | datetime(6) | 可 | 返信日時 |
| CreatedAtUtc | datetime(6) | 不可 | 作成日時 |
| UpdatedAtUtc | datetime(6) | 不可 | 更新日時 |

### インデックス

- `INDEX (RoomId)`
- `INDEX (UserId)`
- `INDEX (Score)`
- `INDEX (PublicVisible)`
- `INDEX (CreatedAtUtc)`

### 削除ルール

- Rooms削除時：Restrict
- Users削除時：Restrict

### 業務要件

- 評価は1～5とする。
- コメントは必須とする。
- 非公開にする場合は理由入力を推奨する。
- ホスト返信が存在する場合、返信日時を保持する。
- レビュー投稿可能条件は、対象スタジオの利用実績があることとする。
- 現行DBでは予約とレビューの直接関連を保持していない。
- 1予約1レビューを厳密に保証する場合は `ReservationId` の追加を検討する。

---

## 6.13 AppSettings

アプリケーション共通設定を管理する。

| カラム | DB型 | NULL | 制約・説明 |
|---|---|---:|---|
| Id | int | 不可 | PK、AUTO_INCREMENT |
| Key | varchar(100) | 不可 | 設定キー、一意 |
| Value | varchar(100) | 不可 | 設定値 |
| UpdatedAtUtc | datetime(6) | 不可 | 更新日時 |

### インデックス

- `UNIQUE (Key)`

### 想定設定

- 税率
- 管理手数料率

### 業務要件

- 設定値は文字列で保持し、利用時に型変換する。
- 税率・管理手数料率は0～100の範囲とする。
- 設定変更時は監査ログを記録する。

---

## 6.14 AuditLogs

管理操作、ログイン、更新などの監査ログを管理する。

| カラム | DB型 | NULL | 制約・説明 |
|---|---|---:|---|
| Id | int | 不可 | PK、AUTO_INCREMENT |
| Ts | datetime(6) | 不可 | 操作日時 |
| ActorId | int | 可 | 操作者ID、FKなし |
| Action | varchar(100) | 不可 | 操作種別 |
| Entity | varchar(100) | 不可 | 対象種別 |
| EntityId | int | 可 | 対象ID、FKなし |
| Note | varchar(2000) | 可 | 操作内容・補足 |

### インデックス

- `INDEX (Ts)`
- `INDEX (ActorId)`
- `INDEX (Action)`
- `INDEX (Entity)`
- `INDEX (EntityId)`

### 業務要件

- 操作者や対象データが削除されてもログを保持する。
- 更新前後の詳細な差分を残す場合は `Note` に記録する。
- ログの保存期間と削除方針は運用要件で定義する。
- 監査ログは原則として更新・削除しない。

---

## 6.15 AiSearchLogs

AIスタジオ検索の実行履歴を管理する。

| カラム | DB型 | NULL | 制約・説明 |
|---|---|---:|---|
| Id | int | 不可 | PK、AUTO_INCREMENT |
| CreatedAtUtc | datetime(6) | 不可 | 実行日時 |
| Query | varchar(500) | 不可 | 検索文 |
| IpAddress | varchar(100) | 可 | 接続元IP |
| UserId | int | 可 | 利用者ID、FKなし |
| Model | varchar(100) | 可 | 使用AIモデル |
| Succeeded | tinyint(1) | 不可 | 成功可否 |
| ResultCount | int | 不可 | 結果件数 |
| ErrorMessage | varchar(1000) | 可 | エラー内容 |

### インデックス

- `INDEX (CreatedAtUtc)`
- `INDEX (IpAddress)`
- `INDEX (UserId)`
- `INDEX (Succeeded)`

### 業務要件

- 未認証ユーザーによる検索では `UserId` をNULLとする。
- AI検索失敗時も履歴を記録する。
- 検索文は500文字以内とする。
- 個人情報・機密情報を検索文に保存する場合の取扱いを運用規定で定義する。
- 保存期間と削除方針を定義する。

---

# 7. 外部キー・削除ルール一覧

| 子テーブル | 外部キー | 親テーブル | 削除時動作 |
|---|---|---|---|
| UserRoles | UserId | Users | Cascade |
| UserRoles | RoleId | Roles | Cascade |
| VerificationTokens | UserId | Users | Cascade |
| PasswordResetTokens | UserId | Users | Cascade |
| Rooms | UserId | Users | Cascade |
| BusinessHours | RoomId | Rooms | Cascade |
| Closures | RoomId | Rooms | Cascade |
| PriceRules | RoomId | Rooms | Cascade |
| Reservations | RoomId | Rooms | Restrict |
| Reservations | UserId | Users | Restrict |
| ReservationChargeItems | ReservationId | Reservations | Cascade |
| Reviews | RoomId | Rooms | Restrict |
| Reviews | UserId | Users | Restrict |

---

# 8. 一意制約一覧

| テーブル | 対象 |
|---|---|
| Users | Email |
| Roles | Name |
| UserRoles | UserId + RoleId |
| VerificationTokens | Token |
| PasswordResetTokens | Token |
| Rooms | Name + Address |
| BusinessHours | RoomId + DayOfWeek |
| AppSettings | Key |

---

# 9. インデックス一覧

## 9.1 認証・会員

| テーブル | インデックス |
|---|---|
| Users | Email（Unique） |
| Roles | Name（Unique） |
| UserRoles | RoleId、UserId＋RoleId（Unique） |
| VerificationTokens | Token（Unique）、UserId |
| PasswordResetTokens | Token（Unique）、UserId |

## 9.2 スタジオ

| テーブル | インデックス |
|---|---|
| Rooms | UserId、Name＋Address（Unique） |
| BusinessHours | RoomId＋DayOfWeek（Unique） |
| Closures | RoomId |
| PriceRules | RoomId＋RuleType＋Weekday |

## 9.3 予約・レビュー

| テーブル | インデックス |
|---|---|
| Reservations | RoomId、UserId、Status、StartAt |
| ReservationChargeItems | ReservationId、SliceStart |
| Reviews | RoomId、UserId、Score、PublicVisible、CreatedAtUtc |

## 9.4 管理・ログ

| テーブル | インデックス |
|---|---|
| AppSettings | Key（Unique） |
| AuditLogs | Ts、ActorId、Action、Entity、EntityId |
| AiSearchLogs | CreatedAtUtc、IpAddress、UserId、Succeeded |

---

# 10. 区分値・ステータス

| 対象 | 値 |
|---|---|
| ロール | `Admin`、`Host`、`GeneralUser` |
| 利用区分 | `personal`、`business` |
| 予約状態 | `booked`、`paid`、`canceled` |
| 料金ルール種別 | `multiplier`、`flat_fee` |
| 料金明細種別 | `base`、`multiplier`、`tax`、`platform_fee` |
| BusinessHours曜日 | 月=1、火=2、水=3、木=4、金=5、土=6、日=7 |
| PriceRules曜日 | 日=0、月=1、火=2、水=3、木=4、金=5、土=6、NULL=全曜日 |

---

# 11. 業務データ制約

以下はDBのCHECK制約ではなく、主にアプリケーション層で保証する。

| 対象 | 制約 |
|---|---|
| Users.Email | メールアドレス形式であること |
| Users.UsageType | `personal` または `business` |
| Rooms.Price | 1円以上 |
| Rooms.Capacity | 1人以上 |
| BusinessHours.DayOfWeek | 1～7 |
| BusinessHours | 営業日は開始・終了時刻必須 |
| BusinessHours | 終了時刻 > 開始時刻 |
| Closures | 終了日時 > 開始日時 |
| PriceRules.Weekday | NULLまたは0～6 |
| PriceRules.Multiplier | 0より大きい |
| PriceRules.FlatFee | 0円以上 |
| Reservations | 終了日時 > 開始日時 |
| Reservations | 同一スタジオの有効予約時間帯を重複させない |
| Reservations.Amount | 0円以上 |
| Reviews.Score | 1～5 |
| Reviews.Content | 必須、最大2000文字 |
| AppSettings | 税率・手数料率は0～100 |
| VerificationTokens | 有効期限内かつ未使用 |
| PasswordResetTokens | 有効期限内かつ未使用 |

---

# 12. トランザクション・排他要件

## 12.1 予約確定

以下は同一トランザクション内で実行することが望ましい。

1. 予約重複確認
2. 料金計算
3. Reservations登録
4. ReservationChargeItems登録
5. Stripe決済情報との整合確認
6. 監査ログ記録

同時予約による二重予約を防ぐため、予約重複確認から登録までの排他制御を考慮する。

## 12.2 管理設定更新

管理設定更新と監査ログ記録は、可能な限り同一トランザクションで実行する。

## 12.3 予約承認・キャンセル

予約状態変更と監査ログ記録は、可能な限り同一トランザクションで実行する。

---

# 13. データ保持・削除方針

| データ | 方針 |
|---|---|
| Users | 原則論理無効化 |
| Roles | マスタデータとして原則削除しない |
| VerificationTokens | 使用済み・期限切れデータの定期削除を検討 |
| PasswordResetTokens | 使用済み・期限切れデータの定期削除を検討 |
| Rooms | 予約・レビューが存在する場合は物理削除しない |
| Reservations | 売上・履歴として原則保持 |
| ReservationChargeItems | 予約とともに保持 |
| Reviews | 原則保持。公開状態で制御 |
| AuditLogs | 原則変更不可。保存期間を運用要件で定義 |
| AiSearchLogs | 保存期間と匿名化・削除方針を定義 |
| AppSettings | 現行値を保持。変更履歴はAuditLogsで追跡 |

---

# 14. セキュリティ要件

1. パスワードはハッシュ化し、平文を保存しない。
2. 認証トークン・再設定トークンは十分なランダム性を持たせる。
3. トークンは有効期限と使用済み日時を確認する。
4. 監査ログにはパスワード、JWT、Stripe秘密情報などを記録しない。
5. AI検索ログに個人情報が入力される可能性を考慮する。
6. ログ閲覧はAdminロールに限定する。
7. DB接続情報はソースコードへ直接記述せず、環境変数または秘密情報管理機能で管理する。
8. 本番DBへのアクセス権限は最小権限とする。

---

# 15. 性能要件・インデックス検討

現状のインデックスは基本的な検索条件をカバーしている。

データ量増加時は以下を検討する。

| 対象 | 候補 |
|---|---|
| Reservations | `(RoomId, StartAt, EndAt)` |
| Reservations | `(UserId, CreatedAtUtc)` |
| Reservations | `(Status, StartAt)` |
| Reviews | `(RoomId, PublicVisible, CreatedAtUtc)` |
| AuditLogs | `(Ts, Action)` |
| AuditLogs | `(Entity, EntityId, Ts)` |
| AiSearchLogs | `(CreatedAtUtc, Succeeded)` |
| Closures | `(RoomId, StartAt, EndAt)` |

実際の追加は、実行計画と検索頻度を確認して判断する。

---

# 16. 現状の設計上の注意点

## 16.1 曜日番号体系の不統一

`BusinessHours` と `PriceRules` で曜日番号体系が異なる。

```text
BusinessHours:
月=1 ～ 日=7

PriceRules:
日=0 ～ 土=6
NULL=全曜日
```

変換処理を共通化し、設計書へ明記する。

## 16.2 レビューと予約の直接関連がない

`Reviews` に `ReservationId` が存在しない。

現行ではアプリケーション側でレビュー可能条件を判定する必要がある。1予約1レビューを厳密に保証する場合は、次を検討する。

```text
Reviews.ReservationId
UNIQUE (ReservationId)
```

## 16.3 予約重複制約がない

時間範囲の重複をDB一意制約では防止していない。

同一スタジオに対する予約の重複判定はサービス層で行い、必要に応じてトランザクション・ロックを使用する。

## 16.4 ログの参照整合性

`AuditLogs` と `AiSearchLogs` のユーザーIDには外部キーがない。

履歴保持上は妥当だが、ユーザー名などを後から確実に表示する必要がある場合は、操作時点の表示名をログ側へ保持する案もある。

## 16.5 UsersからRoomsへのCascade

ホスト削除時にRoomsはCascade対象だが、RoomsにReservationsまたはReviewsが存在するとRestrictにより削除できない可能性がある。

このため、ユーザーおよびスタジオは原則として物理削除せず、無効化または公開停止で運用する。

## 16.6 CHECK制約

Snapshot上ではCHECK制約が確認できない。

価格、定員、評価、日時前後関係などはアプリケーション側で検証しているが、必要に応じてDB CHECK制約追加を検討する。

## 16.7 Closuresの日時命名

`Closures` のみ `CreatedAt`、`StartAt`、`EndAt` となっており、UTC命名規則と統一されていない。

保存時刻の基準を明確にする。

---

# 17. 今後の改善候補

1. `Reviews.ReservationId` の追加
2. 予約重複防止の排他制御強化
3. CHECK制約の追加
4. 曜日番号体系の統一
5. Users・Roomsの明示的な論理削除項目追加
6. 監査ログへの操作時点表示名保持
7. ログ保存期間と削除ジョブの実装
8. 予約検索用複合インデックスの追加
9. `Closures` のUTC命名統一
10. AppSettingsの値型・単位を定義する設定メタデータ追加

---

# 18. 本書の位置付け

本書はDBの現行実装を基にした棚卸し・要件資料である。

詳細設計では、以下を別途作成する。

- ER図
- 全15テーブルの詳細テーブル定義書
- 初期データ・マスタ定義
- Migration方針
- バックアップ・リストア方針
- 本番環境の接続・権限設計
- データ保持期間
- SQL性能試験方針
