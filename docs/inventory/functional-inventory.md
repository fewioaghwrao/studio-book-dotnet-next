# Studio Book 機能一覧

## 1. 文書概要

本書は、Studio Bookの既存Controller実装を基に、現在実装されている機能を整理した棚卸し資料である。

要件定義書、基本設計書、詳細設計書を作成する際の基礎資料として使用する。

## 2. 利用者区分

| 利用者区分   | 概要                                      |
| ------- | --------------------------------------- |
| 未認証ユーザー | ログインせずにスタジオ検索、スタジオ詳細確認、AI検索などを利用する      |
| 一般ユーザー  | スタジオ予約、予約履歴確認、レビュー投稿、会員情報更新などを行う        |
| ホスト     | 自身が所有するスタジオ、営業時間、休業日、料金、予約、レビュー、売上を管理する |
| 管理者     | 会員、スタジオ、予約、システム設定、監査ログなどを管理する           |
| 外部サービス  | Stripeからの決済完了通知など、外部システムからAPIを呼び出す      |

---

# 3. 共通・認証機能

|  No. | 機能分類  | 機能名          | 利用者      | API                         | HTTP | 概要                               | 主なController   |
| ---: | ----- | ------------ | -------- | --------------------------- | ---- | -------------------------------- | -------------- |
| C-01 | ホーム   | ホーム情報取得      | 全利用者     | `/api/home`                 | GET  | ホーム画面に表示するスタジオや関連情報を取得する         | HomeController |
| C-02 | 認証    | ログイン         | 未認証ユーザー  | `/api/auth/login`           | POST | メールアドレスとパスワードを検証し、JWTを発行する       | AuthController |
| C-03 | 認証    | ログインユーザー情報取得 | 認証済みユーザー | `/api/auth/me`              | GET  | JWTから現在ログインしているユーザー情報を取得する       | AuthController |
| C-04 | 会員情報  | 自分の会員情報更新    | 認証済みユーザー | `/api/auth/me`              | PUT  | 現在のユーザーの氏名、メールアドレスなどを更新する        | AuthController |
| C-05 | 認証    | ログアウト        | 認証済みユーザー | `/api/auth/logout`          | POST | ログアウト処理を実行する                     | AuthController |
| C-06 | 会員登録  | 新規会員登録申請     | 未認証ユーザー  | `/api/auth/signup`          | POST | 会員登録情報を受け付け、メール認証用メールを送信する       | AuthController |
| C-07 | 会員登録  | メールアドレス認証    | 未認証ユーザー  | `/api/auth/verify`          | GET  | 認証トークンを検証し、会員登録を完了する             | AuthController |
| C-08 | パスワード | パスワード再設定申請   | 未認証ユーザー  | `/api/auth/forgot-password` | POST | パスワード再設定メールの送信を要求する              | AuthController |
| C-09 | パスワード | パスワード再設定     | 未認証ユーザー  | `/api/auth/reset-password`  | POST | 再設定トークンを検証し、新しいパスワードを登録する        | AuthController |
| C-10 | 監査    | ログイン監査ログ記録   | システム     | ―                           | ―    | ログイン成功時にユーザー、操作種別、対象情報を監査ログへ記録する | AuthController |

---

# 4. スタジオ検索・閲覧機能

|  No. | 機能分類   | 機能名       | 利用者  | API                   | HTTP | 概要                                    | 主なController           |
| ---: | ------ | --------- | ---- | --------------------- | ---- | ------------------------------------- | ---------------------- |
| R-01 | スタジオ検索 | スタジオ一覧検索  | 全利用者 | `/api/rooms`          | GET  | キーワード、地域、料金、並び順を指定してスタジオを検索する         | RoomsController        |
| R-02 | スタジオ検索 | 検索結果ページング | 全利用者 | `/api/rooms`          | GET  | ページ番号と表示件数を指定して検索結果を取得する              | RoomsController        |
| R-03 | スタジオ閲覧 | スタジオ詳細取得  | 全利用者 | `/api/rooms/{roomId}` | GET  | 指定したスタジオの詳細情報を取得する                    | RoomsController        |
| R-04 | AI検索   | 自然文スタジオ検索 | 全利用者 | `/api/ai/room-search` | POST | 自然文で入力された希望条件を解析し、条件に合うスタジオを検索する      | AiRoomSearchController |
| R-05 | AI検索   | AI検索レート制限 | 全利用者 | `/api/ai/room-search` | POST | AI検索の過剰利用を防ぐため、利用回数を制限する              | AiRoomSearchController |
| R-06 | AI検索   | AI検索履歴記録  | システム | ―                     | ―    | 検索文、IPアドレス、利用者、成功可否、検索結果件数、エラー内容を記録する | AiRoomSearchController |

