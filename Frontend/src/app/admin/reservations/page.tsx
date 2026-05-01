"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";

type AdminReservationRow = {
  reservationId: number;
  roomId: number;
  roomName: string;
  hostUserId: number;
  hostName: string;
  guestUserId: number;
  guestName: string;
  startAt: string;
  endAt: string;
  amount: number;
  status: string;
};

type RoomOption = {
  id: number;
  name: string;
};

type AdminReservationListResponse = {
  items: AdminReservationRow[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  roomOptions: RoomOption[];
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

function formatYen(value: number) {
  return new Intl.NumberFormat("ja-JP", {
    style: "currency",
    currency: "JPY",
    maximumFractionDigits: 0,
  }).format(value);
}

function formatStatus(status: string) {
  if (status === "booked") return "予約済み";
  if (status === "paid") return "承認済み";
  if (status === "canceled") return "キャンセル";
  return status;
}

function statusClassName(status: string) {
  if (status === "paid") {
    return "bg-emerald-50 text-emerald-700 ring-emerald-200";
  }

  if (status === "canceled") {
    return "bg-rose-50 text-rose-700 ring-rose-200";
  }

  return "bg-slate-50 text-slate-700 ring-slate-200";
}

export default function AdminReservationsPage() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const currentKeyword = searchParams.get("keyword") ?? "";
  const currentReservationId = searchParams.get("reservationId") ?? "";
  const currentStatus = searchParams.get("status") ?? "";
  const currentRoomId = searchParams.get("roomId") ?? "";
  const currentStartFrom = searchParams.get("startFrom") ?? "";
  const currentStartTo = searchParams.get("startTo") ?? "";
  const currentPage = Number(searchParams.get("page") ?? "1");

  const [keyword, setKeyword] = useState(currentKeyword);
  const [reservationId, setReservationId] = useState(currentReservationId);
  const [status, setStatus] = useState(currentStatus);
  const [roomId, setRoomId] = useState(currentRoomId);
  const [startFrom, setStartFrom] = useState(currentStartFrom);
  const [startTo, setStartTo] = useState(currentStartTo);

  const [rows, setRows] = useState<AdminReservationRow[]>([]);
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

    if (keyword.trim()) params.set("keyword", keyword.trim());
    if (reservationId.trim()) params.set("reservationId", reservationId.trim());
    if (status) params.set("status", status);
    if (roomId) params.set("roomId", roomId);
    if (startFrom) params.set("startFrom", startFrom);
    if (startTo) params.set("startTo", startTo);

    params.set("page", String(page));
    params.set("pageSize", "10");

    return params.toString();
  };

  const fetchReservations = async () => {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const params = new URLSearchParams();

      if (currentKeyword.trim()) params.set("keyword", currentKeyword.trim());
      if (currentReservationId.trim()) {
        params.set("reservationId", currentReservationId.trim());
      }
      if (currentStatus) params.set("status", currentStatus);
      if (currentRoomId) params.set("roomId", currentRoomId);
      if (currentStartFrom) params.set("startFrom", currentStartFrom);
      if (currentStartTo) params.set("startTo", currentStartTo);

      params.set("page", String(currentPage > 0 ? currentPage : 1));
      params.set("pageSize", "10");

      const response = await fetch(
        `${apiBaseUrl}/api/admin/reservations?${params.toString()}`,
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
        setErrorMessage("予約一覧の取得に失敗しました。");
        return;
      }

      const data = (await response.json()) as AdminReservationListResponse;

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

  useEffect(() => {
    setKeyword(currentKeyword);
    setReservationId(currentReservationId);
    setStatus(currentStatus);
    setRoomId(currentRoomId);
    setStartFrom(currentStartFrom);
    setStartTo(currentStartTo);
  }, [
    currentKeyword,
    currentReservationId,
    currentStatus,
    currentRoomId,
    currentStartFrom,
    currentStartTo,
  ]);

  useEffect(() => {
    fetchReservations();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    apiBaseUrl,
    currentKeyword,
    currentReservationId,
    currentStatus,
    currentRoomId,
    currentStartFrom,
    currentStartTo,
    currentPage,
  ]);

  const handleSearch = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    router.push(`/admin/reservations?${buildQueryString(1)}`);
  };

  const handleClear = () => {
    router.push("/admin/reservations");
  };

  const movePage = (page: number) => {
    router.push(`/admin/reservations?${buildQueryString(page)}`);
  };

  return (
    <div className="mx-auto max-w-7xl px-4 py-6 sm:py-10">
      <nav className="mb-5 text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">予約一覧（管理者）</span>
      </nav>

      <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
        <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 text-center sm:px-8 sm:py-9">
          <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
            ADMIN RESERVATIONS
          </p>
          <h1 className="mt-3 text-xl font-semibold text-slate-800 sm:text-2xl md:text-3xl">
            予約一覧（管理者）
          </h1>
          <p className="mt-3 text-sm leading-6 text-slate-500">
            全スタジオの予約状況を検索・確認できます。公開デモでは誤操作防止のため、予約状態の変更操作は提供していません。
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
                  value={keyword}
                  onChange={(e) => setKeyword(e.target.value)}
                  placeholder="スタジオ名 / 予約者名 / 提供者名"
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  予約ID
                </label>
                <input
                  type="number"
                  value={reservationId}
                  onChange={(e) => setReservationId(e.target.value)}
                  placeholder="完全一致"
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  状態
                </label>
                <select
                  value={status}
                  onChange={(e) => setStatus(e.target.value)}
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                >
                  <option value="">すべて</option>
                  <option value="booked">予約済み</option>
                  <option value="paid">承認済み</option>
                  <option value="canceled">キャンセル</option>
                </select>
              </div>

              <div className="xl:col-span-2">
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  スタジオ
                </label>
                <select
                  value={roomId}
                  onChange={(e) => setRoomId(e.target.value)}
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                >
                  <option value="">すべてのスタジオ</option>
                  {roomOptions.map((room) => (
                    <option key={room.id} value={room.id}>
                      {room.name}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  開始日From
                </label>
                <input
                  type="date"
                  value={startFrom}
                  onChange={(e) => setStartFrom(e.target.value)}
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  開始日To
                </label>
                <input
                  type="date"
                  value={startTo}
                  onChange={(e) => setStartTo(e.target.value)}
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

          <div className="mb-4 flex flex-col gap-1 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <h2 className="text-lg font-semibold text-slate-800">
                予約一覧
              </h2>
              <p className="mt-1 text-sm text-slate-500">
                全 {pageInfo.totalCount} 件
              </p>
            </div>
          </div>

          {isLoading ? (
            <p className="text-sm text-slate-500">予約一覧を読み込み中...</p>
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
                    <div className="mb-4 flex items-start justify-between gap-3">
                      <div>
                        <p className="text-xs font-medium tracking-[0.18em] text-sky-700">
                          RESERVATION #{row.reservationId}
                        </p>
                        <h3 className="mt-1 text-lg font-semibold text-slate-800">
                          {row.roomName}
                        </h3>
                        <p className="mt-1 text-sm text-slate-500">
                          提供者：{row.hostName || "未設定"}
                        </p>
                      </div>

                      <span
                        className={`rounded-full px-3 py-1 text-xs font-medium ring-1 ${statusClassName(
                          row.status
                        )}`}
                      >
                        {formatStatus(row.status)}
                      </span>
                    </div>

                    <dl className="grid gap-3 text-sm sm:grid-cols-2">
                      <InfoItem label="予約者" value={row.guestName} />
                      <InfoItem label="料金" value={formatYen(row.amount)} />
                      <InfoItem label="開始" value={formatDateTime(row.startAt)} />
                      <InfoItem label="終了" value={formatDateTime(row.endAt)} />
                    </dl>
                  </article>
                ))}
              </div>

              <div className="hidden overflow-x-auto lg:block">
                <table className="w-full min-w-[1080px] border-separate border-spacing-y-3">
                  <thead>
                    <tr className="text-left text-xs font-semibold tracking-wide text-slate-500">
                      <th className="px-4 py-2">予約ID</th>
                      <th className="px-4 py-2">スタジオ</th>
                      <th className="px-4 py-2">提供者</th>
                      <th className="px-4 py-2">予約者</th>
                      <th className="px-4 py-2">開始</th>
                      <th className="px-4 py-2">終了</th>
                      <th className="px-4 py-2">料金</th>
                      <th className="px-4 py-2">状態</th>
                    </tr>
                  </thead>

                  <tbody>
                    {rows.map((row) => (
                      <tr key={row.reservationId} className="bg-stone-50">
                        <td className="rounded-l-2xl px-4 py-4 text-sm font-semibold text-slate-700">
                          {row.reservationId}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {row.roomName}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {row.hostName || "未設定"}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {row.guestName}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {formatDateTime(row.startAt)}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {formatDateTime(row.endAt)}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {formatYen(row.amount)}
                        </td>
                        <td className="rounded-r-2xl px-4 py-4">
                          <span
                            className={`rounded-full px-3 py-1 text-xs font-medium ring-1 ${statusClassName(
                              row.status
                            )}`}
                          >
                            {formatStatus(row.status)}
                          </span>
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