export default function SignupVerifyErrorPage() {
  return (
    <main className="mx-auto flex min-h-screen max-w-3xl items-center justify-center px-6">
      <div className="w-full rounded-2xl border border-red-200 bg-white p-8 shadow-sm">
        <h1 className="mb-4 text-2xl font-bold text-red-700">
          メール認証に失敗しました
        </h1>

        <p className="mb-6 text-sm leading-7 text-slate-600">
          認証URLが無効、または有効期限切れの可能性があります。<br />
          もう一度会員登録を行うか、認証メールの再送機能をご利用ください。
        </p>

        <div className="flex gap-3">
          <a
            href="/signup"
            className="rounded-xl bg-sky-700 px-5 py-3 text-sm font-medium text-white hover:opacity-90"
          >
            会員登録へ
          </a>

          <a
            href="/"
            className="rounded-xl border border-slate-300 px-5 py-3 text-sm font-medium text-slate-700 hover:bg-slate-50"
          >
            トップへ戻る
          </a>
        </div>
      </div>
    </main>
  );
}