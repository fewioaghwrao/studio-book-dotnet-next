"use client";

import Link from "next/link";
import { Suspense, useEffect, useMemo, useState } from "react";
import { useSearchParams } from "next/navigation";
import { apiFetch } from "@/lib/apiFetch";

type CurrentUserResponse = {
  isAuthenticated: boolean;
  user?: {
    id: number;
    name: string;
    email: string;
    roles: string[];
  };
};

type UserReservationListItem = {
  reservationId: number;
  roomId: number;
  roomName: string;
  roomImageName: string | null;
  roomAddress: string;
  startAt: string;
  endAt: string;
  amount: number;
  status: string;
  createdAtUtc: string;
};

type UserReservationListResponse = {
  items: UserReservationListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

function getImageSrc(imageName: string | null) {
  return imageName ? `/storage/${imageName}` : "/images/noImage.png";
}

function formatDateTime(value: string) {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "-";
  }

  return new Intl.DateTimeFormat("ja-JP", {
    year: "numeric",
    month: "numeric",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}

function formatYen(value: number) {
  return `${new Intl.NumberFormat("ja-JP").format(value)} 円`;
}

function getStatusLabel(status: string) {
  switch (status) {
    case "booked":
      return "予約済み";
    case "paid":
      return "決済済み";
    case "canceled":
      return "キャンセル";
    default:
      return status || "-";
  }
}

function getStatusClassName(status: string) {
  switch (status) {
    case "booked":
      return "bg-slate-100 text-slate-700";
    case "paid":
      return "bg-emerald-100 text-emerald-700";
    case "canceled":
      return "bg-rose-100 text-rose-700";
    default:
      return "bg-stone-100 text-slate-600";
  }
}

function hasGeneralUserRole(roles: string[] | undefined) {
  if (!roles) return false;

  return roles.some((role) => {
    const value = role.toUpperCase();
    return value.includes("GENERAL") || value.includes("USER");
  });
}

export default function ReservationsPage() {
  return (
    <Suspense fallback={<LoadingView />}>
      <ReservationsContent />
    </Suspense>
  );
}

function ReservationsContent() {
  const searchParams = useSearchParams();

  const reserved = searchParams.get("reserved");
  const isReserved = reserved === "true";

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [data, setData] = useState<UserReservationListResponse | null>(null);
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  const fetchReservations = async (targetPage: number) => {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const meResponse = await apiFetch(`${apiBaseUrl}/api/auth/me`, {
        method: "GET",        cache: "no-store",
      });

      if (meResponse.status === 401) {
        window.location.href = "/auth/login";
        return;
      }

      if (!meResponse.ok) {
        setErrorMessage("ログイン情報の取得に失敗しました。");
        return;
      }

      const me = (await meResponse.json()) as CurrentUserResponse;

      if (!me.isAuthenticated) {
        window.location.href = "/auth/login";
        return;
      }

      if (!hasGeneralUserRole(me.user?.roles)) {
        setErrorMessage("予約一覧は一般会員のみ閲覧できます。");
        return;
      }

      const query = new URLSearchParams({
        page: String(targetPage),
        pageSize: "10",
      });

      const response = await apiFetch(
        `${apiBaseUrl}/api/reservations?${query.toString()}`,
        {
          method: "GET",          cache: "no-store",
        }
      );

      if (response.status === 401) {
        window.location.href = "/auth/login";
        return;
      }

      if (response.status === 403) {
        setErrorMessage("予約一覧は一般会員のみ閲覧できます。");
        return;
      }

      if (!response.ok) {
        setErrorMessage("予約一覧の取得に失敗しました。");
        return;
      }

      const json = (await response.json()) as UserReservationListResponse;
      setData(json);
      setPage(json.page);
    } catch {
      setErrorMessage(
        "通信エラーが発生しました。時間をおいて再度お試しください。"
      );
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchReservations(page);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [apiBaseUrl]);

  const handleMovePage = (nextPage: number) => {
    if (nextPage < 1) return;
    if (data && nextPage > data.totalPages) return;

    setPage(nextPage);
    fetchReservations(nextPage);
  };

  if (isLoading) {
    return <LoadingView />;
  }

  return (
    <main className="min-h-screen bg-stone-50">
      <div className="mx-auto max-w-6xl px-4 py-6 sm:py-10">
        <nav className="mb-5 text-sm text-slate-500">
          <Link href="/" className="hover:text-sky-700">
            ホーム
          </Link>
          <span className="mx-2">&gt;</span>
          <span className="text-slate-700">予約一覧・履歴</span>
        </nav>

        <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
          <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-8 text-center sm:px-8 sm:py-10">
            <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
              RESERVATION HISTORY
            </p>

            <h1 className="mt-3 text-2xl font-semibold text-slate-800 md:text-3xl">
              予約一覧・履歴
            </h1>

            <p className="mt-3 text-sm leading-6 text-slate-500">
              予約済み・決済済み・キャンセル済みの予約を確認できます。
            </p>
          </div>

          <div className="grid gap-6 p-5 md:p-8">
            {isReserved && (
              <div className="rounded-2xl border border-emerald-200 bg-emerald-50 px-5 py-4 text-sm leading-6 text-emerald-800">
                <p className="font-semibold">予約が完了しました。</p>
                <p className="mt-1">
                  ご予約ありがとうございます。予約内容は下記の一覧から確認できます。
                </p>
              </div>
            )}

            {errorMessage && (
              <div className="rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">
                {errorMessage}
              </div>
            )}

            {!errorMessage && data && data.items.length === 0 && (
              <div className="rounded-2xl border border-stone-200 bg-stone-50 px-5 py-8 text-center">
                <p className="text-sm font-medium text-slate-700">
                  予約履歴はまだありません。
                </p>
                <p className="mt-2 text-sm text-slate-500">
                  スタジオ詳細ページから予約できます。
                </p>

                <div className="mt-5">
                  <Link
                    href="/rooms"
                    className="inline-flex rounded-xl bg-sky-700 px-6 py-3 text-sm font-medium text-white shadow-sm transition hover:opacity-90"
                  >
                    スタジオを探す
                  </Link>
                </div>
              </div>
            )}

            {!errorMessage && data && data.items.length > 0 && (
              <>
                <div className="hidden overflow-hidden rounded-2xl border border-stone-200 md:block">
                  <table className="w-full text-sm">
                    <thead className="bg-stone-50 text-slate-600">
                      <tr>
                        <th className="px-4 py-3 text-left font-medium">
                          スタジオ
                        </th>
                        <th className="px-4 py-3 text-left font-medium">
                          利用開始
                        </th>
                        <th className="px-4 py-3 text-left font-medium">
                          利用終了
                        </th>
                        <th className="px-4 py-3 text-right font-medium">
                          料金
                        </th>
                        <th className="px-4 py-3 text-center font-medium">
                          状態
                        </th>
                        <th className="px-4 py-3 text-center font-medium">
                          操作
                        </th>
                      </tr>
                    </thead>

                    <tbody className="divide-y divide-stone-100 bg-white">
                      {data.items.map((reservation) => (
                        <tr key={reservation.reservationId}>
                          <td className="px-4 py-3">
                            <Link
                              href={`/rooms/${reservation.roomId}`}
                              className="font-medium text-sky-700 hover:text-sky-800"
                            >
                              {reservation.roomName}
                            </Link>
                            <div className="mt-1 text-xs text-slate-400">
                              {reservation.roomAddress}
                            </div>
                          </td>

                          <td className="px-4 py-3 text-slate-700">
                            {formatDateTime(reservation.startAt)}
                          </td>

                          <td className="px-4 py-3 text-slate-700">
                            {formatDateTime(reservation.endAt)}
                          </td>

                          <td className="px-4 py-3 text-right font-semibold text-slate-800">
                            {formatYen(reservation.amount)}
                          </td>

                          <td className="px-4 py-3 text-center">
                            <span
                              className={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ${getStatusClassName(
                                reservation.status
                              )}`}
                            >
                              {getStatusLabel(reservation.status)}
                            </span>
                          </td>

                          <td className="px-4 py-3 text-center">
                            {reservation.status === "paid" ? (
                              <Link
                                href={`/rooms/${reservation.roomId}/reviews/new?reservationId=${reservation.reservationId}`}
                                className="inline-flex rounded-xl border border-sky-200 px-3 py-2 text-xs font-medium text-sky-700 transition hover:bg-sky-50"
                              >
                                レビューを書く
                              </Link>
                            ) : (
                              <span className="text-xs text-slate-400">―</span>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                <div className="grid gap-4 md:hidden">
                  {data.items.map((reservation) => (
                    <article
                      key={reservation.reservationId}
                      className="overflow-hidden rounded-2xl border border-stone-200 bg-white"
                    >
                      <img
                        src={getImageSrc(reservation.roomImageName)}
                        alt={reservation.roomName}
                        className="h-40 w-full object-cover"
                      />

                      <div className="grid gap-3 p-4">
                        <div className="flex items-start justify-between gap-3">
                          <div>
                            <Link
                              href={`/rooms/${reservation.roomId}`}
                              className="text-base font-semibold text-sky-700 hover:text-sky-800"
                            >
                              {reservation.roomName}
                            </Link>
                            <p className="mt-1 text-xs leading-5 text-slate-500">
                              {reservation.roomAddress}
                            </p>
                          </div>

                          <span
                            className={`shrink-0 rounded-full px-3 py-1 text-xs font-semibold ${getStatusClassName(
                              reservation.status
                            )}`}
                          >
                            {getStatusLabel(reservation.status)}
                          </span>
                        </div>

                        <div className="rounded-xl bg-stone-50 px-4 py-3 text-sm text-slate-700">
                          <div>
                            <span className="font-medium">開始：</span>
                            {formatDateTime(reservation.startAt)}
                          </div>
                          <div className="mt-1">
                            <span className="font-medium">終了：</span>
                            {formatDateTime(reservation.endAt)}
                          </div>
                        </div>

                        <div className="flex items-center justify-between border-t border-stone-100 pt-3">
                          <span className="text-sm text-slate-500">料金</span>
                          <span className="text-base font-bold text-slate-800">
                            {formatYen(reservation.amount)}
                          </span>
                        </div>

                        {reservation.status === "paid" && (
                          <Link
                            href={`/rooms/${reservation.roomId}/reviews/new?reservationId=${reservation.reservationId}`}
                            className="inline-flex justify-center rounded-xl border border-sky-200 px-4 py-3 text-sm font-medium text-sky-700 transition hover:bg-sky-50"
                          >
                            レビューを書く
                          </Link>
                        )}
                      </div>
                    </article>
                  ))}
                </div>

                <Pagination
                  page={data.page}
                  totalPages={data.totalPages}
                  onMovePage={handleMovePage}
                />
              </>
            )}

            <div className="flex justify-center">
              <Link
                href="/rooms"
                className="inline-flex justify-center rounded-xl border border-stone-300 px-6 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
              >
                スタジオ一覧へ
              </Link>
            </div>
          </div>
        </section>
      </div>
    </main>
  );
}

function Pagination({
  page,
  totalPages,
  onMovePage,
}: {
  page: number;
  totalPages: number;
  onMovePage: (page: number) => void;
}) {
  if (totalPages <= 1) {
    return null;
  }

  const pages = Array.from({ length: totalPages }, (_, index) => index + 1);

  return (
    <div className="flex flex-wrap items-center justify-center gap-2 pt-2">
      <button
        type="button"
        disabled={page <= 1}
        onClick={() => onMovePage(page - 1)}
        className="rounded-xl border border-stone-300 px-4 py-2 text-sm text-slate-700 transition hover:border-sky-300 hover:text-sky-700 disabled:cursor-not-allowed disabled:opacity-40"
      >
        前
      </button>

      {pages.map((item) => (
        <button
          key={item}
          type="button"
          onClick={() => onMovePage(item)}
          className={
            item === page
              ? "rounded-xl bg-sky-700 px-4 py-2 text-sm font-semibold text-white"
              : "rounded-xl border border-stone-300 px-4 py-2 text-sm text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
          }
        >
          {item}
        </button>
      ))}

      <button
        type="button"
        disabled={page >= totalPages}
        onClick={() => onMovePage(page + 1)}
        className="rounded-xl border border-stone-300 px-4 py-2 text-sm text-slate-700 transition hover:border-sky-300 hover:text-sky-700 disabled:cursor-not-allowed disabled:opacity-40"
      >
        次
      </button>
    </div>
  );
}

function LoadingView() {
  return (
    <main className="min-h-screen bg-stone-50">
      <div className="mx-auto max-w-6xl px-4 py-12">
        <div className="rounded-2xl border border-stone-200 bg-white px-5 py-6 shadow-sm">
          <p className="text-sm text-slate-500">予約一覧を読み込み中...</p>
        </div>
      </div>
    </main>
  );
}