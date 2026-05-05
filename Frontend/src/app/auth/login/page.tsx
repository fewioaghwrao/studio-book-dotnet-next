"use client";

import Link from "next/link";
import { FormEvent, useMemo, useState } from "react";

type LoginResponse = {
  message?: string;
  token?: string;
  redirectTo?: string;
};

export default function LoginPage() {
  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setErrorMessage("");
    setSuccessMessage("");

    if (!email.trim()) {
      setErrorMessage("メールアドレスを入力してください。");
      return;
    }

    if (!password.trim()) {
      setErrorMessage("パスワードを入力してください。");
      return;
    }

    try {
      setIsLoading(true);

const response = await fetch(`${apiBaseUrl}/api/auth/login`, {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
  },
  body: JSON.stringify({
    email,
    password,
  }),
});

      if (!response.ok) {
        let message = "メールアドレスまたはパスワードが正しくありません。";

        try {
          const data = (await response.json()) as { message?: string };
          if (data?.message) {
            message = data.message;
          }
        } catch {
          // JSONでなくても既定メッセージを使用
        }

        setErrorMessage(message);
        return;
      }

const data = (await response.json().catch(() => ({}))) as LoginResponse;

if (!data.token) {
  setErrorMessage("ログイン情報の取得に失敗しました。");
  return;
}

localStorage.setItem("token", data.token);

setSuccessMessage(data?.message ?? "ログインに成功しました。");

window.location.href = data.redirectTo ?? "/";
    } catch (error) {
      console.error(error);
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main className="relative overflow-hidden">
      <div className="absolute inset-0">
        <img
          src="/images/main1.jpg"
          alt="背景"
          className="h-full w-full object-cover"
        />
        <div className="absolute inset-0 bg-slate-900/55" />
      </div>

      <div className="relative mx-auto flex min-h-[calc(100vh-140px)] max-w-6xl items-center px-4 py-10">
        <div className="grid w-full items-center gap-10 lg:grid-cols-[1.1fr_460px]">
          <section className="hidden text-white lg:block">
            <p className="text-sm font-medium tracking-[0.2em] text-white/80">
              LOGIN
            </p>
            <h1 className="mt-4 text-4xl font-semibold leading-relaxed xl:text-5xl">
              Studio Book にログインして、
              <br />
              予約管理をもっとスムーズに。
            </h1>
            <p className="mt-6 max-w-xl text-sm leading-7 text-white/85 md:text-base">
              お気に入りスタジオの確認、予約情報の管理、
              会員向け機能の利用のためのログイン画面です。
            </p>

            <div className="mt-8 grid max-w-xl gap-4 sm:grid-cols-2">
              <div className="rounded-2xl border border-white/15 bg-white/10 p-4 backdrop-blur-sm">
                <div className="text-sm font-semibold">簡単ログイン</div>
                <div className="mt-2 text-sm text-white/80">
                  メールアドレスとパスワードで利用できます。
                </div>
              </div>
              <div className="rounded-2xl border border-white/15 bg-white/10 p-4 backdrop-blur-sm">
                <div className="text-sm font-semibold">会員機能にアクセス</div>
                <div className="mt-2 text-sm text-white/80">
                  お気に入りや予約確認などをまとめて利用できます。
                </div>
              </div>
            </div>
          </section>

          <section className="mx-auto w-full max-w-md">
            <div className="rounded-[28px] border border-white/20 bg-white/95 p-6 shadow-2xl backdrop-blur md:p-8">
              <div className="mb-6 text-center">
                <h2 className="text-2xl font-semibold text-slate-800">
                  ログイン
                </h2>
                <p className="mt-2 text-sm text-slate-500">
                  登録済みのメールアドレスとパスワードを入力してください。
                </p>
              </div>

              <form className="space-y-5" onSubmit={handleSubmit}>
                <div>
                  <label
                    htmlFor="email"
                    className="mb-2 block text-sm font-medium text-slate-700"
                  >
                    メールアドレス
                  </label>
                  <input
                    id="email"
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    placeholder="sample@example.com"
                    className="w-full rounded-xl border border-stone-300 px-4 py-3 text-sm outline-none transition focus:border-sky-500"
                    autoComplete="email"
                  />
                </div>

                <div>
                  <div className="mb-2 flex items-center justify-between">
                    <label
                      htmlFor="password"
                      className="block text-sm font-medium text-slate-700"
                    >
                      パスワード
                    </label>
                    <Link
                      href="/forgot-password"
                      className="text-xs text-sky-700 hover:text-sky-800"
                    >
                      パスワードをお忘れですか？
                    </Link>
                  </div>

                  <div className="relative">
                    <input
                      id="password"
                      type={showPassword ? "text" : "password"}
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      placeholder="パスワードを入力"
                      className="w-full rounded-xl border border-stone-300 px-4 py-3 pr-14 text-sm outline-none transition focus:border-sky-500"
                      autoComplete="current-password"
                    />
                    <button
                      type="button"
                      onClick={() => setShowPassword((prev) => !prev)}
                      className="absolute inset-y-0 right-3 my-auto text-xs font-medium text-slate-500 hover:text-sky-700"
                    >
                      {showPassword ? "非表示" : "表示"}
                    </button>
                  </div>
                </div>

                {errorMessage && (
                  <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
                    {errorMessage}
                  </div>
                )}

                {successMessage && (
                  <div className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
                    {successMessage}
                  </div>
                )}

                <button
                  type="submit"
                  disabled={isLoading}
                  className="w-full rounded-xl bg-sky-700 px-4 py-3 text-sm font-medium text-white transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {isLoading ? "ログイン中..." : "ログイン"}
                </button>
              </form>

              <div className="mt-6 border-t border-stone-200 pt-5 text-center">
                <p className="text-sm text-slate-500">
                  アカウントをお持ちでない方
                </p>
                <Link
                  href="/signup"
                  className="mt-3 inline-flex rounded-xl border border-stone-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
                >
                  会員登録はこちら
                </Link>
              </div>
            </div>
          </section>
        </div>
      </div>
    </main>
  );
}