---

# 5. 一般ユーザー向け予約機能

|  No. | 機能分類 | 機能名               | 利用者    | API                         | HTTP     | 概要                                       | 主なController           |
| ---: | ---- | ----------------- | ------ | --------------------------- | -------- | ---------------------------------------- | ---------------------- |
| U-01 | 予約   | 予約内容確認            | 一般ユーザー | `/api/reservations/confirm` | POST     | 希望日時やスタジオ情報を基に予約内容と料金を計算する               | ReservationsController |
| U-02 | 決済   | Stripe Checkout作成 | 一般ユーザー | `/api/reservations/confirm` | POST     | 予約確認後にStripe Checkout Sessionを作成する       | ReservationsController |
| U-03 | 決済   | Stripe公開キー返却      | 一般ユーザー | `/api/reservations/confirm` | POST     | フロントエンドで決済画面へ遷移するための公開キーを返す              | ReservationsController |
| U-04 | 決済   | Checkout URL返却    | 一般ユーザー | `/api/reservations/confirm` | POST     | Stripe決済画面のURLとSession IDを返す             | ReservationsController |
| U-05 | 予約   | 自分の予約一覧取得         | 一般ユーザー | `/api/reservations`         | GET      | ログインユーザー自身の予約履歴をページングして取得する              | ReservationsController |
| U-06 | 権限制御 | 一般ユーザー限定予約        | 一般ユーザー | `/api/reservations/*`       | GET・POST | HostやAdminではなく、GeneralUserロールのみ予約処理を許可する | ReservationsController |

---

# 6. レビュー機能

|   No. | 機能分類 | 機能名          | 利用者      | API                               | HTTP | 概要                                | 主なController             |
| ----: | ---- | ------------ | -------- | --------------------------------- | ---- | --------------------------------- | ------------------------ |
| RV-01 | レビュー | レビュー投稿画面情報取得 | 一般ユーザー   | `/api/rooms/{roomId}/reviews/new` | GET  | スタジオ情報、予約情報、既存レビューなど投稿に必要な情報を取得する | ReviewsController        |
| RV-02 | レビュー | レビュー投稿       | 一般ユーザー   | `/api/rooms/{roomId}/reviews`     | POST | 利用したスタジオに対する評価とコメントを登録する          | ReviewsController        |
| RV-03 | レビュー | 予約との関連確認     | 一般ユーザー   | `/api/rooms/{roomId}/reviews/new` | GET  | 指定された予約がログインユーザーの予約か確認する          | ReviewsController        |
| RV-04 | AI補助 | レビュー文章生成補助   | 認証済みユーザー | `/api/ai/review-assist`           | POST | 入力内容を基にAIがレビュー文章の作成を補助する          | AiReviewAssistController |
| RV-05 | AI補助 | AI補助入力検証     | 認証済みユーザー | `/api/ai/review-assist`           | POST | 不正・不足した入力に対して入力エラーを返す             | AiReviewAssistController |

---

# 7. Stripe決済連携機能

