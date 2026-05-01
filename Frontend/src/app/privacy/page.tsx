import Link from "next/link";

export default function PrivacyPage() {
  const lastUpdated = "2025-10-23";

  return (
    <main>
      <div className="mx-auto max-w-5xl px-4 py-8 md:py-10">
        <nav aria-label="breadcrumb" className="mb-6 text-sm text-slate-500">
          <ol className="flex flex-wrap items-center gap-2">
            <li>
              <Link href="/" className="hover:text-sky-700">
                ホーム
              </Link>
            </li>
            <li>{">"}</li>
            <li className="text-slate-700">プライバシーポリシー</li>
          </ol>
        </nav>

        <h1 className="text-3xl font-semibold text-slate-800">
          プライバシーポリシー
        </h1>
        <p className="mt-3 text-sm text-slate-500">
          最終更新日：{lastUpdated}
        </p>
        <div className="mt-4 border-t border-stone-200" />

        <div className="mt-8 space-y-8 text-sm leading-7 text-slate-700">
          <section>
            <h2 className="text-xl font-semibold text-slate-800">1. 基本方針</h2>
            <p className="mt-3">
              当運営者は、個人情報保護法等を遵守し、適切に取り扱います。
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold text-slate-800">2. 取得する情報</h2>
            <ul className="mt-3 list-disc space-y-2 pl-6">
              <li>登録情報（氏名、メールアドレス、電話番号 など）</li>
              <li>予約・決済情報（利用日時、金額、決済手段 など）</li>
              <li>アクセスログ、IPアドレス、ブラウザ情報、Cookie など</li>
            </ul>
          </section>

          <section>
            <h2 className="text-xl font-semibold text-slate-800">3. 利用目的</h2>
            <ol className="mt-3 list-decimal space-y-2 pl-6">
              <li>本サービスの提供・運営・本人確認</li>
              <li>予約・決済の確認、連絡対応</li>
              <li>料金請求・返金処理</li>
              <li>問い合わせ対応、サービス改善・新機能開発</li>
              <li>不正利用防止、法令遵守</li>
            </ol>
          </section>

          <section>
            <h2 className="text-xl font-semibold text-slate-800">4. 第三者提供</h2>
            <p className="mt-3">
              本人同意、法令に基づく場合、または委託先への必要な提供を除き、第三者提供は行いません。
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold text-slate-800">5. 委託先の監督</h2>
            <p className="mt-3">
              個人情報の取扱いを委託する場合、適切な監督を行います。
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold text-slate-800">6. 安全管理措置</h2>
            <p className="mt-3">
              アクセス制御、暗号化、ログ監査等の対策を講じます。
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold text-slate-800">7. Cookie・解析</h2>
            <p className="mt-3">
              体験向上・利用分析のためCookieを使用します。設定により無効化できます。
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold text-slate-800">8. 開示・訂正・削除</h2>
            <p className="mt-3">
              ユーザーは当運営者に対し、保有個人データの開示・訂正・削除等を請求できます。
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold text-slate-800">9. お問い合わせ</h2>
            <address className="mt-3 not-italic leading-7">
              N.O.（運営者）
              <br />
              E-mail：
              <a
                href="mailto:info@example.com"
                className="text-sky-700 hover:text-sky-800"
              >
                info@example.com
              </a>
              <br />
              所在地：東京都〇〇区〇〇1-2-3
            </address>
          </section>

          <section>
            <h2 className="text-xl font-semibold text-slate-800">10. 改定</h2>
            <p className="mt-3">
              本ポリシーは必要に応じて改定し、サイト上に公表します。
            </p>
          </section>
        </div>
      </div>
    </main>
  );
}