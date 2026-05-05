"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { apiFetch } from "@/lib/apiFetch";

type AdminUserListItem = {
  id: number;
  name: string;
  kana: string;
  email: string;
  roleName: string;
  roleLabel: string;
  enabled: boolean;
};

type AdminUserListResponse = {
  items: AdminUserListItem[];
  keyword: string;
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

function statusLabel(enabled: boolean) {
  return enabled ? "有効" : "無効";
}

export default function AdminUsersPage() {
  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [keywordInput, setKeywordInput] = useState("");
  const [keyword, setKeyword] = useState("");
  const [page, setPage] = useState(1);
  const [data, setData] = useState<AdminUserListResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  const fetchUsers = async (
    nextKeyword: string,
    nextPage: number
  ) => {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const params = new URLSearchParams();
      params.set("page", String(nextPage));
      params.set("pageSize", "10");

      if (nextKeyword.trim()) {
        params.set("keyword", nextKeyword.trim());
      }

// 修正後
const response = await apiFetch(
  `${apiBaseUrl}/api/admin/users?${params.toString()}`,
  {
    method: "GET",
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

      if (!response.ok) {
        setErrorMessage("会員一覧の取得に失敗しました。");
        return;
      }

      const json = (await response.json()) as AdminUserListResponse;
      setData(json);
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchUsers(keyword, page);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [apiBaseUrl, keyword, page]);

  const handleSearch = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setPage(1);
    setKeyword(keywordInput);
  };

  const handleClear = () => {
    setKeywordInput("");
    setKeyword("");
    setPage(1);
  };

  const handleMovePage = (nextPage: number) => {
    if (!data) return;

    if (nextPage < 1 || nextPage > data.totalPages) {
      return;
    }

    setPage(nextPage);
  };

  const pageNumbers = useMemo(() => {
    const totalPages = data?.totalPages ?? 1;
    const current = data?.page ?? page;

    const start = Math.max(1, current - 2);
    const end = Math.min(totalPages, current + 2);

    const numbers: number[] = [];

    for (let i = start; i <= end; i += 1) {
      numbers.push(i);
    }

    return numbers;
  }, [data, page]);

  return (
    <div className="mx-auto max-w-7xl px-4 py-6 sm:py-10">
      <nav className="mb-5 text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">会員一覧</span>
      </nav>

      <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
        <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 text-center sm:px-8 sm:py-9">
          <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
            ADMIN USERS
          </p>
          <h1 className="mt-3 text-xl font-semibold text-slate-800 sm:text-2xl md:text-3xl">
            会員一覧
          </h1>
          <p className="mt-3 text-sm leading-6 text-slate-500">
            登録されている会員情報を検索・確認できます。
          </p>
        </div>

        <div className="px-5 py-5 sm:px-8 sm:py-7">
          {errorMessage && (
            <div className="mb-5 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
              {errorMessage}
            </div>
          )}

          <div className="mb-6 rounded-3xl border border-stone-200 bg-stone-50 p-4 sm:p-5">
            <form
              onSubmit={handleSearch}
              className="flex flex-col gap-3 md:flex-row md:items-end"
            >
              <div className="flex-1">
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  キーワード
                </label>
                <input
                  type="text"
                  value={keywordInput}
                  onChange={(e) => setKeywordInput(e.target.value)}
                  placeholder="氏名・フリガナ・メールアドレス"
                  className="w-full rounded-xl border border-stone-300 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                />
              </div>

              <div className="flex gap-2">
                <button
                  type="submit"
                  className="rounded-xl bg-sky-700 px-5 py-3 text-sm font-medium text-white transition hover:opacity-90"
                >
                  検索
                </button>

                <button
                  type="button"
                  onClick={handleClear}
                  className="rounded-xl border border-stone-300 bg-white px-5 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
                >
                  クリア
                </button>
              </div>
            </form>
          </div>

          {isLoading ? (
            <p className="text-sm text-slate-500">会員一覧を読み込み中...</p>
          ) : !data || data.items.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-stone-300 bg-stone-50 px-4 py-10 text-center">
              <p className="text-sm text-slate-500">会員情報がありません。</p>
            </div>
          ) : (
            <div className="space-y-5">
              <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                <p className="text-sm text-slate-500">
                  全{data.totalCount}件中 {data.page} / {data.totalPages} ページ
                </p>

                {data.keyword && (
                  <p className="text-sm text-slate-500">
                    検索条件：
                    <span className="font-medium text-slate-700">
                      {data.keyword}
                    </span>
                  </p>
                )}
              </div>

              <div className="hidden overflow-hidden rounded-2xl border border-stone-200 md:block">
                <table className="min-w-full divide-y divide-stone-200">
                  <thead className="bg-stone-50">
                    <tr>
                      <th className="px-4 py-3 text-left text-xs font-semibold text-slate-500">
                        ID
                      </th>
                      <th className="px-4 py-3 text-left text-xs font-semibold text-slate-500">
                        氏名
                      </th>
                      <th className="px-4 py-3 text-left text-xs font-semibold text-slate-500">
                        フリガナ
                      </th>
                      <th className="px-4 py-3 text-left text-xs font-semibold text-slate-500">
                        メールアドレス
                      </th>
                      <th className="px-4 py-3 text-left text-xs font-semibold text-slate-500">
                        属性
                      </th>
                      <th className="px-4 py-3 text-left text-xs font-semibold text-slate-500">
                        状態
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-semibold text-slate-500">
                        操作
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-stone-200 bg-white">
                    {data.items.map((user) => (
                      <tr key={user.id}>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {user.id}
                        </td>
                        <td className="px-4 py-4 text-sm font-medium text-slate-800">
                          {user.name}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {user.kana || "未登録"}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {user.email}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {user.roleLabel || "未設定"}
                        </td>
                        <td className="px-4 py-4">
                          <StatusBadge enabled={user.enabled} />
                        </td>
                        <td className="px-4 py-4 text-right">
                          <Link
                            href={`/admin/users/${user.id}`}
                            className="text-sm font-medium text-sky-700 hover:underline"
                          >
                            詳細
                          </Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="space-y-3 md:hidden">
                {data.items.map((user) => (
                  <div
                    key={user.id}
                    className="rounded-2xl border border-stone-200 bg-white p-4 shadow-sm"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-xs text-slate-500">ID: {user.id}</p>
                        <p className="mt-1 text-base font-semibold text-slate-800">
                          {user.name}
                        </p>
                        <p className="mt-1 text-sm text-slate-500">
                          {user.kana || "未登録"}
                        </p>
                      </div>

                      <StatusBadge enabled={user.enabled} />
                    </div>

                    <div className="mt-4 space-y-2 text-sm text-slate-700">
                      <p>{user.email}</p>
                      <p>
                        属性：
                        <span className="font-medium">
                          {user.roleLabel || "未設定"}
                        </span>
                      </p>
                    </div>

                    <div className="mt-4 text-right">
                      <Link
                        href={`/admin/users/${user.id}`}
                        className="text-sm font-medium text-sky-700 hover:underline"
                      >
                        詳細を見る
                      </Link>
                    </div>
                  </div>
                ))}
              </div>

              <div className="flex flex-wrap items-center justify-center gap-2 pt-4">
                <button
                  type="button"
                  onClick={() => handleMovePage((data?.page ?? 1) - 1)}
                  disabled={(data?.page ?? 1) <= 1}
                  className="rounded-xl border border-stone-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  前
                </button>

                {pageNumbers.map((pageNumber) => (
                  <button
                    key={pageNumber}
                    type="button"
                    onClick={() => handleMovePage(pageNumber)}
                    className={
                      pageNumber === data.page
                        ? "rounded-xl bg-sky-700 px-4 py-2 text-sm font-medium text-white"
                        : "rounded-xl border border-stone-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
                    }
                  >
                    {pageNumber}
                  </button>
                ))}

                <button
                  type="button"
                  onClick={() => handleMovePage((data?.page ?? 1) + 1)}
                  disabled={(data?.page ?? 1) >= (data?.totalPages ?? 1)}
                  className="rounded-xl border border-stone-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  次
                </button>
              </div>
            </div>
          )}
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