|   No. | 機能分類   | 機能名                | 利用者    | API                   | HTTP | 概要                                               | 主なController            |
| ----: | ------ | ------------------ | ------ | --------------------- | ---- | ------------------------------------------------ | ----------------------- |
| ST-01 | Stripe | Webhook受信          | Stripe | `/api/stripe/webhook` | POST | Stripeから送信されるイベントを受信する                           | StripeWebhookController |
| ST-02 | Stripe | Webhook署名検証        | Stripe | `/api/stripe/webhook` | POST | Stripe-SignatureとWebhook Secretを使用して通知元を検証する     | StripeWebhookController |
| ST-03 | Stripe | 決済完了イベント処理         | システム   | ―                     | ―    | `checkout.session.completed`を受信した場合に予約確定処理を実行する  | StripeWebhookController |
| ST-04 | Stripe | Checkout Session取得 | システム   | ―                     | ―    | Stripe APIからCheckout SessionとPaymentIntentを再取得する | StripeWebhookController |
| ST-05 | Stripe | 決済メタデータ取得          | システム   | ―                     | ―    | PaymentIntentに保存された予約情報を取得する                     | StripeWebhookController |
| ST-06 | 予約     | 決済後予約確定            | システム   | ―                     | ―    | 決済金額、PaymentIntent ID、Session IDを使用して予約を確定する     | StripeWebhookController |
| ST-07 | ログ     | Stripe処理ログ出力       | システム   | ―                     | ―    | Webhook受信、署名検証、処理成功、処理失敗をアプリケーションログへ出力する         | StripeWebhookController |

---

# 8. ホスト向けスタジオ管理機能

|  No. | 機能分類   | 機能名        | 利用者 | API                    | HTTP | 概要                           | 主なController        |
| ---: | ------ | ---------- | --- | ---------------------- | ---- | ---------------------------- | ------------------- |
| H-01 | スタジオ管理 | 所有スタジオ一覧取得 | ホスト | `/api/host/rooms`      | GET  | ログインホストが所有するスタジオを一覧取得する      | HostRoomsController |
| H-02 | スタジオ管理 | 所有スタジオ検索   | ホスト | `/api/host/rooms`      | GET  | スタジオ名のキーワードで所有スタジオを検索する      | HostRoomsController |
| H-03 | スタジオ管理 | 所有スタジオ詳細取得 | ホスト | `/api/host/rooms/{id}` | GET  | ログインホストが所有するスタジオの詳細を取得する     | HostRoomsController |
| H-04 | 権限制御   | 所有者チェック    | ホスト | `/api/host/rooms/*`    | GET  | 他ホストが所有するスタジオ情報を取得できないよう制御する | HostRoomsController |

---

# 9. ホスト向け営業時間・休業日管理

|  No. | 機能分類 | 機能名            | 利用者 | API                                             | HTTP   | 概要                      | 主なController                |
| ---: | ---- | -------------- | --- | ----------------------------------------------- | ------ | ----------------------- | --------------------------- |
| H-05 | 営業時間 | 営業時間取得         | ホスト | `/api/host/rooms/{roomId}/business-hours`       | GET    | 対象スタジオの曜日別営業時間を取得する     | HostBusinessHoursController |
| H-06 | 営業時間 | 営業時間更新         | ホスト | `/api/host/rooms/{roomId}/business-hours`       | PUT    | 曜日別の営業開始時刻、終了時刻などを更新する  | HostBusinessHoursController |
| H-07 | 営業時間 | 営業時間入力検証       | ホスト | `/api/host/rooms/{roomId}/business-hours`       | PUT    | 開始・終了時刻などの入力内容を検証する     | HostBusinessHoursController |
| H-08 | 休業日  | 休業日一覧取得        | ホスト | `/api/host/rooms/{roomId}/closures`             | GET    | 対象スタジオに登録された休業日を取得する    | HostClosuresController      |
| H-09 | 休業日  | カレンダー用休業イベント取得 | ホスト | `/api/host/rooms/{roomId}/closures/events`      | GET    | カレンダー表示用の休業日イベント情報を取得する | HostClosuresController      |
| H-10 | 休業日  | 休業日登録          | ホスト | `/api/host/rooms/{roomId}/closures`             | POST   | スタジオの臨時休業日や利用不可期間を登録する  | HostClosuresController      |
| H-11 | 休業日  | 休業日削除          | ホスト | `/api/host/rooms/{roomId}/closures/{closureId}` | DELETE | 登録済みの休業日を削除する           | HostClosuresController      |

---

# 10. ホスト向け料金ルール管理

