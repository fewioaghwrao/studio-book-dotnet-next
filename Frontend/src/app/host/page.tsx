"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
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

export default function HostPage() {
  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [user, setUser] = useState<CurrentUserResponse["user"] | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

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

        setUser(data.user);
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

  if (isLoading) {
    return (
      <div className="mx-auto max-w-4xl px-4 py-12">
        <p className="text-sm text-slate-500">会員情報を読み込み中...</p>
      </div>
    );
  }

  if (errorMessage) {
    return (
      <div className="mx-auto max-w-4xl px-4 py-12">
        <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
          {errorMessage}
        </div>
      </div>
    );
  }

  if (!user) {
    return null;
  }

  const rows = [
    { label: "氏名", value: user.name },
    { label: "フリガナ", value: user.kana },
    { label: "郵便番号", value: user.postalCode },
    { label: "住所", value: user.address },
    { label: "電話番号", value: user.phoneNumber },
    { label: "メールアドレス", value: user.email },
  ];

  return (
    <div className="mx-auto max-w-4xl px-4 py-10">
      <nav className="mb-6 text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">会員情報（ホスト）</span>
      </nav>

      <section className="rounded-[28px] border border-stone-200 bg-white p-6 shadow-sm md:p-8">
        <div className="mb-6 flex items-start justify-between gap-4">
          <div>
            <p className="text-sm font-medium tracking-[0.2em] text-sky-700">
              HOST MY PAGE
            </p>
            <h1 className="mt-2 text-2xl font-semibold text-slate-800">
              会員情報（ホスト）
            </h1>
            <p className="mt-2 text-sm text-slate-500">
              スタジオ提供者として登録されている会員情報を確認できます。
            </p>
          </div>

          <Link
            href="/host/edit"
            className="rounded-xl border border-stone-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
          >
            編集
          </Link>
        </div>

        <div className="divide-y divide-stone-200">
          {rows.map((row) => (
            <div
              key={row.label}
              className="grid gap-2 py-4 md:grid-cols-[160px_1fr]"
            >
              <div className="text-sm font-semibold text-slate-700">
                {row.label}
              </div>
              <div className="text-sm text-slate-700">
                {row.value || "未登録"}
              </div>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}