"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useParams } from "next/navigation";

type AdminUserDetail = {
  id: number;
  name: string;
  kana: string;
  postalCode: string;
  address: string;
  phoneNumber: string;
  email: string;
  usageType: string;
  roleName: string;
  roleLabel: string;
  enabled: boolean;
};

function statusLabel(enabled: boolean) {
  return enabled ? "有効" : "無効";
}

function usageTypeLabel(value: string) {
  if (!value) return "未登録";

  switch (value) {
    case "personal":
      return "個人利用";
    case "business":
      return "法人・業務利用";
    default:
      return value;
  }
}

export default function AdminUserDetailPage() {
  const params = useParams<{ userId: string }>();

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [user, setUser] = useState<AdminUserDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    let ignore = false;

    const fetchUser = async () => {
      setIsLoading(true);
      setErrorMessage("");

      try {
        const response = await fetch(
          `${apiBaseUrl}/api/admin/users/${params.userId}`,
          {
            method: "GET",
            credentials: "include",
            cache: "no-store",
          }
        );

        if (response.status === 401) {
          window.location.href = "/auth/login";
          return;
        }

        if (response.status === 403) {
          setErrorMessage("管理者のみアクセスできます。");
          return;
        }

        if (response.status === 404) {
          setErrorMessage("会員情報が見つかりません。");
          return;
        }

        if (!response.ok) {
          setErrorMessage("会員詳細の取得に失敗しました。");
          return;
        }

        const json = (await response.json()) as AdminUserDetail;

        if (!ignore) {
          setUser(json);
        }
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
  }, [apiBaseUrl, params.userId]);

  if (isLoading) {
    return (
      <div className="mx-auto max-w-4xl px-4 py-12">
        <p className="text-sm text-slate-500">会員詳細を読み込み中...</p>
      </div>
    );
  }

  if (errorMessage) {
    return (
      <div className="mx-auto max-w-4xl px-4 py-12">
        <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
          {errorMessage}
        </div>

        <div className="mt-6">
          <Link
            href="/admin/users"
            className="inline-flex rounded-xl border border-stone-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
          >
            会員一覧へ戻る
          </Link>
        </div>
      </div>
    );
  }

  if (!user) {
    return null;
  }

  const rows = [
    { label: "ID", value: String(user.id) },
    { label: "氏名", value: user.name },
    { label: "フリガナ", value: user.kana },
    { label: "郵便番号", value: user.postalCode },
    { label: "住所", value: user.address },
    { label: "電話番号", value: user.phoneNumber },
    { label: "メールアドレス", value: user.email },
    { label: "利用区分", value: usageTypeLabel(user.usageType) },
    { label: "属性", value: user.roleLabel || "未設定" },
  ];

  return (
    <div className="mx-auto max-w-4xl px-4 py-6 sm:py-10">
      <nav className="mb-5 text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <Link href="/admin/users" className="hover:text-sky-700">
          会員一覧
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">会員詳細</span>
      </nav>

      <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
        <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 text-center sm:px-8 sm:py-9">
          <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
            ADMIN USER DETAIL
          </p>
          <h1 className="mt-3 text-xl font-semibold text-slate-800 sm:text-2xl md:text-3xl">
            {user.name}
          </h1>
          <div className="mt-4 flex justify-center gap-2">
            <StatusBadge enabled={user.enabled} />
            <span className="inline-flex rounded-full border border-sky-200 bg-sky-50 px-3 py-1 text-xs font-medium text-sky-700">
              {user.roleLabel || "未設定"}
            </span>
          </div>
        </div>

        <div className="px-5 py-5 sm:px-8 sm:py-7">
          <div className="divide-y divide-stone-200">
            {rows.map((row) => (
              <div
                key={row.label}
                className="grid gap-2 py-4 md:grid-cols-[180px_1fr]"
              >
                <div className="text-sm font-semibold text-slate-700">
                  {row.label}
                </div>
                <div className="break-words text-sm text-slate-700">
                  {row.value || "未登録"}
                </div>
              </div>
            ))}
          </div>

          <div className="mt-8 flex justify-center">
            <Link
              href="/admin/users"
              className="inline-flex justify-center rounded-xl border border-stone-300 px-6 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
            >
              会員一覧へ戻る
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}

function StatusBadge({ enabled }: { enabled: boolean }) {
  return (
    <span
      className={
        enabled
          ? "inline-flex rounded-full border border-emerald-200 bg-emerald-50 px-3 py-1 text-xs font-medium text-emerald-700"
          : "inline-flex rounded-full border border-slate-200 bg-slate-100 px-3 py-1 text-xs font-medium text-slate-600"
      }
    >
      {statusLabel(enabled)}
    </span>
  );
}