|  No. | 機能分類 | 機能名       | 利用者 | API                                             | HTTP   | 概要                            | 主なController             |
| ---: | ---- | --------- | --- | ----------------------------------------------- | ------ | ----------------------------- | ------------------------ |
| H-12 | 料金設定 | 料金ルール取得   | ホスト | `/api/host/rooms/{roomId}/price-rules`          | GET    | 対象スタジオの時間帯・曜日などに応じた料金ルールを取得する | HostPriceRulesController |
| H-13 | 料金設定 | 料金ルール登録   | ホスト | `/api/host/rooms/{roomId}/price-rules`          | POST   | 対象スタジオへ新しい料金ルールを追加する          | HostPriceRulesController |
| H-14 | 料金設定 | 料金ルール削除   | ホスト | `/api/host/rooms/{roomId}/price-rules/{ruleId}` | DELETE | 登録済みの料金ルールを削除する               | HostPriceRulesController |
| H-15 | 料金設定 | 料金ルール入力検証 | ホスト | `/api/host/rooms/{roomId}/price-rules`          | POST   | 料金、適用時間、適用条件などを検証する           | HostPriceRulesController |

---

# 11. ホスト向け予約管理

|  No. | 機能分類 | 機能名         | 利用者  | API                                              | HTTP | 概要                            | 主なController               |
| ---: | ---- | ----------- | ---- | ------------------------------------------------ | ---- | ----------------------------- | -------------------------- |
| H-16 | 予約管理 | 予約一覧取得      | ホスト  | `/api/host/reservations`                         | GET  | 自身が所有するスタジオの予約を一覧取得する         | HostReservationsController |
| H-17 | 予約管理 | 予約検索        | ホスト  | `/api/host/reservations`                         | GET  | キーワード、状態、予約ID、スタジオ、期間で予約を検索する | HostReservationsController |
| H-18 | 予約管理 | 予約承認        | ホスト  | `/api/host/reservations/{reservationId}/approve` | POST | 対象予約をホストが承認する                 | HostReservationsController |
| H-19 | 予約管理 | 予約キャンセル     | ホスト  | `/api/host/reservations/{reservationId}/cancel`  | POST | 対象予約をホストがキャンセルする              | HostReservationsController |
| H-20 | 監査   | 予約承認ログ記録    | システム | ―                                                | ―    | 予約承認時に監査ログを記録する               | HostReservationsController |
| H-21 | 監査   | 予約キャンセルログ記録 | システム | ―                                                | ―    | 予約キャンセル時に監査ログを記録する            | HostReservationsController |

---

# 12. ホスト向けレビュー管理

|  No. | 機能分類   | 機能名        | 利用者 | API                                       | HTTP | 概要                       | 主なController          |
| ---: | ------ | ---------- | --- | ----------------------------------------- | ---- | ------------------------ | --------------------- |
| H-22 | レビュー管理 | レビュー一覧取得   | ホスト | `/api/host/reviews`                       | GET  | 自身のスタジオに投稿されたレビューを取得する   | HostReviewsController |
| H-23 | レビュー管理 | レビュー絞り込み   | ホスト | `/api/host/reviews`                       | GET  | スタジオ、評価点、非表示状態でレビューを絞り込む | HostReviewsController |
| H-24 | レビュー管理 | レビュー返信登録   | ホスト | `/api/host/reviews/{reviewId}/reply`      | POST | 利用者が投稿したレビューにホストが返信する    | HostReviewsController |
| H-25 | レビュー管理 | レビュー公開状態変更 | ホスト | `/api/host/reviews/{reviewId}/visibility` | POST | レビューの表示・非表示状態を変更する       | HostReviewsController |

---

# 13. ホスト向け売上管理

