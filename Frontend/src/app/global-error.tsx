"use client";

import Link from "next/link";

export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <html lang="ja">
      <body className="min-h-screen bg-stone-50 text-slate-800">
        <main className="mx-auto w-full max-w-5xl px-4 py-10 md:py-14">
          <div className="rounded-3xl border border-stone-200 bg-white p-6 shadow-sm md:p-8">
            <p className="text-sm font-semibold tracking-[0.2em] text-sky-700">
              ERROR
            </p>
            <h1 className="mt-3 text-3xl font-semibold text-slate-800 md:text-4xl">
              500 Internal Server Error
            </h1>
            <p className="mt-3 text-sm leading-7 text-slate-500 md:text-base">
              処理中に予期しないエラーが発生しました。時間をおいて再度お試しください。
            </p>

            <div className="mt-6 rounded-2xl border border-stone-200 bg-stone-50 p-5 text-sm text-slate-700">
              <div>
                <span className="font-semibold">Message:</span>{" "}
                {error?.message || "Unexpected error"}
              </div>
            </div>

            <div className="mt-6 flex flex-wrap gap-3">
              <button
                type="button"
                onClick={() => reset()}
                className="rounded-xl bg-sky-700 px-5 py-3 text-sm font-medium text-white hover:opacity-90"
              >
                再試行
              </button>

              <Link
                href="/"
                className="rounded-xl border border-stone-300 px-5 py-3 text-sm font-medium text-slate-700 hover:border-sky-300 hover:text-sky-700"
              >
                ホームに戻る
              </Link>
            </div>
          </div>
        </main>
      </body>
    </html>
  );
}