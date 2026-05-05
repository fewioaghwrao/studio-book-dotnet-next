"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { apiFetch } from "@/lib/apiFetch";

type CurrentUserResponse = {
  isAuthenticated: boolean;
  user?: {
    id: number;
    name: string;
    kana: string;
    email: string;
    postalCode: string;
    address: string;
    phoneNumber: string;
    usageType: string;
    roles: string[];
  };
};

type FormState = {
  name: string;
  kana: string;
  postalCode: string;
  address: string;
  phoneNumber: string;
  email: string;
};

const initialFormState: FormState = {
  name: "",
  kana: "",
  postalCode: "",
  address: "",
  phoneNumber: "",
  email: "",
};

export default function UserEditPage() {
  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [form, setForm] = useState<FormState>(initialFormState);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  useEffect(() => {
    let ignore = false;

    const fetchUser = async () => {
      try {
        const response = await apiFetch(`${apiBaseUrl}/api/auth/me`, {
          method: "GET",          cache: "no-store",
        });

        if (response.status === 401) {
          window.location.href = "/auth/login";
          return;
        }

        if (!response.ok) {
          setErrorMessage("会員情報の取得に失敗しました。");
          return;
        }

        const data = (await response.json()) as CurrentUserResponse;

        if (ignore) return;

        if (!data.isAuthenticated || !data.user) {
          window.location.href = "/auth/login";
          return;
        }

        setForm({
          name: data.user.name ?? "",
          kana: data.user.kana ?? "",
          postalCode: data.user.postalCode ?? "",
          address: data.user.address ?? "",
          phoneNumber: data.user.phoneNumber ?? "",
          email: data.user.email ?? "",
        });
      } catch {
        if (!ignore) {
          setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
        }
      } finally {
        if (!ignore) {
          setIsLoading(false);
        }
      }
    };

    fetchUser();

    return () => {
      ignore = true;
    };
  }, [apiBaseUrl]);

  const updateField = (key: keyof FormState, value: string) => {
    setForm((prev) => ({
      ...prev,
      [key]: value,
    }));
  };

  const validate = () => {
    if (!form.name.trim()) return "氏名を入力してください。";
    if (!form.kana.trim()) return "フリガナを入力してください。";
    if (!form.postalCode.trim()) return "郵便番号を入力してください。";
    if (!form.address.trim()) return "住所を入力してください。";
    if (!form.phoneNumber.trim()) return "電話番号を入力してください。";
    if (!form.email.trim()) return "メールアドレスを入力してください。";
    if (!form.email.includes("@")) return "メールアドレスの形式を確認してください。";
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
      setIsSaving(true);

      const response = await apiFetch(`${apiBaseUrl}/api/auth/me`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },        body: JSON.stringify(form),
      });

      if (response.status === 401) {
        window.location.href = "/auth/login";
        return;
      }

      if (!response.ok) {
        let message = "会員情報の更新に失敗しました。";

        try {
          const data = (await response.json()) as { message?: string };
          if (data?.message) {
            message = data.message;
          }
        } catch {
          // 既定メッセージを使用
        }

        setErrorMessage(message);
        return;
      }

      setSuccessMessage("会員情報を更新しました。");

      setTimeout(() => {
        window.location.href = "/user";
      }, 600);
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-12">
        <p className="text-sm text-slate-500">会員情報を読み込み中...</p>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-3xl px-4 py-10">
      <nav className="mb-6 text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <Link href="/user" className="hover:text-sky-700">
          会員情報
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">会員情報編集</span>
      </nav>

      <section className="rounded-[28px] border border-stone-200 bg-white p-6 shadow-sm md:p-8">
        <div className="mb-6 text-center">
          <p className="text-sm font-medium tracking-[0.2em] text-sky-700">
            EDIT PROFILE
          </p>
          <h1 className="mt-2 text-2xl font-semibold text-slate-800">
            会員情報編集
          </h1>
          <p className="mt-2 text-sm text-slate-500">
            登録されている会員情報を変更できます。
          </p>
        </div>

        {errorMessage && (
          <div className="mb-5 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
            {errorMessage}
          </div>
        )}

        {successMessage && (
          <div className="mb-5 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
            {successMessage}
          </div>
        )}

        <form className="space-y-5" onSubmit={handleSubmit}>
<TextInput
  id="name"
  label="氏名"
  required
  value={form.name}
  onChange={(value) => updateField("name", value)}
  placeholder="侍 太郎"
  autoComplete="name"
/>

<TextInput
  id="kana"
  label="フリガナ"
  required
  value={form.kana}
  onChange={(value) => updateField("kana", value)}
  placeholder="サムライ タロウ"
/>

<TextInput
  id="postalCode"
  label="郵便番号"
  required
  value={form.postalCode}
  onChange={(value) => updateField("postalCode", value)}
  placeholder="101-0022"
  autoComplete="postal-code"
/>

<TextInput
  id="address"
  label="住所"
  required
  value={form.address}
  onChange={(value) => updateField("address", value)}
  placeholder="東京都千代田区神田練塀町300番地"
  autoComplete="street-address"
/>

<TextInput
  id="phoneNumber"
  label="電話番号"
  required
  value={form.phoneNumber}
  onChange={(value) => updateField("phoneNumber", value)}
  placeholder="090-1234-5678"
  autoComplete="tel-national"
/>

<TextInput
  id="email"
  label="メールアドレス"
  required
  type="email"
  value={form.email}
  onChange={(value) => updateField("email", value)}
  placeholder="taro.samurai@example.com"
  autoComplete="email"
/>

          <div className="flex flex-col-reverse gap-3 pt-4 sm:flex-row sm:justify-center">
            <Link
              href="/user"
              className="inline-flex justify-center rounded-xl border border-stone-300 px-6 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
            >
              戻る
            </Link>

            <button
              type="submit"
              disabled={isSaving}
              className="inline-flex justify-center rounded-xl bg-sky-700 px-8 py-3 text-sm font-medium text-white transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSaving ? "更新中..." : "更新"}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}

function TextInput({
  id,
  label,
  value,
  onChange,
  placeholder,
  type = "text",
  required = false,
  autoComplete,
}: {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  type?: string;
  required?: boolean;
  autoComplete?: string;
}) {
  return (
    <div className="grid gap-2 md:grid-cols-[160px_1fr] md:items-center">
      <label htmlFor={id} className="text-sm font-semibold text-slate-700">
        <span>{label}</span>
        {required && (
          <span className="ml-2 rounded-full bg-rose-100 px-2 py-0.5 text-xs font-medium text-rose-700">
            必須
          </span>
        )}
      </label>

      <input
        id={id}
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        autoComplete={autoComplete}
        className="w-full rounded-xl border border-stone-300 px-4 py-3 text-sm outline-none transition focus:border-sky-500"
      />
    </div>
  );
}