|  No. | 機能分類  | 機能名         | 利用者 | API                                           | HTTP | 概要                                  | 主なController              |
| ---: | ----- | ----------- | --- | --------------------------------------------- | ---- | ----------------------------------- | ------------------------- |
| H-26 | 売上管理  | 売上一覧取得      | ホスト | `/api/host/sales`                             | GET  | 所有スタジオの予約・売上情報を一覧取得する               | HostSalesController       |
| H-27 | 売上管理  | 売上一覧絞り込み    | ホスト | `/api/host/sales`                             | GET  | スタジオおよび売上明細の有無で一覧を絞り込む              | HostSalesController       |
| H-28 | 売上管理  | 売上詳細取得      | ホスト | `/api/host/sales/{reservationId}`             | GET  | 指定予約の売上情報と料金明細を取得する                 | HostSalesController       |
| H-29 | CSV出力 | 売上一覧CSV出力   | ホスト | `/api/host/sales.csv`                         | GET  | 売上一覧をExcelで開けるUTF-8 BOM付きCSVとして出力する | HostSalesCsvController    |
| H-30 | CSV出力 | 予約料金明細CSV出力 | ホスト | `/api/host/sales/{reservationId}/items.csv`   | GET  | 指定予約の基本料金、加算料金、税、手数料などをCSV出力する      | HostSalesExportController |
| H-31 | PDF出力 | 請求書PDF出力    | ホスト | `/api/host/sales/{reservationId}/invoice.pdf` | GET  | 指定予約の売上情報と料金内訳を請求書形式のPDFとして出力する     | HostSalesExportController |
| H-32 | PDF出力 | 日本語フォント対応   | ホスト | `/api/host/sales/{reservationId}/invoice.pdf` | GET  | NotoSansJPを使用して日本語を含むPDFを生成する       | HostSalesExportController |
| H-33 | 権限制御  | 売上所有者チェック   | ホスト | `/api/host/sales/*`                           | GET  | 他ホストのスタジオに関する売上情報を取得できないよう制御する      | HostSalesControllerほか     |

---

# 14. ホスト向け稼働状況機能

|  No. | 機能分類    | 機能名       | 利用者 | API                | HTTP | 概要                        | 主なController         |
| ---: | ------- | --------- | --- | ------------------ | ---- | ------------------------- | -------------------- |
| H-34 | ダッシュボード | ホスト稼働状況取得 | ホスト | `/api/host/status` | GET  | ホストが所有するスタジオの予約・稼働状況を取得する | HostStatusController |
| H-35 | ダッシュボード | スタジオ別絞り込み | ホスト | `/api/host/status` | GET  | 指定スタジオに限定して稼働状況を取得する      | HostStatusController |
| H-36 | ダッシュボード | 月別稼働状況取得  | ホスト | `/api/host/status` | GET  | 指定年・月の稼働状況を取得する           | HostStatusController |

---

# 15. 管理者向け会員管理

|  No. | 機能分類 | 機能名       | 利用者 | API                         | HTTP | 概要                       | 主なController         |
| ---: | ---- | --------- | --- | --------------------------- | ---- | ------------------------ | -------------------- |
| A-01 | 会員管理 | 会員一覧取得    | 管理者 | `/api/admin/users`          | GET  | システムに登録されている会員を一覧取得する    | AdminUsersController |
| A-02 | 会員管理 | 会員検索      | 管理者 | `/api/admin/users`          | GET  | キーワードを指定して会員を検索する        | AdminUsersController |
| A-03 | 会員管理 | 会員一覧ページング | 管理者 | `/api/admin/users`          | GET  | ページ番号と表示件数を指定して会員一覧を取得する | AdminUsersController |
| A-04 | 会員管理 | 会員詳細取得    | 管理者 | `/api/admin/users/{userId}` | GET  | 指定した会員の詳細情報を取得する         | AdminUsersController |

---

# 16. 管理者向けスタジオ管理

