export default function SignupVerifiedPage() {
  return (
    <main className="mx-auto flex min-h-screen max-w-3xl items-center justify-center px-6">
      <div className="w-full rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
        <h1 className="mb-4 text-2xl font-bold text-slate-800">
          メール認証が完了しました
        </h1>

        <p className="mb-6 text-sm leading-7 text-slate-600">
          アカウントの有効化が完了しました。<br />
          ログイン画面からサインインしてください。
        </p>

        <div className="flex gap-3">
          <a
            href="/login"
            className="rounded-xl bg-sky-700 px-5 py-3 text-sm font-medium text-white hover:opacity-90"
          >
            ログイン画面へ
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