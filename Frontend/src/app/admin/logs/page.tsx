"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";

type AuditLogRow = {
  id: number;
  ts: string;
  actorId: number | null;
  action: string;
  entity: string;
  entityId: number | null;
  note: string;
};

type AuditLogListResponse = {
  items: AuditLogRow[];
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

function actionBadgeClassName(action: string) {
  const upper = action.toUpperCase();

  if (upper.includes("CREATE")) {
    return "bg-emerald-50 text-emerald-700 ring-emerald-200";
  }

  if (upper.includes("UPDATE") || upper.includes("SETTING")) {
    return "bg-sky-50 text-sky-700 ring-sky-200";
  }

  if (upper.includes("DELETE") || upper.includes("CANCEL")) {
    return "bg-rose-50 text-rose-700 ring-rose-200";
  }

  if (upper.includes("LOGIN")) {
    return "bg-violet-50 text-violet-700 ring-violet-200";
  }

  return "bg-slate-50 text-slate-700 ring-slate-200";
}

export default function AdminLogsPage() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const currentQ = searchParams.get("q") ?? "";
  const currentActorId = searchParams.get("actorId") ?? "";
  const currentAction = searchParams.get("action") ?? "";
  const currentEntity = searchParams.get("entity") ?? "";
  const currentEntityId = searchParams.get("entityId") ?? "";
  const currentFrom = searchParams.get("from") ?? "";
  const currentTo = searchParams.get("to") ?? "";
  const currentPage = Number(searchParams.get("page") ?? "1");

  const [q, setQ] = useState(currentQ);
  const [actorId, setActorId] = useState(currentActorId);
  const [action, setAction] = useState(currentAction);
  const [entity, setEntity] = useState(currentEntity);
  const [entityId, setEntityId] = useState(currentEntityId);
  const [from, setFrom] = useState(currentFrom);
  const [to, setTo] = useState(currentTo);

  const [rows, setRows] = useState<AuditLogRow[]>([]);
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
    if (actorId.trim()) params.set("actorId", actorId.trim());
    if (action.trim()) params.set("action", action.trim());
    if (entity.trim()) params.set("entity", entity.trim());
    if (entityId.trim()) params.set("entityId", entityId.trim());
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
      if (currentActorId.trim()) params.set("actorId", currentActorId.trim());
      if (currentAction.trim()) params.set("action", currentAction.trim());
      if (currentEntity.trim()) params.set("entity", currentEntity.trim());
      if (currentEntityId.trim()) params.set("entityId", currentEntityId.trim());
      if (currentFrom) params.set("from", currentFrom);
      if (currentTo) params.set("to", currentTo);

      params.set("page", String(currentPage > 0 ? currentPage : 1));
      params.set("pageSize", "10");

      const response = await fetch(
        `${apiBaseUrl}/api/admin/logs?${params.toString()}`,
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

      if (!response.ok) {
        setErrorMessage("ログ一覧の取得に失敗しました。");
        return;
      }

      const data = (await response.json()) as AuditLogListResponse;

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
    setActorId(currentActorId);
    setAction(currentAction);
    setEntity(currentEntity);
    setEntityId(currentEntityId);
    setFrom(currentFrom);
    setTo(currentTo);
  }, [
    currentQ,
    currentActorId,
    currentAction,
    currentEntity,
    currentEntityId,
    currentFrom,
    currentTo,
  ]);

  useEffect(() => {
    fetchLogs();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    apiBaseUrl,
    currentQ,
    currentActorId,
    currentAction,
    currentEntity,
    currentEntityId,
    currentFrom,
    currentTo,
    currentPage,
  ]);

  const handleSearch = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    router.push(`/admin/logs?${buildQueryString(1)}`);
  };

  const handleClear = () => {
    router.push("/admin/logs");
  };

  const movePage = (page: number) => {
    router.push(`/admin/logs?${buildQueryString(page)}`);
  };

  return (
    <div className="mx-auto max-w-7xl px-4 py-6 sm:py-10">
      <nav className="mb-5 text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">ログ一覧</span>
      </nav>

      <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
        <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 text-center sm:px-8 sm:py-9">
          <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
            ADMIN AUDIT LOGS
          </p>
          <h1 className="mt-3 text-xl font-semibold text-slate-800 sm:text-2xl md:text-3xl">
            ログ一覧
          </h1>
          <p className="mt-3 text-sm leading-6 text-slate-500">
            管理操作や主要イベントの履歴を検索・確認できます。
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
                  placeholder="action / entity / note"
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  会員ID
                </label>
                <input
                  type="number"
                  min="1"
                  value={actorId}
                  onChange={(e) => setActorId(e.target.value)}
                  placeholder="actorId"
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  対象ID
                </label>
                <input
                  type="number"
                  min="1"
                  value={entityId}
                  onChange={(e) => setEntityId(e.target.value)}
                  placeholder="entityId"
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  アクション
                </label>
                <input
                  type="text"
                  value={action}
                  onChange={(e) => setAction(e.target.value)}
                  placeholder="UPDATE"
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  対象
                </label>
                <input
                  type="text"
                  value={entity}
                  onChange={(e) => setEntity(e.target.value)}
                  placeholder="Room"
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
              ログ一覧
            </h2>
            <p className="mt-1 text-sm text-slate-500">
              全 {pageInfo.totalCount} 件
            </p>
          </div>

          {isLoading ? (
            <p className="text-sm text-slate-500">ログ一覧を読み込み中...</p>
          ) : rows.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-stone-300 bg-stone-50 px-4 py-10 text-center">
              <p className="text-sm text-slate-500">
                ログはありません。
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
                          LOG #{row.id}
                        </p>
                        <h3 className="mt-1 text-base font-semibold text-slate-800">
                          {row.entity || "-"}
                          {row.entityId != null ? ` #${row.entityId}` : ""}
                        </h3>
                        <p className="mt-1 text-sm text-slate-500">
                          {formatDateTime(row.ts)}
                        </p>
                      </div>

                      <span
                        className={`rounded-full px-3 py-1 text-xs font-medium ring-1 ${actionBadgeClassName(
                          row.action
                        )}`}
                      >
                        {row.action || "-"}
                      </span>
                    </div>

                    <dl className="grid gap-3 text-sm sm:grid-cols-2">
                      <InfoItem
                        label="会員ID"
                        value={row.actorId != null ? String(row.actorId) : "-"}
                      />
                      <InfoItem
                        label="対象ID"
                        value={row.entityId != null ? String(row.entityId) : "-"}
                      />
                      <div className="sm:col-span-2">
                        <dt className="text-xs font-medium text-slate-400">
                          メモ
                        </dt>
                        <dd className="mt-1 whitespace-pre-wrap break-words text-slate-700">
                          {row.note || "-"}
                        </dd>
                      </div>
                    </dl>
                  </article>
                ))}
              </div>

              <div className="hidden overflow-x-auto lg:block">
                <table className="w-full min-w-[1080px] border-separate border-spacing-y-3">
                  <thead>
                    <tr className="text-left text-xs font-semibold tracking-wide text-slate-500">
                      <th className="px-4 py-2">ID</th>
                      <th className="px-4 py-2">記録時刻</th>
                      <th className="px-4 py-2">会員ID</th>
                      <th className="px-4 py-2">アクション</th>
                      <th className="px-4 py-2">対象</th>
                      <th className="px-4 py-2">対象ID</th>
                      <th className="px-4 py-2">メモ</th>
                    </tr>
                  </thead>

                  <tbody>
                    {rows.map((row) => (
                      <tr key={row.id} className="bg-stone-50 align-top">
                        <td className="rounded-l-2xl px-4 py-4 text-sm font-semibold text-slate-700">
                          {row.id}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {formatDateTime(row.ts)}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {row.actorId ?? "-"}
                        </td>
                        <td className="px-4 py-4">
                          <span
                            className={`rounded-full px-3 py-1 text-xs font-medium ring-1 ${actionBadgeClassName(
                              row.action
                            )}`}
                          >
                            {row.action || "-"}
                          </span>
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {row.entity || "-"}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {row.entityId ?? "-"}
                        </td>
                        <td className="rounded-r-2xl px-4 py-4 text-sm text-slate-700">
                          <div className="max-w-[360px] whitespace-pre-wrap break-words">
                            {row.note || "-"}
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
      <dd className="mt-1 text-slate-700">{value}</dd>
    </div>
  );
}