|  No. | 機能分類   | 機能名        | 利用者  | API                             | HTTP     | 概要                       | 主なController         |
| ---: | ------ | ---------- | ---- | ------------------------------- | -------- | ------------------------ | -------------------- |
| A-05 | スタジオ管理 | スタジオ一覧取得   | 管理者  | `/api/admin/rooms`              | GET      | 全スタジオを一覧取得する             | AdminRoomsController |
| A-06 | スタジオ管理 | スタジオ検索     | 管理者  | `/api/admin/rooms`              | GET      | キーワードでスタジオを検索する          | AdminRoomsController |
| A-07 | スタジオ管理 | ホスト選択肢取得   | 管理者  | `/api/admin/rooms/host-options` | GET      | スタジオ登録・編集時に選択可能なホストを取得する | AdminRoomsController |
| A-08 | スタジオ管理 | スタジオ詳細取得   | 管理者  | `/api/admin/rooms/{roomId}`     | GET      | 指定スタジオの詳細情報を取得する         | AdminRoomsController |
| A-09 | スタジオ管理 | スタジオ登録     | 管理者  | `/api/admin/rooms`              | POST     | 管理者がホストを指定してスタジオを登録する    | AdminRoomsController |
| A-10 | スタジオ管理 | スタジオ更新     | 管理者  | `/api/admin/rooms/{roomId}`     | PUT      | スタジオの基本情報を更新する           | AdminRoomsController |
| A-11 | 入力検証   | 重複スタジオチェック | 管理者  | `/api/admin/rooms`              | POST・PUT | 同一スタジオ名と住所の重複登録を防止する     | AdminRoomsController |
| A-12 | 入力検証   | ホスト存在チェック  | 管理者  | `/api/admin/rooms`              | POST・PUT | 有効なホストが指定されていることを確認する    | AdminRoomsController |
| A-13 | 監査     | スタジオ登録ログ記録 | システム | ―                               | ―        | 管理者によるスタジオ登録を監査ログへ記録する   | AdminRoomsController |
| A-14 | 監査     | スタジオ更新ログ記録 | システム | ―                               | ―        | 管理者によるスタジオ更新を監査ログへ記録する   | AdminRoomsController |

---

# 17. 管理者向け予約管理

|  No. | 機能分類 | 機能名       | 利用者 | API                       | HTTP | 概要                     | 主なController                |
| ---: | ---- | --------- | --- | ------------------------- | ---- | ---------------------- | --------------------------- |
| A-15 | 予約管理 | 全予約一覧取得   | 管理者 | `/api/admin/reservations` | GET  | システム内の予約を横断的に一覧取得する    | AdminReservationsController |
| A-16 | 予約管理 | 予約キーワード検索 | 管理者 | `/api/admin/reservations` | GET  | 会員名、スタジオ名などのキーワードで検索する | AdminReservationsController |
| A-17 | 予約管理 | 予約状態絞り込み  | 管理者 | `/api/admin/reservations` | GET  | 予約状態を指定して一覧を絞り込む       | AdminReservationsController |
| A-18 | 予約管理 | 予約ID検索    | 管理者 | `/api/admin/reservations` | GET  | 予約IDを指定して予約を検索する       | AdminReservationsController |
| A-19 | 予約管理 | スタジオ別検索   | 管理者 | `/api/admin/reservations` | GET  | スタジオIDを指定して予約を検索する     | AdminReservationsController |
| A-20 | 予約管理 | 利用期間検索    | 管理者 | `/api/admin/reservations` | GET  | 開始日時の範囲を指定して予約を検索する    | AdminReservationsController |

---

# 18. 管理者向け稼働状況・設定管理

|  No. | 機能分類    | 機能名         | 利用者  | API                   | HTTP | 概要                        | 主なController            |
| ---: | ------- | ----------- | ---- | --------------------- | ---- | ------------------------- | ----------------------- |
| A-21 | ダッシュボード | 全体稼働状況取得    | 管理者  | `/api/admin/status`   | GET  | システム全体または指定スタジオの稼働状況を取得する | AdminStatusController   |
| A-22 | ダッシュボード | スタジオ別稼働状況取得 | 管理者  | `/api/admin/status`   | GET  | 指定スタジオに限定して稼働状況を取得する      | AdminStatusController   |
| A-23 | ダッシュボード | 月別稼働状況取得    | 管理者  | `/api/admin/status`   | GET  | 指定した年・月の稼働状況を取得する         | AdminStatusController   |
| A-24 | システム設定  | 管理設定取得      | 管理者  | `/api/admin/settings` | GET  | 税率、管理手数料率などの設定値を取得する      | AdminSettingsController |
| A-25 | システム設定  | 管理設定更新      | 管理者  | `/api/admin/settings` | PUT  | 税率、管理手数料率などの設定値を更新する      | AdminSettingsController |
| A-26 | 監査      | 管理設定更新ログ記録  | システム | ―                     | ―    | 管理設定の変更内容を監査ログへ記録する       | AdminSettingsController |

