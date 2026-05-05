"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { apiFetch } from "@/lib/apiFetch";

type AiSearchLogRow = {
  id: number;
  createdAtUtc: string;
  query: string;
  ipAddress: string | null;
  userId: number | null;
  model: string | null;
  succeeded: boolean;
  resultCount: number;
  errorMessage: string | null;
};

type AiSearchLogListResponse = {
  items: AiSearchLogRow[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

function formatDateTime(value: string) {
  if (!value) return "-";

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("ja-JP", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}

function statusBadgeClassName(succeeded: boolean) {
  return succeeded
    ? "bg-emerald-50 text-emerald-700 ring-emerald-200"
    : "bg-rose-50 text-rose-700 ring-rose-200";
}

function truncateText(value: string | null | undefined, maxLength = 80) {
  if (!value) return "-";
  return value.length > maxLength ? `${value.slice(0, maxLength)}...` : value;
}

export default function AdminAiSearchLogsPage() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const currentQ = searchParams.get("q") ?? "";
  const currentUserId = searchParams.get("userId") ?? "";
  const currentIpAddress = searchParams.get("ipAddress") ?? "";
  const currentSucceeded = searchParams.get("succeeded") ?? "";
  const currentModel = searchParams.get("model") ?? "";
  const currentFrom = searchParams.get("from") ?? "";
  const currentTo = searchParams.get("to") ?? "";
  const currentPage = Number(searchParams.get("page") ?? "1");

  const [q, setQ] = useState(currentQ);
  const [userId, setUserId] = useState(currentUserId);
  const [ipAddress, setIpAddress] = useState(currentIpAddress);
  const [succeeded, setSucceeded] = useState(currentSucceeded);
  const [model, setModel] = useState(currentModel);
  const [from, setFrom] = useState(currentFrom);
  const [to, setTo] = useState(currentTo);

  const [rows, setRows] = useState<AiSearchLogRow[]>([]);
  const [pageInfo, setPageInfo] = useState({
    page: currentPage,
    pageSize: 10,
    totalCount: 0,
    totalPages: 1,
  });

  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  const buildQueryString = (page: number) => {
    const params = new URLSearchParams();

    if (q.trim()) params.set("q", q.trim());
    if (userId.trim()) params.set("userId", userId.trim());
    if (ipAddress.trim()) params.set("ipAddress", ipAddress.trim());
    if (succeeded) params.set("succeeded", succeeded);
    if (model.trim()) params.set("model", model.trim());
    if (from) params.set("from", from);
    if (to) params.set("to", to);

    params.set("page", String(page));
    params.set("pageSize", "10");

    return params.toString();
  };

  const fetchLogs = async () => {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const params = new URLSearchParams();

      if (currentQ.trim()) params.set("q", currentQ.trim());
      if (currentUserId.trim()) params.set("userId", currentUserId.trim());
      if (currentIpAddress.trim()) {
        params.set("ipAddress", currentIpAddress.trim());
      }
      if (currentSucceeded) params.set("succeeded", currentSucceeded);
      if (currentModel.trim()) params.set("model", currentModel.trim());
      if (currentFrom) params.set("from", currentFrom);
      if (currentTo) params.set("to", currentTo);

      params.set("page", String(currentPage > 0 ? currentPage : 1));
      params.set("pageSize", "10");

      const response = await apiFetch(
        `${apiBaseUrl}/api/admin/ai-search-logs?${params.toString()}`,
        {
          method: "GET",          cache: "no-store",
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
        setErrorMessage("AI検索ログの取得に失敗しました。");
        return;
      }

      const data = (await response.json()) as AiSearchLogListResponse;

      setRows(data.items ?? []);
      setPageInfo({
        page: data.page,
        pageSize: data.pageSize,
        totalCount: data.totalCount,
        totalPages: data.totalPages || 1,
      });
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    setQ(currentQ);
    setUserId(currentUserId);
    setIpAddress(currentIpAddress);
    setSucceeded(currentSucceeded);
    setModel(currentModel);
    setFrom(currentFrom);
    setTo(currentTo);
  }, [
    currentQ,
    currentUserId,
    currentIpAddress,
    currentSucceeded,
    currentModel,
    currentFrom,
    currentTo,
  ]);

  useEffect(() => {
    fetchLogs();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    apiBaseUrl,
    currentQ,
    currentUserId,
    currentIpAddress,
    currentSucceeded,
    currentModel,
    currentFrom,
    currentTo,
    currentPage,
  ]);

  const handleSearch = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    router.push(`/admin/ai-search-logs?${buildQueryString(1)}`);
  };

  const handleClear = () => {
    router.push("/admin/ai-search-logs");
  };

  const movePage = (page: number) => {
    router.push(`/admin/ai-search-logs?${buildQueryString(page)}`);
  };

  return (
    <div className="mx-auto max-w-7xl px-4 py-6 sm:py-10">
      <nav className="mb-5 text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <Link href="/admin/logs" className="hover:text-sky-700">
          ログ一覧
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">AI検索ログ</span>
      </nav>

      <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
        <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 text-center sm:px-8 sm:py-9">
          <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
            ADMIN AI SEARCH LOGS
          </p>
          <h1 className="mt-3 text-xl font-semibold text-slate-800 sm:text-2xl md:text-3xl">
            AI検索ログ
          </h1>
          <p className="mt-3 text-sm leading-6 text-slate-500">
            AI自然文スタジオ検索の利用状況、成功・失敗、検索文、結果件数を確認できます。
          </p>
        </div>

        <div className="px-5 py-5 sm:px-8 sm:py-7">
          {errorMessage && (
            <div className="mb-5 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
              {errorMessage}
            </div>
          )}

          <form
            onSubmit={handleSearch}
            className="mb-7 rounded-3xl border border-stone-200 bg-stone-50 p-4 sm:p-5"
          >
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-6">
              <div className="xl:col-span-2">
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  キーワード
                </label>
                <input
                  type="text"
                  value={q}
                  onChange={(e) => setQ(e.target.value)}
                  placeholder="検索文 / エラー内容"
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  ユーザーID
                </label>
                <input
                  type="number"
                  min="1"
                  value={userId}
                  onChange={(e) => setUserId(e.target.value)}
                  placeholder="userId"
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  IPアドレス
                </label>
                <input
                  type="text"
                  value={ipAddress}
                  onChange={(e) => setIpAddress(e.target.value)}
                  placeholder="127.0.0.1"
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  成功/失敗
                </label>
                <select
                  value={succeeded}
                  onChange={(e) => setSucceeded(e.target.value)}
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                >
                  <option value="">すべて</option>
                  <option value="true">成功</option>
                  <option value="false">失敗</option>
                </select>
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  モデル
                </label>
                <input
                  type="text"
                  value={model}
                  onChange={(e) => setModel(e.target.value)}
                  placeholder="gpt-5.4-mini"
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  記録日From
                </label>
                <input
                  type="date"
                  value={from}
                  onChange={(e) => setFrom(e.target.value)}
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  記録日To
                </label>
                <input
                  type="date"
                  value={to}
                  onChange={(e) => setTo(e.target.value)}
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                />
              </div>
            </div>

            <div className="mt-5 flex flex-col gap-3 sm:flex-row">
              <button
                type="submit"
                className="rounded-xl bg-sky-700 px-6 py-3 text-sm font-medium text-white shadow-sm transition hover:opacity-90"
              >
                検索
              </button>

              <button
                type="button"
                onClick={handleClear}
                className="rounded-xl border border-stone-300 bg-white px-6 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
              >
                クリア
              </button>
            </div>
          </form>

          <div className="mb-4">
            <h2 className="text-lg font-semibold text-slate-800">
              AI検索ログ一覧
            </h2>
            <p className="mt-1 text-sm text-slate-500">
              全 {pageInfo.totalCount} 件
            </p>
          </div>

          {isLoading ? (
            <p className="text-sm text-slate-500">
              AI検索ログを読み込み中...
            </p>
          ) : rows.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-stone-300 bg-stone-50 px-4 py-10 text-center">
              <p className="text-sm text-slate-500">
                AI検索ログはありません。
              </p>
            </div>
          ) : (
            <>
              <div className="grid gap-4 lg:hidden">
                {rows.map((row) => (
                  <article
                    key={row.id}
                    className="rounded-2xl border border-stone-200 bg-white p-4 shadow-sm"
                  >
                    <div className="mb-4 flex items-start justify-between gap-3">
                      <div>
                        <p className="text-xs font-medium tracking-[0.18em] text-sky-700">
                          AI LOG #{row.id}
                        </p>
                        <h3 className="mt-1 text-base font-semibold text-slate-800">
                          {row.model || "-"}
                        </h3>
                        <p className="mt-1 text-sm text-slate-500">
                          {formatDateTime(row.createdAtUtc)}
                        </p>
                      </div>

                      <span
                        className={`rounded-full px-3 py-1 text-xs font-medium ring-1 ${statusBadgeClassName(
                          row.succeeded
                        )}`}
                      >
                        {row.succeeded ? "成功" : "失敗"}
                      </span>
                    </div>

                    <dl className="grid gap-3 text-sm sm:grid-cols-2">
                      <InfoItem
                        label="ユーザーID"
                        value={row.userId != null ? String(row.userId) : "-"}
                      />
                      <InfoItem
                        label="IPアドレス"
                        value={row.ipAddress ?? "-"}
                      />
                      <InfoItem
                        label="結果件数"
                        value={`${row.resultCount}件`}
                      />
                      <InfoItem label="モデル" value={row.model ?? "-"} />

                      <div className="sm:col-span-2">
                        <dt className="text-xs font-medium text-slate-400">
                          検索文
                        </dt>
                        <dd className="mt-1 whitespace-pre-wrap break-words text-slate-700">
                          {row.query || "-"}
                        </dd>
                      </div>

                      {row.errorMessage && (
                        <div className="sm:col-span-2">
                          <dt className="text-xs font-medium text-rose-400">
                            エラー
                          </dt>
                          <dd className="mt-1 whitespace-pre-wrap break-words text-rose-700">
                            {row.errorMessage}
                          </dd>
                        </div>
                      )}
                    </dl>
                  </article>
                ))}
              </div>

              <div className="hidden overflow-x-auto lg:block">
                <table className="w-full min-w-[1180px] border-separate border-spacing-y-3">
                  <thead>
                    <tr className="text-left text-xs font-semibold tracking-wide text-slate-500">
                      <th className="px-4 py-2">ID</th>
                      <th className="px-4 py-2">記録時刻</th>
                      <th className="px-4 py-2">状態</th>
                      <th className="px-4 py-2">検索文</th>
                      <th className="px-4 py-2">結果</th>
                      <th className="px-4 py-2">ユーザーID</th>
                      <th className="px-4 py-2">IP</th>
                      <th className="px-4 py-2">モデル</th>
                      <th className="px-4 py-2">エラー</th>
                    </tr>
                  </thead>

                  <tbody>
                    {rows.map((row) => (
                      <tr key={row.id} className="bg-stone-50 align-top">
                        <td className="rounded-l-2xl px-4 py-4 text-sm font-semibold text-slate-700">
                          {row.id}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {formatDateTime(row.createdAtUtc)}
                        </td>
                        <td className="px-4 py-4">
<span
  className={`whitespace-nowrap rounded-full px-3 py-1 text-xs font-medium ring-1 ${statusBadgeClassName(
    row.succeeded
  )}`}
>
  {row.succeeded ? "成功" : "失敗"}
</span>
                        </td>
<td className="px-4 py-4 text-sm text-slate-700">
  <div
    title={row.query}
    className="max-w-[320px] overflow-hidden text-ellipsis whitespace-nowrap"
  >
    {row.query || "-"}
  </div>
</td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {row.resultCount}件
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {row.userId ?? "-"}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {row.ipAddress ?? "-"}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {row.model ?? "-"}
                        </td>
<td className="rounded-r-2xl px-4 py-4 text-sm text-rose-700">
  <div
    title={row.errorMessage ?? ""}
    className="max-w-[260px] overflow-hidden text-ellipsis whitespace-nowrap"
  >
    {row.errorMessage || "-"}
  </div>
</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {pageInfo.totalPages > 1 && (
                <div className="mt-8 flex flex-wrap items-center justify-center gap-2">
                  <button
                    type="button"
                    disabled={pageInfo.page <= 1}
                    onClick={() => movePage(pageInfo.page - 1)}
                    className="rounded-lg border border-stone-300 px-4 py-2 text-sm text-slate-700 disabled:cursor-not-allowed disabled:opacity-40"
                  >
                    前
                  </button>

                  {Array.from({ length: pageInfo.totalPages }, (_, index) => {
                    const page = index + 1;
                    const isActive = page === pageInfo.page;

                    return (
                      <button
                        key={page}
                        type="button"
                        onClick={() => movePage(page)}
                        className={
                          isActive
                            ? "rounded-lg bg-sky-700 px-4 py-2 text-sm font-medium text-white"
                            : "rounded-lg border border-stone-300 px-4 py-2 text-sm text-slate-700 hover:border-sky-300 hover:text-sky-700"
                        }
                      >
                        {page}
                      </button>
                    );
                  })}

                  <button
                    type="button"
                    disabled={pageInfo.page >= pageInfo.totalPages}
                    onClick={() => movePage(pageInfo.page + 1)}
                    className="rounded-lg border border-stone-300 px-4 py-2 text-sm text-slate-700 disabled:cursor-not-allowed disabled:opacity-40"
                  >
                    次
                  </button>
                </div>
              )}
            </>
          )}
        </div>
      </section>
    </div>
  );
}

function InfoItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-xs font-medium text-slate-400">{label}</dt>
      <dd className="mt-1 break-words text-slate-700">{value}</dd>
    </div>
  );
}