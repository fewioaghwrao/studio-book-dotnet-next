import Link from "next/link";

export default function TermsPage() {
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
            <li className="text-slate-700">利用規約</li>
          </ol>
        </nav>

        <h1 className="text-3xl font-semibold text-slate-800">利用規約</h1>
        <p className="mt-3 text-sm text-slate-500">
          最終更新日：{lastUpdated}
        </p>
        <div className="mt-4 border-t border-stone-200" />

        <div className="mt-8 space-y-8 text-sm leading-7 text-slate-700">
          <section>
            <h2 className="text-xl font-semibold text-slate-800">第1条（適用）</h2>
            <p className="mt-3">
              本規約は、当運営者が提供するスタジオ時間貸し予約サービス「架空スタジオ予約サービス」（以下「本サービス」）の利用条件を定めるものです。
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold text-slate-800">第2条（定義）</h2>
            <ul className="mt-3 list-disc space-y-2 pl-6">
              <li>「ユーザー」…本サービスを利用するすべての者</li>
              <li>「ホスト」…スタジオを登録・貸出するユーザー</li>
              <li>「ゲスト」…スタジオを予約・利用するユーザー</li>
            </ul>
          </section>

          <section>
            <h2 className="text-xl font-semibold text-slate-800">第3条（登録）</h2>
            <p className="mt-3">
              ユーザーは正確かつ最新の情報を提供するものとし、当運営者は不適当と判断した場合登録を拒否できます。
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold text-slate-800">第4条（料金・支払）</h2>
            <p className="mt-3">
              表示料金を所定の方法（例：クレジットカード）で支払うものとします。キャンセルはキャンセルポリシーに従います。
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold text-slate-800">第5条（禁止事項）</h2>
            <ul className="mt-3 list-disc space-y-2 pl-6">
              <li>法令・公序良俗に反する行為</li>
              <li>無断転貸、設備の破損・汚損</li>
              <li>システムへの不正アクセス 等</li>
            </ul>
          </section>

          <section>
            <h2 className="text-xl font-semibold text-slate-800">第6条（免責）</h2>
            <p className="mt-3">
              当運営者はユーザー間のトラブル等について責任を負いません。不可抗力による停止等についても同様です。
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold text-slate-800">第7条（知的財産）</h2>
            <p className="mt-3">
              本サービスのコンテンツの権利は当運営者または正当な権利者に帰属します。
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold text-slate-800">第8条（規約の変更）</h2>
            <p className="mt-3">
              当運営者は必要に応じて本規約を変更し、本サイト上で告知します。
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold text-slate-800">第9条（準拠法・管轄）</h2>
            <p className="mt-3">
              日本法に準拠し、当運営者所在地を管轄する地方裁判所を第一審の専属的合意管轄とします。
            </p>
          </section>
        </div>
      </div>
    </main>
  );
}