---

# 19. 管理者向けログ管理

|  No. | 機能分類   | 機能名          | 利用者 | API                         | HTTP | 概要                                | 主なController                |
| ---: | ------ | ------------ | --- | --------------------------- | ---- | --------------------------------- | --------------------------- |
| A-27 | 監査ログ   | 監査ログ一覧取得     | 管理者 | `/api/admin/logs`           | GET  | システム内で実行された操作履歴を一覧取得する            | AdminAuditLogsController    |
| A-28 | 監査ログ   | 監査ログキーワード検索  | 管理者 | `/api/admin/logs`           | GET  | ログ内容をキーワード検索する                    | AdminAuditLogsController    |
| A-29 | 監査ログ   | 操作者絞り込み      | 管理者 | `/api/admin/logs`           | GET  | 操作者IDを指定して監査ログを絞り込む               | AdminAuditLogsController    |
| A-30 | 監査ログ   | 操作種別絞り込み     | 管理者 | `/api/admin/logs`           | GET  | LOGIN、CREATE、UPDATEなどの操作種別で絞り込む   | AdminAuditLogsController    |
| A-31 | 監査ログ   | 対象エンティティ絞り込み | 管理者 | `/api/admin/logs`           | GET  | User、Room、Reservationなどの対象種別で絞り込む | AdminAuditLogsController    |
| A-32 | 監査ログ   | 対象ID絞り込み     | 管理者 | `/api/admin/logs`           | GET  | 対象データのIDを指定して監査ログを検索する            | AdminAuditLogsController    |
| A-33 | 監査ログ   | 操作日時範囲検索     | 管理者 | `/api/admin/logs`           | GET  | 開始日時と終了日時を指定してログを検索する             | AdminAuditLogsController    |
| A-34 | AI検索ログ | AI検索ログ一覧取得   | 管理者 | `/api/admin/ai-search-logs` | GET  | AIスタジオ検索の実行履歴を一覧取得する              | AdminAiSearchLogsController |
| A-35 | AI検索ログ | 検索文検索        | 管理者 | `/api/admin/ai-search-logs` | GET  | AIへ入力された検索文をキーワード検索する             | AdminAiSearchLogsController |
| A-36 | AI検索ログ | 利用者絞り込み      | 管理者 | `/api/admin/ai-search-logs` | GET  | ユーザーIDを指定してAI検索履歴を絞り込む            | AdminAiSearchLogsController |
| A-37 | AI検索ログ | IPアドレス絞り込み   | 管理者 | `/api/admin/ai-search-logs` | GET  | 接続元IPアドレスを指定して検索履歴を絞り込む           | AdminAiSearchLogsController |
| A-38 | AI検索ログ | 成功可否絞り込み     | 管理者 | `/api/admin/ai-search-logs` | GET  | AI検索の成功・失敗状態で履歴を絞り込む              | AdminAiSearchLogsController |
| A-39 | AI検索ログ | AIモデル絞り込み    | 管理者 | `/api/admin/ai-search-logs` | GET  | 使用されたAIモデル名で履歴を絞り込む               | AdminAiSearchLogsController |
| A-40 | AI検索ログ | 実行日時範囲検索     | 管理者 | `/api/admin/ai-search-logs` | GET  | AI検索の実行日時範囲を指定して検索する              | AdminAiSearchLogsController |

---

# 20. 認証・認可一覧

| 対象             | 認証・認可条件               |
| -------------- | --------------------- |
| ホーム情報          | 認証不要                  |
| スタジオ検索・詳細      | 認証不要                  |
| AIスタジオ検索       | 認証不要。ただしレート制限あり       |
| AIレビュー補助       | 認証必須                  |
| 会員情報取得・更新      | 認証必須                  |
| 予約確認・予約履歴      | 認証必須かつGeneralUserロール  |
| レビュー投稿         | 認証必須                  |
| ホスト管理機能        | 認証必須かつHostロール         |
| 管理者機能          | 認証必須かつAdminロール        |
| Stripe Webhook | JWT認証なし。Stripe署名による検証 |

---

