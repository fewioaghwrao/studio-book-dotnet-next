"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { apiFetch } from "@/lib/apiFetch";

type HostReservationRow = {
  reservationId: number;
  roomId: number;
  roomName: string;
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

type HostReservationListResponse = {
  items: HostReservationRow[];
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

export default function HostReservationsPage() {
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

  const [rows, setRows] = useState<HostReservationRow[]>([]);
  const [roomOptions, setRoomOptions] = useState<RoomOption[]>([]);
  const [pageInfo, setPageInfo] = useState({
    page: currentPage,
    pageSize: 10,
    totalCount: 0,
    totalPages: 1,
  });

  const [isLoading, setIsLoading] = useState(true);
  const [processingId, setProcessingId] = useState<number | null>(null);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

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
      if (currentReservationId.trim()) params.set("reservationId", currentReservationId.trim());
      if (currentStatus) params.set("status", currentStatus);
      if (currentRoomId) params.set("roomId", currentRoomId);
      if (currentStartFrom) params.set("startFrom", currentStartFrom);
      if (currentStartTo) params.set("startTo", currentStartTo);

      params.set("page", String(currentPage > 0 ? currentPage : 1));
      params.set("pageSize", "10");

      const response = await apiFetch(
        `${apiBaseUrl}/api/host/reservations?${params.toString()}`,
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
        setErrorMessage("予約一覧の取得に失敗しました。");
        return;
      }

      const data = (await response.json()) as HostReservationListResponse;

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
    router.push(`/host/reservations?${buildQueryString(1)}`);
  };

  const handleClear = () => {
    router.push("/host/reservations");
  };

  const movePage = (page: number) => {
    router.push(`/host/reservations?${buildQueryString(page)}`);
  };

  const handleApprove = async (id: number) => {
    setProcessingId(id);
    setErrorMessage("");
    setSuccessMessage("");

    try {
      const response = await apiFetch(
        `${apiBaseUrl}/api/host/reservations/${id}/approve`,
        {
          method: "POST",
        }
      );

      if (response.status === 401) {
        window.location.href = "/auth/login";
        return;
      }

      if (!response.ok) {
        const data = await response.json().catch(() => null);
        setErrorMessage(data?.message ?? "予約の承認に失敗しました。");
        return;
      }

      setSuccessMessage("予約を承認しました。");
      await fetchReservations();
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    } finally {
      setProcessingId(null);
    }
  };

  const handleCancel = async (id: number) => {
    const ok = window.confirm("本当にキャンセルしますか？");

    if (!ok) return;

    setProcessingId(id);
    setErrorMessage("");
    setSuccessMessage("");

    try {
      const response = await apiFetch(
        `${apiBaseUrl}/api/host/reservations/${id}/cancel`,
        {
          method: "POST",
        }
      );

      if (response.status === 401) {
        window.location.href = "/auth/login";
        return;
      }

      if (!response.ok) {
        const data = await response.json().catch(() => null);
        setErrorMessage(data?.message ?? "予約のキャンセルに失敗しました。");
        return;
      }

      setSuccessMessage("予約をキャンセルしました。");
      await fetchReservations();
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    } finally {
      setProcessingId(null);
    }
  };

  return (
    <div className="mx-auto max-w-7xl px-4 py-6 sm:py-10">
      <nav className="mb-5 text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">予約一覧（スタジオ提供者）</span>
      </nav>

      <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
        <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 text-center sm:px-8 sm:py-9">
          <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
            HOST RESERVATIONS
          </p>
          <h1 className="mt-3 text-xl font-semibold text-slate-800 sm:text-2xl md:text-3xl">
            予約一覧（スタジオ提供者）
          </h1>
          <p className="mt-3 text-sm leading-6 text-slate-500">
            自分が管理するスタジオの予約状況を確認し、予約の承認・キャンセルを行えます。
          </p>
        </div>

        <div className="px-5 py-5 sm:px-8 sm:py-7">
          {successMessage && (
            <div className="mb-5 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
              {successMessage}
            </div>
          )}

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
                  placeholder="スタジオ名 / 予約者名"
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
                      <div>
                        <dt className="text-xs font-medium text-slate-400">
                          予約者
                        </dt>
                        <dd className="mt-1 text-slate-700">{row.guestName}</dd>
                      </div>
                      <div>
                        <dt className="text-xs font-medium text-slate-400">
                          料金
                        </dt>
                        <dd className="mt-1 text-slate-700">
                          {row.amount.toLocaleString()}円
                        </dd>
                      </div>
                      <div>
                        <dt className="text-xs font-medium text-slate-400">
                          開始
                        </dt>
                        <dd className="mt-1 text-slate-700">
                          {formatDateTime(row.startAt)}
                        </dd>
                      </div>
                      <div>
                        <dt className="text-xs font-medium text-slate-400">
                          終了
                        </dt>
                        <dd className="mt-1 text-slate-700">
                          {formatDateTime(row.endAt)}
                        </dd>
                      </div>
                    </dl>

                    {row.status === "booked" && (
                      <div className="mt-5 grid gap-2 sm:grid-cols-2">
                        <button
                          type="button"
                          onClick={() => handleApprove(row.reservationId)}
                          disabled={processingId === row.reservationId}
                          className="rounded-xl bg-emerald-600 px-4 py-3 text-sm font-medium text-white transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                          承認
                        </button>

                        <button
                          type="button"
                          onClick={() => handleCancel(row.reservationId)}
                          disabled={processingId === row.reservationId}
                          className="rounded-xl border border-rose-200 px-4 py-3 text-sm font-medium text-rose-700 transition hover:bg-rose-50 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                          キャンセル
                        </button>
                      </div>
                    )}
                  </article>
                ))}
              </div>

              <div className="hidden overflow-x-auto lg:block">
                <table className="w-full min-w-[980px] border-separate border-spacing-y-3">
                  <thead>
                    <tr className="text-left text-xs font-semibold tracking-wide text-slate-500">
                      <th className="px-4 py-2">予約ID</th>
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
                        <td className="rounded-l-2xl px-4 py-4 text-sm font-semibold text-slate-700">
                          {row.reservationId}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {row.roomName}
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
                          {row.amount.toLocaleString()}円
                        </td>
                        <td className="px-4 py-4">
                          <span
                            className={`rounded-full px-3 py-1 text-xs font-medium ring-1 ${statusClassName(
                              row.status
                            )}`}
                          >
                            {formatStatus(row.status)}
                          </span>
                        </td>
                        <td className="rounded-r-2xl px-4 py-4 text-right">
                          {row.status === "booked" ? (
                            <div className="flex justify-end gap-2">
                              <button
                                type="button"
                                onClick={() => handleApprove(row.reservationId)}
                                disabled={processingId === row.reservationId}
                                className="rounded-xl bg-emerald-600 px-4 py-2 text-sm font-medium text-white transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
                              >
                                承認
                              </button>

                              <button
                                type="button"
                                onClick={() => handleCancel(row.reservationId)}
                                disabled={processingId === row.reservationId}
                                className="rounded-xl border border-rose-200 bg-white px-4 py-2 text-sm font-medium text-rose-700 transition hover:bg-rose-50 disabled:cursor-not-allowed disabled:opacity-50"
                              >
                                キャンセル
                              </button>
                            </div>
                          ) : (
                            <span className="text-sm text-slate-400">-</span>
                          )}
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