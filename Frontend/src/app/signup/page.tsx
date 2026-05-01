"use client";

import Link from "next/link";
import { FormEvent, useMemo, useState } from "react";

type SignupResponse = {
  success?: boolean;
  message?: string;
};

type UsageType = "personal" | "business";

export default function SignupPage() {
  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [name, setName] = useState("");
  const [kana, setKana] = useState("");
  const [email, setEmail] = useState("");
  const [postalCode, setPostalCode] = useState("");
  const [address, setAddress] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [usageType, setUsageType] = useState<UsageType>("personal");
  const [password, setPassword] = useState("");
  const [passwordConfirmation, setPasswordConfirmation] = useState("");

  const [showPassword, setShowPassword] = useState(false);
  const [showPasswordConfirmation, setShowPasswordConfirmation] =
    useState(false);

  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const validate = () => {
    if (!name.trim()) {
      return "氏名を入力してください。";
    }

    if (!kana.trim()) {
      return "フリガナを入力してください。";
    }

    if (!email.trim()) {
      return "メールアドレスを入力してください。";
    }

    if (!postalCode.trim()) {
      return "郵便番号を入力してください。";
    }

    if (!address.trim()) {
      return "住所を入力してください。";
    }

    if (!phoneNumber.trim()) {
      return "電話番号を入力してください。";
    }

    if (!usageType.trim()) {
      return "利用区分を選択してください。";
    }

    if (!password.trim()) {
      return "パスワードを入力してください。";
    }

    if (password.length < 8) {
      return "パスワードは8文字以上で入力してください。";
    }

    if (!passwordConfirmation.trim()) {
      return "確認用パスワードを入力してください。";
    }

    if (password !== passwordConfirmation) {
      return "パスワードが一致しません。";
    }

    return "";
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setErrorMessage("");
    setSuccessMessage("");

    const validationMessage = validate();
    if (validationMessage) {
      setErrorMessage(validationMessage);
      return;
    }

    try {
      setIsLoading(true);

      const response = await fetch(`${apiBaseUrl}/api/auth/signup`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        credentials: "include",
        body: JSON.stringify({
          name: name.trim(),
          kana: kana.trim(),
          email: email.trim(),
          postalCode: postalCode.trim(),
          address: address.trim(),
          phoneNumber: phoneNumber.trim(),
          usageType,
          password,
          passwordConfirmation,
        }),
      });

      if (!response.ok) {
        let message = "会員登録に失敗しました。入力内容をご確認ください。";

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

      const data = (await response.json().catch(() => ({}))) as SignupResponse;
      setSuccessMessage(
        data?.message ??
          "認証メールを送信しました。メール内のリンクから登録を完了してください。"
      );

      setName("");
      setKana("");
      setEmail("");
      setPostalCode("");
      setAddress("");
      setPhoneNumber("");
      setUsageType("personal");
      setPassword("");
      setPasswordConfirmation("");
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
          src="/images/main2.jpg"
          alt="背景"
          className="h-full w-full object-cover"
        />
        <div className="absolute inset-0 bg-slate-950/60" />
      </div>

      <div className="relative mx-auto flex min-h-[calc(100vh-140px)] max-w-6xl items-center px-4 py-10">
        <div className="grid w-full items-center gap-10 lg:grid-cols-[1.05fr_560px]">
          <section className="hidden text-white lg:block">
            <p className="text-sm font-medium tracking-[0.2em] text-white/80">
              SIGN UP
            </p>
            <h1 className="mt-4 text-4xl font-semibold leading-relaxed xl:text-5xl">
              Studio Book の会員登録で、
              <br />
              スタジオ利用をもっと便利に。
            </h1>
            <p className="mt-6 max-w-xl text-sm leading-7 text-white/85 md:text-base">
              会員登録後、認証メールのリンクを開くことで登録が完了します。
              お気に入り登録や予約確認など、会員向け機能をご利用いただけます。
            </p>

            <div className="mt-8 grid max-w-xl gap-4 sm:grid-cols-2">
              <div className="rounded-2xl border border-white/15 bg-white/10 p-4 backdrop-blur-sm">
                <div className="text-sm font-semibold">必要情報をまとめて登録</div>
                <div className="mt-2 text-sm text-white/80">
                  氏名、住所、電話番号などを入力して会員登録できます。
                </div>
              </div>
              <div className="rounded-2xl border border-white/15 bg-white/10 p-4 backdrop-blur-sm">
                <div className="text-sm font-semibold">メール認証対応</div>
                <div className="mt-2 text-sm text-white/80">
                  認証メールから本登録を完了する安全なフローです。
                </div>
              </div>
            </div>
          </section>

          <section className="mx-auto w-full max-w-2xl">
            <div className="rounded-[28px] border border-white/20 bg-white/95 p-6 shadow-2xl backdrop-blur md:p-8">
              <div className="mb-6 text-center">
                <h2 className="text-2xl font-semibold text-slate-800">
                  会員登録
                </h2>
                <p className="mt-2 text-sm text-slate-500">
                  必要事項を入力してアカウントを作成してください。
                </p>
              </div>

              <div className="mb-6 rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm leading-6 text-amber-800">
  <p className="font-semibold">公開デモについて</p>
  <p className="mt-1">
    この画面はポートフォリオ用のデモ環境です。実在する個人情報・メールアドレス・電話番号は入力しないでください。
  </p>
</div>

              <form className="space-y-5" onSubmit={handleSubmit}>
                <div className="grid gap-5 md:grid-cols-2">
                  <div>
                    <label
                      htmlFor="name"
                      className="mb-2 block text-sm font-medium text-slate-700"
                    >
                      氏名
                    </label>
                    <input
                      id="name"
                      type="text"
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                      placeholder="サンプル 太郎"
                      className="w-full rounded-xl border border-stone-300 px-4 py-3 text-sm outline-none transition focus:border-sky-500"
                      autoComplete="name"
                    />
                  </div>

                  <div>
                    <label
                      htmlFor="kana"
                      className="mb-2 block text-sm font-medium text-slate-700"
                    >
                      フリガナ
                    </label>
                    <input
                      id="kana"
                      type="text"
                      value={kana}
                      onChange={(e) => setKana(e.target.value)}
                      placeholder="サンプル タロウ"
                      className="w-full rounded-xl border border-stone-300 px-4 py-3 text-sm outline-none transition focus:border-sky-500"
                    />
                  </div>
                </div>

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

                <div className="grid gap-5 md:grid-cols-[180px_1fr]">
                  <div>
                    <label
                      htmlFor="postalCode"
                      className="mb-2 block text-sm font-medium text-slate-700"
                    >
                      郵便番号
                    </label>
                    <input
                      id="postalCode"
                      type="text"
                      value={postalCode}
                      onChange={(e) => setPostalCode(e.target.value)}
                      placeholder="123-4567"
                      className="w-full rounded-xl border border-stone-300 px-4 py-3 text-sm outline-none transition focus:border-sky-500"
                      autoComplete="postal-code"
                    />
                  </div>

                  <div>
                    <label
                      htmlFor="address"
                      className="mb-2 block text-sm font-medium text-slate-700"
                    >
                      住所
                    </label>
                    <input
                      id="address"
                      type="text"
                      value={address}
                      onChange={(e) => setAddress(e.target.value)}
                      placeholder="東京都○○区..."
                      className="w-full rounded-xl border border-stone-300 px-4 py-3 text-sm outline-none transition focus:border-sky-500"
                      autoComplete="street-address"
                    />
                  </div>
                </div>

                <div className="grid gap-5 md:grid-cols-2">
                  <div>
                    <label
                      htmlFor="phoneNumber"
                      className="mb-2 block text-sm font-medium text-slate-700"
                    >
                      電話番号
                    </label>
                    <input
                      id="phoneNumber"
                      type="tel"
                      value={phoneNumber}
                      onChange={(e) => setPhoneNumber(e.target.value)}
                      placeholder="09012345678"
                      className="w-full rounded-xl border border-stone-300 px-4 py-3 text-sm outline-none transition focus:border-sky-500"
                      autoComplete="tel"
                    />
                  </div>

                  <div>
                    <label
                      htmlFor="usageType"
                      className="mb-2 block text-sm font-medium text-slate-700"
                    >
                      利用区分
                    </label>
                    <select
                      id="usageType"
                      value={usageType}
                      onChange={(e) =>
                        setUsageType(e.target.value as UsageType)
                      }
                      className="w-full rounded-xl border border-stone-300 bg-white px-4 py-3 text-sm outline-none transition focus:border-sky-500"
                    >
                      <option value="personal">個人利用</option>
                      <option value="business">法人利用</option>
                    </select>
                  </div>
                </div>

                <div className="grid gap-5 md:grid-cols-2">
                  <div>
                    <label
                      htmlFor="password"
                      className="mb-2 block text-sm font-medium text-slate-700"
                    >
                      パスワード
                    </label>

                    <div className="relative">
                      <input
                        id="password"
                        type={showPassword ? "text" : "password"}
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        placeholder="8文字以上で入力"
                        className="w-full rounded-xl border border-stone-300 px-4 py-3 pr-14 text-sm outline-none transition focus:border-sky-500"
                        autoComplete="new-password"
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

                  <div>
                    <label
                      htmlFor="passwordConfirmation"
                      className="mb-2 block text-sm font-medium text-slate-700"
                    >
                      パスワード（確認用）
                    </label>

                    <div className="relative">
                      <input
                        id="passwordConfirmation"
                        type={showPasswordConfirmation ? "text" : "password"}
                        value={passwordConfirmation}
                        onChange={(e) =>
                          setPasswordConfirmation(e.target.value)
                        }
                        placeholder="確認用パスワードを入力"
                        className="w-full rounded-xl border border-stone-300 px-4 py-3 pr-14 text-sm outline-none transition focus:border-sky-500"
                        autoComplete="new-password"
                      />
                      <button
                        type="button"
                        onClick={() =>
                          setShowPasswordConfirmation((prev) => !prev)
                        }
                        className="absolute inset-y-0 right-3 my-auto text-xs font-medium text-slate-500 hover:text-sky-700"
                      >
                        {showPasswordConfirmation ? "非表示" : "表示"}
                      </button>
                    </div>
                  </div>
                </div>

                {errorMessage && (
                  <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
                    {errorMessage}
                  </div>
                )}

                {successMessage && (
                  <div className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm leading-6 text-emerald-700">
                    {successMessage}
                  </div>
                )}

                <button
                  type="submit"
                  disabled={isLoading}
                  className="w-full rounded-xl bg-sky-700 px-4 py-3 text-sm font-medium text-white transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {isLoading ? "登録中..." : "会員登録"}
                </button>
              </form>

              <div className="mt-6 border-t border-stone-200 pt-5 text-center">
                <p className="text-sm text-slate-500">
                  すでにアカウントをお持ちの方
                </p>
                <Link
                  href="/login"
                  className="mt-3 inline-flex rounded-xl border border-stone-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
                >
                  ログインはこちら
                </Link>
              </div>
            </div>
          </section>
        </div>
      </div>
    </main>
  );
}