# 21. 外部サービス連携

| 外部サービス     | 利用目的            | 関連機能                                              |
| ---------- | --------------- | ------------------------------------------------- |
| Stripe     | 予約料金のオンライン決済    | Checkout Session作成、PaymentIntent取得、Webhook受信、予約確定 |
| OpenAI系API | 自然文によるスタジオ検索    | AIスタジオ検索                                          |
| OpenAI系API | レビュー文章作成の補助     | AIレビュー補助                                          |
| メール送信サービス  | 新規会員登録時の認証メール送信 | 会員登録                                              |
| メール送信サービス  | パスワード再設定メール送信   | パスワード再設定                                          |
| QuestPDF   | 売上明細・請求書PDFの生成  | ホスト売上PDF出力                                        |

---

# 22. 出力機能

| 出力形式 | 出力内容                | API                                           |
| ---- | ------------------- | --------------------------------------------- |
| CSV  | ホスト売上一覧             | `/api/host/sales.csv`                         |
| CSV  | 予約単位の料金明細           | `/api/host/sales/{reservationId}/items.csv`   |
| PDF  | 予約単位の売上明細・請求書       | `/api/host/sales/{reservationId}/invoice.pdf` |
| JSON | 各画面・機能で使用するAPIレスポンス | 各API                                          |

---

# 23. Controller一覧

| No. | Controller                  | 主な責務                    |
| --: | --------------------------- | ----------------------- |
|   1 | HomeController              | ホーム情報取得                 |
|   2 | AuthController              | ログイン、会員登録、会員情報、パスワード再設定 |
|   3 | RoomsController             | 一般向けスタジオ検索・詳細           |
|   4 | ReservationsController      | 一般ユーザー向け予約確認・予約履歴       |
|   5 | ReviewsController           | 一般ユーザー向けレビュー投稿          |
|   6 | AiRoomSearchController      | AIスタジオ検索                |
|   7 | AiReviewAssistController    | AIレビュー文章補助              |
|   8 | StripeWebhookController     | Stripe決済完了通知処理          |
|   9 | HostRoomsController         | ホスト所有スタジオ一覧・詳細          |
|  10 | HostBusinessHoursController | 営業時間管理                  |
|  11 | HostClosuresController      | 休業日管理                   |
|  12 | HostPriceRulesController    | 料金ルール管理                 |
|  13 | HostReservationsController  | ホスト予約管理                 |
|  14 | HostReviewsController       | ホストレビュー管理               |
|  15 | HostSalesController         | 売上一覧・詳細                 |
|  16 | HostSalesCsvController      | 売上一覧CSV出力               |
|  17 | HostSalesExportController   | 売上明細CSV・請求書PDF出力        |
|  18 | HostStatusController        | ホスト向け稼働状況               |
|  19 | AdminUsersController        | 管理者向け会員管理               |
|  20 | AdminRoomsController        | 管理者向けスタジオ管理             |
|  21 | AdminReservationsController | 管理者向け予約管理               |
|  22 | AdminStatusController       | 管理者向け稼働状況               |
|  23 | AdminSettingsController     | システム設定管理                |
|  24 | AdminAuditLogsController    | 監査ログ閲覧                  |
|  25 | AdminAiSearchLogsController | AI検索ログ閲覧                |

---

# 24. 現時点の実装範囲に関する補足

本一覧はControllerから確認できる外部公開機能を中心に整理している。

以下はControllerだけでは詳細を確定できないため、Service、DTO、Entity、AppDbContext、Program.csなどを確認して詳細設計書で補完する。

* 予約料金の具体的な計算式
* 料金ルールの優先順位
* 予約可能時間の判定条件
* 休業日と予約済み時間の重複判定
* Stripe決済失敗・キャンセル時の処理
* メール認証トークンの有効期限
* パスワード再設定トークンの有効期限
* AI検索のプロンプトおよびモデル設定
* AI検索レート制限の具体的な回数
* レビュー投稿可能条件
* 管理画面での会員停止・ロール変更機能の有無
* 監査ログの保存期間
* CSV・PDFの正式な帳票仕様
* 例外の共通処理方針
* トランザクション境界
* データベース制約
