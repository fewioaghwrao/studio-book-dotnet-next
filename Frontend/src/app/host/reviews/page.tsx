"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { apiFetch } from "@/lib/apiFetch";

type HostReviewRow = {
  id: number;
  roomId: number;
  roomName: string;
  userId: number;
  userName: string;
  score: number;
  content: string;
  publicVisible: boolean;
  hiddenReason: string | null;
  hostReply: string | null;
  hostReplyAt: string | null;
  createdAtUtc: string;
};

type RoomOption = {
  id: number;
  name: string;
};

type HostReviewListResponse = {
  items: HostReviewRow[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  roomOptions: RoomOption[];
};

function formatDate(value: string) {
  if (!value) return "-";

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("ja-JP", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(date);
}

function formatDateTime(value: string | null) {
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

function StarRating({ score }: { score: number }) {
  return (
    <div className="flex items-center gap-2">
      <div className="text-base tracking-tight text-amber-400">
        {"★★★★★".slice(0, score)}
        <span className="text-slate-300">{"★★★★★".slice(score)}</span>
      </div>
      <span className="text-xs text-slate-500">{score}/5</span>
    </div>
  );
}

function visibilityClassName(publicVisible: boolean) {
  return publicVisible
    ? "bg-emerald-50 text-emerald-700 ring-emerald-200"
    : "bg-slate-100 text-slate-600 ring-slate-200";
}

export default function HostReviewsPage() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const currentRoomId = searchParams.get("roomId") ?? "";
  const currentStars = searchParams.get("stars") ?? "";
  const currentOnlyHidden = searchParams.get("onlyHidden") ?? "";
  const currentPage = Number(searchParams.get("page") ?? "1");

  const [roomId, setRoomId] = useState(currentRoomId);
  const [stars, setStars] = useState(currentStars);
  const [onlyHidden, setOnlyHidden] = useState(currentOnlyHidden === "true");

  const [rows, setRows] = useState<HostReviewRow[]>([]);
  const [roomOptions, setRoomOptions] = useState<RoomOption[]>([]);
  const [replyDrafts, setReplyDrafts] = useState<Record<number, string>>({});
  const [visibilityReasons, setVisibilityReasons] = useState<Record<number, string>>({});

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

    if (roomId) params.set("roomId", roomId);
    if (stars) params.set("stars", stars);
    if (onlyHidden) params.set("onlyHidden", "true");

    params.set("page", String(page));
    params.set("pageSize", "10");

    return params.toString();
  };

  const fetchReviews = async () => {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const params = new URLSearchParams();

      if (currentRoomId) params.set("roomId", currentRoomId);
      if (currentStars) params.set("stars", currentStars);
      if (currentOnlyHidden === "true") params.set("onlyHidden", "true");

      params.set("page", String(currentPage > 0 ? currentPage : 1));
      params.set("pageSize", "10");

      const response = await apiFetch(
        `${apiBaseUrl}/api/host/reviews?${params.toString()}`,
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
        setErrorMessage("レビュー一覧の取得に失敗しました。");
        return;
      }

      const data = (await response.json()) as HostReviewListResponse;

      setRows(data.items ?? []);
      setRoomOptions(data.roomOptions ?? []);
      setPageInfo({
        page: data.page,
        pageSize: data.pageSize,
        totalCount: data.totalCount,
        totalPages: data.totalPages || 1,
      });

      const drafts: Record<number, string> = {};
      for (const item of data.items ?? []) {
        drafts[item.id] = item.hostReply ?? "";
      }
      setReplyDrafts(drafts);
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    setRoomId(currentRoomId);
    setStars(currentStars);
    setOnlyHidden(currentOnlyHidden === "true");
  }, [currentRoomId, currentStars, currentOnlyHidden]);

  useEffect(() => {
    fetchReviews();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [apiBaseUrl, currentRoomId, currentStars, currentOnlyHidden, currentPage]);

  const handleSearch = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    router.push(`/host/reviews?${buildQueryString(1)}`);
  };

  const handleClear = () => {
    router.push("/host/reviews");
  };

  const movePage = (page: number) => {
    router.push(`/host/reviews?${buildQueryString(page)}`);
  };

  const handleSaveReply = async (reviewId: number) => {
    setProcessingId(reviewId);
    setErrorMessage("");
    setSuccessMessage("");

    try {
      const response = await apiFetch(
        `${apiBaseUrl}/api/host/reviews/${reviewId}/reply`,
        {
          method: "POST",          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            hostReply: replyDrafts[reviewId] ?? "",
          }),
        }
      );

      if (response.status === 401) {
        window.location.href = "/auth/login";
        return;
      }

      if (!response.ok) {
        const data = await response.json().catch(() => null);
        setErrorMessage(data?.message ?? "返信の保存に失敗しました。");
        return;
      }

      setSuccessMessage("返信を保存しました。");
      await fetchReviews();
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    } finally {
      setProcessingId(null);
    }
  };

  const handleToggleVisibility = async (review: HostReviewRow) => {
    const nextPublic = !review.publicVisible;

    if (!nextPublic) {
      const ok = window.confirm("このレビューを非公開にしますか？");
      if (!ok) return;
    }

    setProcessingId(review.id);
    setErrorMessage("");
    setSuccessMessage("");

    try {
      const response = await apiFetch(
        `${apiBaseUrl}/api/host/reviews/${review.id}/visibility`,
        {
          method: "POST",          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            isPublic: nextPublic,
            reason: nextPublic ? null : visibilityReasons[review.id] || null,
          }),
        }
      );

      if (response.status === 401) {
        window.location.href = "/auth/login";
        return;
      }

      if (!response.ok) {
        const data = await response.json().catch(() => null);
        setErrorMessage(data?.message ?? "公開状態の変更に失敗しました。");
        return;
      }

      setSuccessMessage(nextPublic ? "公開に変更しました。" : "非公開に変更しました。");
      await fetchReviews();
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
        <span className="text-slate-700">レビュー一覧</span>
      </nav>

      <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
        <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 text-center sm:px-8 sm:py-9">
          <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
            HOST REVIEWS
          </p>
          <h1 className="mt-3 text-xl font-semibold text-slate-800 sm:text-2xl md:text-3xl">
            レビュー管理
          </h1>
          <p className="mt-3 text-sm leading-6 text-slate-500">
            自分が管理するスタジオのレビュー確認、返信、公開状態の切り替えができます。
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
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
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
                  評価
                </label>
                <select
                  value={stars}
                  onChange={(e) => setStars(e.target.value)}
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                >
                  <option value="">すべて</option>
                  {[1, 2, 3, 4, 5].map((score) => (
                    <option key={score} value={score}>
                      {score} ★
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex items-end">
                <label className="flex w-full cursor-pointer items-center gap-3 rounded-xl border border-stone-300 bg-white px-4 py-3 text-sm text-slate-700">
                  <input
                    type="checkbox"
                    checked={onlyHidden}
                    onChange={(e) => setOnlyHidden(e.target.checked)}
                    className="h-4 w-4 rounded border-stone-300 text-sky-700"
                  />
                  非公開のみ
                </label>
              </div>

              <div className="flex items-end gap-3">
                <button
                  type="submit"
                  className="w-full rounded-xl bg-sky-700 px-6 py-3 text-sm font-medium text-white shadow-sm transition hover:opacity-90"
                >
                  絞り込む
                </button>
              </div>
            </div>

            <div className="mt-4">
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
                レビュー一覧
              </h2>
              <p className="mt-1 text-sm text-slate-500">
                全 {pageInfo.totalCount} 件
              </p>
            </div>
          </div>

          {isLoading ? (
            <p className="text-sm text-slate-500">レビューを読み込み中...</p>
          ) : rows.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-stone-300 bg-stone-50 px-4 py-10 text-center">
              <p className="text-sm text-slate-500">
                該当するレビューはありません。
              </p>
            </div>
          ) : (
            <>
              <div className="grid gap-4">
                {rows.map((review) => (
                  <article
                    key={review.id}
                    className="rounded-2xl border border-stone-200 bg-white p-5 shadow-sm"
                  >
                    <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                      <div>
                        <p className="text-xs font-medium tracking-[0.18em] text-sky-700">
                          REVIEW #{review.id}
                        </p>
                        <h3 className="mt-1 text-lg font-semibold text-slate-800">
                          {review.roomName}
                        </h3>
                        <p className="mt-1 text-sm text-slate-500">
                          {review.userName} / {formatDate(review.createdAtUtc)}
                        </p>
                      </div>

                      <div className="flex flex-wrap items-center gap-2">
                        <StarRating score={review.score} />
                        <span
                          className={`rounded-full px-3 py-1 text-xs font-medium ring-1 ${visibilityClassName(
                            review.publicVisible
                          )}`}
                        >
                          {review.publicVisible ? "公開" : "非公開"}
                        </span>
                      </div>
                    </div>

                    <div className="rounded-2xl bg-stone-50 px-4 py-3 text-sm leading-6 text-slate-700">
                      {review.content}
                    </div>

                    {!review.publicVisible && review.hiddenReason && (
                      <div className="mt-3 rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
                        非公開理由：{review.hiddenReason}
                      </div>
                    )}

                    <div className="mt-5 grid gap-4 lg:grid-cols-[1fr_340px]">
                      <div>
                        <label className="mb-1 block text-xs font-semibold text-slate-500">
                          ホストからの返信
                        </label>
                        <textarea
                          rows={3}
                          value={replyDrafts[review.id] ?? ""}
                          onChange={(e) =>
                            setReplyDrafts((current) => ({
                              ...current,
                              [review.id]: e.target.value,
                            }))
                          }
                          placeholder="ホストからの返信を入力"
                          className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                        />

                        {review.hostReplyAt && (
                          <p className="mt-1 text-xs text-slate-400">
                            最終更新：{formatDateTime(review.hostReplyAt)}
                          </p>
                        )}

                        <div className="mt-3">
                          <button
                            type="button"
                            onClick={() => handleSaveReply(review.id)}
                            disabled={processingId === review.id}
                            className="rounded-xl bg-sky-700 px-5 py-3 text-sm font-medium text-white transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
                          >
                            返信を保存
                          </button>
                        </div>
                      </div>

                      <div className="rounded-2xl border border-stone-200 bg-stone-50 p-4">
                        <p className="text-sm font-semibold text-slate-700">
                          公開状態の変更
                        </p>

                        {review.publicVisible && (
                          <div className="mt-3">
                            <label className="mb-1 block text-xs font-semibold text-slate-500">
                              非公開理由
                            </label>
                            <input
                              type="text"
                              value={visibilityReasons[review.id] ?? ""}
                              onChange={(e) =>
                                setVisibilityReasons((current) => ({
                                  ...current,
                                  [review.id]: e.target.value,
                                }))
                              }
                              placeholder="任意"
                              className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                            />
                          </div>
                        )}

                        <button
                          type="button"
                          onClick={() => handleToggleVisibility(review)}
                          disabled={processingId === review.id}
                          className={
                            review.publicVisible
                              ? "mt-4 w-full rounded-xl border border-slate-300 bg-white px-4 py-3 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
                              : "mt-4 w-full rounded-xl bg-emerald-600 px-4 py-3 text-sm font-medium text-white transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
                          }
                        >
                          {review.publicVisible ? "非公開にする" : "公開に戻す"}
                        </button>
                      </div>
                    </div>
                  </article>
                ))}
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