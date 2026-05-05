"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { apiFetch } from "@/lib/apiFetch";

type HostSalesRow = {
  reservationId: number;
  roomId: number;
  roomName: string;
  guestName: string;
  startAt: string;
  endAt: string;
  amount: number;
  status: string;
  hasItems: boolean;
};

type RoomOption = {
  id: number;
  name: string;
};

type HostSalesListResponse = {
  items: HostSalesRow[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  roomOptions: RoomOption[];
};

function formatDateTime(value: string) {
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

function formatStatus(status: string) {
  if (status === "booked") return "予約済み";
  if (status === "paid") return "利用済み";
  if (status === "canceled") return "キャンセル済み";
  return status;
}

export default function HostSalesPage() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const currentRoomId = searchParams.get("roomId") ?? "";
  const currentOnlyWithItems = searchParams.get("onlyWithItems") ?? "true";
  const currentPage = Number(searchParams.get("page") ?? "1");

  const [roomId, setRoomId] = useState(currentRoomId);
  const [onlyWithItems, setOnlyWithItems] = useState(
    currentOnlyWithItems !== "false"
  );

  const [rows, setRows] = useState<HostSalesRow[]>([]);
  const [roomOptions, setRoomOptions] = useState<RoomOption[]>([]);
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
    if (roomId) params.set("roomId", roomId);
    params.set("onlyWithItems", String(onlyWithItems));
    params.set("page", String(page));
    params.set("pageSize", "10");
    return params.toString();
  };

  const fetchSales = async () => {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const params = new URLSearchParams();
      if (currentRoomId) params.set("roomId", currentRoomId);
      params.set("onlyWithItems", currentOnlyWithItems);
      params.set("page", String(currentPage > 0 ? currentPage : 1));
      params.set("pageSize", "10");

      const response = await apiFetch(
        `${apiBaseUrl}/api/host/sales?${params.toString()}`,
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
        setErrorMessage("ホストユーザーのみアクセスできます。");
        return;
      }

      if (!response.ok) {
        setErrorMessage("売上一覧の取得に失敗しました。");
        return;
      }

      const data = (await response.json()) as HostSalesListResponse;
      setRows(data.items ?? []);
      setRoomOptions(data.roomOptions ?? []);
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

  const handleCsvDownload = async () => {
    try {
      const params = new URLSearchParams();
      if (roomId) params.set("roomId", roomId);
      params.set("onlyWithItems", String(onlyWithItems));

      const response = await apiFetch(
        `${apiBaseUrl}/api/host/sales.csv?${params.toString()}`,
        { method: "GET" }
      );

      if (response.status === 401) {
        window.location.href = "/auth/login";
        return;
      }

      if (!response.ok) {
        setErrorMessage("CSVのダウンロードに失敗しました。");
        return;
      }

      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = "sales.csv";
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    }
  };

  useEffect(() => {
    setRoomId(currentRoomId);
    setOnlyWithItems(currentOnlyWithItems !== "false");
  }, [currentRoomId, currentOnlyWithItems]);

  useEffect(() => {
    fetchSales();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [apiBaseUrl, currentRoomId, currentOnlyWithItems, currentPage]);

  const handleSearch = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    router.push(`/host/sales?${buildQueryString(1)}`);
  };

  const movePage = (page: number) => {
    router.push(`/host/sales?${buildQueryString(page)}`);
  };

  return (
    <div className="mx-auto max-w-7xl px-4 py-6 sm:py-10">
      <nav className="mb-5 text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">売上明細一覧</span>
      </nav>

      <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
        <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 text-center sm:px-8 sm:py-9">
          <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
            HOST SALES
          </p>
          <h1 className="mt-3 text-xl font-semibold text-slate-800 sm:text-2xl md:text-3xl">
            売上明細一覧
          </h1>
          <p className="mt-3 text-sm leading-6 text-slate-500">
            管理スタジオの予約売上と料金明細を確認できます。
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
            <div className="grid gap-4 md:grid-cols-4">
              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  スタジオ
                </label>
                <select
                  value={roomId}
                  onChange={(e) => setRoomId(e.target.value)}
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                >
                  <option value="">全体</option>
                  {roomOptions.map((room) => (
                    <option key={room.id} value={room.id}>
                      {room.name}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex items-end">
                <label className="flex w-full cursor-pointer items-center gap-3 rounded-xl border border-stone-300 bg-white px-4 py-3 text-sm text-slate-700">
                  <input
                    type="checkbox"
                    checked={onlyWithItems}
                    onChange={(e) => setOnlyWithItems(e.target.checked)}
                    className="h-4 w-4 rounded border-stone-300 text-sky-700"
                  />
                  明細がある予約のみ
                </label>
              </div>

              <div className="flex items-end">
                <button
                  type="submit"
                  className="w-full rounded-xl bg-sky-700 px-6 py-3 text-sm font-medium text-white shadow-sm transition hover:opacity-90"
                >
                  選択適用
                </button>
              </div>

              <div className="flex items-end">
                <button
                  type="button"
                  onClick={handleCsvDownload}
                  className="w-full rounded-xl border border-sky-200 bg-white px-6 py-3 text-center text-sm font-medium text-sky-700 transition hover:bg-sky-50"
                >
                  CSVダウンロード
                </button>
              </div>
            </div>
          </form>

          <div className="mb-4 flex flex-col gap-1 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <h2 className="text-lg font-semibold text-slate-800">
                売上一覧
              </h2>
              <p className="mt-1 text-sm text-slate-500">
                全 {pageInfo.totalCount} 件
              </p>
            </div>
          </div>

          {isLoading ? (
            <p className="text-sm text-slate-500">売上一覧を読み込み中...</p>
          ) : rows.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-stone-300 bg-stone-50 px-4 py-10 text-center">
              <p className="text-sm text-slate-500">
                該当する予約はありません。
              </p>
            </div>
          ) : (
            <>
              <div className="grid gap-4 lg:hidden">
                {rows.map((row) => (
                  <article
                    key={row.reservationId}
                    className="rounded-2xl border border-stone-200 bg-white p-4 shadow-sm"
                  >
                    <p className="text-xs font-medium tracking-[0.18em] text-sky-700">
                      RESERVATION #{row.reservationId}
                    </p>
                    <h3 className="mt-1 text-lg font-semibold text-slate-800">
                      {row.roomName}
                    </h3>
                    <p className="mt-1 text-sm text-slate-500">
                      {row.guestName}
                    </p>

                    <dl className="mt-4 grid gap-3 text-sm sm:grid-cols-2">
                      <div>
                        <dt className="text-xs font-medium text-slate-400">開始</dt>
                        <dd className="mt-1 text-slate-700">{formatDateTime(row.startAt)}</dd>
                      </div>
                      <div>
                        <dt className="text-xs font-medium text-slate-400">終了</dt>
                        <dd className="mt-1 text-slate-700">{formatDateTime(row.endAt)}</dd>
                      </div>
                      <div>
                        <dt className="text-xs font-medium text-slate-400">料金</dt>
                        <dd className="mt-1 font-semibold text-slate-800">
                          {row.amount.toLocaleString()}円
                        </dd>
                      </div>
                      <div>
                        <dt className="text-xs font-medium text-slate-400">状態</dt>
                        <dd className="mt-1 text-slate-700">{formatStatus(row.status)}</dd>
                      </div>
                    </dl>

                    <Link
                      href={`/host/sales/${row.reservationId}`}
                      className="mt-5 block rounded-xl border border-sky-200 px-4 py-3 text-center text-sm font-medium text-sky-700 transition hover:bg-sky-50"
                    >
                      明細
                    </Link>
                  </article>
                ))}
              </div>

              <div className="hidden overflow-x-auto lg:block">
                <table className="w-full min-w-[900px] border-separate border-spacing-y-3">
                  <thead>
                    <tr className="text-left text-xs font-semibold tracking-wide text-slate-500">
                      <th className="px-4 py-2">スタジオ</th>
                      <th className="px-4 py-2">予約者</th>
                      <th className="px-4 py-2">開始</th>
                      <th className="px-4 py-2">終了</th>
                      <th className="px-4 py-2">料金</th>
                      <th className="px-4 py-2">状態</th>
                      <th className="px-4 py-2 text-right">操作</th>
                    </tr>
                  </thead>
                  <tbody>
                    {rows.map((row) => (
                      <tr key={row.reservationId} className="bg-stone-50">
                        <td className="rounded-l-2xl px-4 py-4 text-sm text-slate-700">
                          {row.roomName}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">{row.guestName}</td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {formatDateTime(row.startAt)}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {formatDateTime(row.endAt)}
                        </td>
                        <td className="px-4 py-4 text-sm font-semibold text-slate-800">
                          {row.amount.toLocaleString()}円
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {formatStatus(row.status)}
                        </td>
                        <td className="rounded-r-2xl px-4 py-4 text-right">
                          <Link
                            href={`/host/sales/${row.reservationId}`}
                            className="rounded-xl border border-sky-200 px-4 py-2 text-sm font-medium text-sky-700 transition hover:bg-sky-50"
                          >
                            明細
                          </Link>
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