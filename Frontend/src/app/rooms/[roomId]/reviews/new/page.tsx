"use client";

import Link from "next/link";
import { FormEvent, Suspense, useEffect, useMemo, useState } from "react";
import { useParams, useRouter, useSearchParams } from "next/navigation";

type RoomReviewPageResponse = {
  roomId: number;
  roomName: string;
  roomImageName: string | null;
  roomAddress: string;
  averageScore: number;
  reviewCount: number;
  reviews: ReviewListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  alreadyReviewed: boolean;
  canReview: boolean;
};

type ReviewListItem = {
  id: number;
  score: number;
  content: string;
  userName: string;
  createdAtUtc: string;
};

type AiReviewAssistResponse = {
  assistedContent: string;
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

function renderStars(score: number) {
  const rounded = Math.round(score);

  return "★★★★★"
    .split("")
    .map((star, index) => (index < rounded ? "★" : "☆"))
    .join("");
}

export default function ReviewNewPage() {
  return (
    <Suspense fallback={<LoadingView />}>
      <ReviewNewContent />
    </Suspense>
  );
}

function ReviewNewContent() {
  const params = useParams<{ roomId: string }>();
  const searchParams = useSearchParams();
  const router = useRouter();

  const reservationId = searchParams.get("reservationId");

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [data, setData] = useState<RoomReviewPageResponse | null>(null);
  const [score, setScore] = useState<number>(5);
  const [content, setContent] = useState("");
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isAssisting, setIsAssisting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  const fetchReviewPage = async (targetPage: number) => {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const query = new URLSearchParams({
        page: String(targetPage),
        pageSize: "10",
      });

      if (reservationId) {
        query.set("reservationId", reservationId);
      }

      const response = await fetch(
        `${apiBaseUrl}/api/rooms/${params.roomId}/reviews/new?${query.toString()}`,
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

      if (response.status === 404) {
        setErrorMessage("スタジオが見つかりません。");
        return;
      }

      if (!response.ok) {
        setErrorMessage("レビュー画面の取得に失敗しました。");
        return;
      }

      const json = (await response.json()) as RoomReviewPageResponse;
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
    fetchReviewPage(page);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [apiBaseUrl, params.roomId, reservationId]);

  const handleAssistReview = async () => {
    setErrorMessage("");
    setSuccessMessage("");

    const trimmedContent = content.trim();

    if (!trimmedContent) {
      setErrorMessage("AIで整える前に、レビューの下書きを入力してください。");
      return;
    }

    if (trimmedContent.length > 1000) {
      setErrorMessage("コメントは1000文字以内で入力してください。");
      return;
    }

    if (score < 1 || score > 5) {
      setErrorMessage("評価は1〜5で選択してください。");
      return;
    }

    setIsAssisting(true);

    try {
      const response = await fetch(`${apiBaseUrl}/api/ai/review-assist`, {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
        },
        cache: "no-store",
        body: JSON.stringify({
          content: trimmedContent,
          score,
          roomName: data?.roomName ?? "",
        }),
      });

      const json = await response.json().catch(() => null);

      if (response.status === 401) {
        window.location.href = "/auth/login";
        return;
      }

      if (!response.ok) {
        setErrorMessage(
          json?.message ??
            "AIによるレビュー文補助に失敗しました。時間をおいて再度お試しください。"
        );
        return;
      }

      const assisted = (json as AiReviewAssistResponse | null)?.assistedContent;

      if (!assisted) {
        setErrorMessage("AIレビュー文補助の結果が空でした。");
        return;
      }

      setContent(assisted);
      setSuccessMessage(
        "AIがレビュー文を整えました。内容を確認してから投稿してください。"
      );
    } catch {
      setErrorMessage(
        "通信エラーが発生しました。バックエンドの起動状態を確認してください。"
      );
    } finally {
      setIsAssisting(false);
    }
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setErrorMessage("");
    setSuccessMessage("");

    if (score < 1 || score > 5) {
      setErrorMessage("評価は1〜5で選択してください。");
      return;
    }

    if (!content.trim()) {
      setErrorMessage("コメントを入力してください。");
      return;
    }

    if (content.length > 1000) {
      setErrorMessage("コメントは1000文字以内で入力してください。");
      return;
    }

    setIsSubmitting(true);

    try {
      const response = await fetch(
        `${apiBaseUrl}/api/rooms/${params.roomId}/reviews`,
        {
          method: "POST",
          credentials: "include",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            reservationId: reservationId ? Number(reservationId) : null,
            score,
            content,
          }),
        }
      );

      const json = await response.json().catch(() => null);

      if (response.status === 401) {
        window.location.href = "/auth/login";
        return;
      }

      if (response.status === 400) {
        setErrorMessage(json?.message ?? "レビューを投稿できませんでした。");
        return;
      }

      if (response.status === 404) {
        setErrorMessage("スタジオが見つかりません。");
        return;
      }

      if (!response.ok) {
        setErrorMessage("レビュー投稿に失敗しました。");
        return;
      }

      setSuccessMessage("レビューを投稿しました。");
      setContent("");
      setScore(5);

      await fetchReviewPage(1);

      setTimeout(() => {
        router.push("/reservations");
      }, 700);
    } catch {
      setErrorMessage(
        "通信エラーが発生しました。時間をおいて再度お試しください。"
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleMovePage = (nextPage: number) => {
    if (!data) return;
    if (nextPage < 1 || nextPage > data.totalPages) return;

    setPage(nextPage);
    fetchReviewPage(nextPage);
  };

  if (isLoading) {
    return <LoadingView />;
  }

  if (!data) {
    return (
      <main className="min-h-screen bg-stone-50">
        <div className="mx-auto max-w-5xl px-4 py-12">
          <div className="rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">
            {errorMessage || "レビュー画面を表示できませんでした。"}
          </div>

          <div className="mt-6">
            <Link
              href="/reservations"
              className="text-sm font-medium text-sky-700 hover:text-sky-800"
            >
              予約一覧へ戻る
            </Link>
          </div>
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-stone-50">
      <div className="mx-auto max-w-5xl px-4 py-6 sm:py-10">
        <nav className="mb-5 text-sm text-slate-500">
          <Link href="/" className="hover:text-sky-700">
            ホーム
          </Link>
          <span className="mx-2">&gt;</span>
          <Link href="/reservations" className="hover:text-sky-700">
            予約一覧
          </Link>
          <span className="mx-2">&gt;</span>
          <span className="text-slate-700">レビューを書く</span>
        </nav>

        <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
          <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-8 text-center sm:px-8 sm:py-10">
            <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
              WRITE REVIEW
            </p>
            <h1 className="mt-3 text-2xl font-semibold text-slate-800 md:text-3xl">
              レビューを書く
            </h1>
            <p className="mt-3 text-sm leading-6 text-slate-500">
              利用したスタジオの感想を投稿できます。
            </p>
          </div>

          <div className="grid gap-8 p-5 md:p-8">
            <div className="overflow-hidden rounded-2xl border border-stone-200 bg-white">
              <div className="grid md:grid-cols-[260px_1fr]">
                <img
                  src={getImageSrc(data.roomImageName)}
                  alt={data.roomName}
                  className="h-52 w-full object-cover md:h-full"
                />

                <div className="grid content-center gap-3 p-5">
                  <h2 className="text-xl font-semibold text-slate-800">
                    {data.roomName}
                  </h2>
                  <p className="text-sm leading-6 text-slate-500">
                    {data.roomAddress}
                  </p>

                  <div className="flex flex-wrap items-center gap-2 text-sm">
                    <span className="text-amber-500">
                      {renderStars(data.averageScore)}
                    </span>
                    <span className="font-semibold text-slate-700">
                      {data.averageScore.toFixed(1)} / 5
                    </span>
                    <span className="text-slate-500">
                      （{data.reviewCount}件）
                    </span>
                  </div>
                </div>
              </div>
            </div>

            {errorMessage && (
              <div className="rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700">
                {errorMessage}
              </div>
            )}

            {successMessage && (
              <div className="rounded-2xl border border-emerald-200 bg-emerald-50 px-5 py-4 text-sm text-emerald-700">
                {successMessage}
              </div>
            )}

            {!data.canReview && (
              <div className="rounded-2xl border border-amber-200 bg-amber-50 px-5 py-4 text-sm leading-6 text-amber-800">
                {data.alreadyReviewed
                  ? "このスタジオへのレビューは投稿済みです。"
                  : "レビュー投稿は、決済済みの予約があるユーザーのみ可能です。"}
              </div>
            )}

            {data.canReview && (
              <form
                className="rounded-2xl border border-stone-200 bg-white p-5"
                onSubmit={handleSubmit}
              >
                <div>
                  <label className="mb-3 block text-sm font-semibold text-slate-700">
                    評価
                  </label>

                  <div className="flex flex-row-reverse justify-end gap-2">
                    {[5, 4, 3, 2, 1].map((value) => (
                      <button
                        key={value}
                        type="button"
                        onClick={() => setScore(value)}
                        className={
                          value <= score
                            ? "text-3xl text-amber-400 transition hover:scale-105"
                            : "text-3xl text-stone-300 transition hover:scale-105"
                        }
                        aria-label={`${value}点`}
                      >
                        ★
                      </button>
                    ))}
                  </div>

                  <p className="mt-2 text-sm text-slate-500">
                    選択中: {score} / 5
                  </p>
                </div>

                <div className="mt-5">
                  <label
                    htmlFor="content"
                    className="mb-2 block text-sm font-semibold text-slate-700"
                  >
                    コメント
                  </label>
                  <textarea
                    id="content"
                    value={content}
                    maxLength={1000}
                    rows={6}
                    onChange={(e) => setContent(e.target.value)}
                    placeholder="良かった点、気になった点などを入力してください。"
                    className="w-full rounded-2xl border border-stone-300 px-4 py-3 text-sm leading-6 outline-none transition focus:border-sky-500"
                  />
                  <div className="mt-2 flex justify-between text-xs text-slate-400">
                    <span>1000文字まで入力できます。</span>
                    <span>{content.length}/1000</span>
                  </div>

                  <div className="mt-4 rounded-2xl border border-sky-100 bg-sky-50 px-4 py-4">
                    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                      <div>
                        <p className="text-sm font-semibold text-slate-800">
                          AIレビュー文補助
                        </p>
                        <p className="mt-1 text-xs leading-5 text-slate-500">
                          入力した感想を、自然なレビュー文に整えます。
                        </p>
                      </div>

                      <button
                        type="button"
                        onClick={handleAssistReview}
                        disabled={isAssisting || isSubmitting || !content.trim()}
                        className="inline-flex justify-center rounded-xl border border-sky-200 bg-white px-4 py-2 text-sm font-medium text-sky-700 transition hover:bg-sky-100 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        {isAssisting ? "AI整文中..." : "レビュー文を整える"}
                      </button>
                    </div>

                    <p className="mt-3 text-xs leading-5 text-slate-500">
                      ※AIが生成した文章は参考です。内容を確認・修正してから投稿してください。
                    </p>
                  </div>
                </div>

                <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-center">
                  <Link
                    href="/reservations"
                    className="inline-flex justify-center rounded-xl border border-stone-300 px-6 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
                  >
                    戻る
                  </Link>

                  <button
                    type="submit"
                    disabled={isSubmitting || isAssisting}
                    className="inline-flex justify-center rounded-xl bg-sky-700 px-8 py-3 text-sm font-medium text-white shadow-sm transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    {isSubmitting ? "投稿中..." : "投稿する"}
                  </button>
                </div>
              </form>
            )}

            <section className="rounded-2xl border border-stone-200 bg-white p-5">
              <h2 className="text-lg font-semibold text-slate-800">
                この部屋のレビュー
              </h2>

              {data.reviews.length === 0 ? (
                <p className="mt-4 text-sm text-slate-500">
                  まだレビューはありません。
                </p>
              ) : (
                <div className="mt-5 grid gap-4">
                  {data.reviews.map((review) => (
                    <article
                      key={review.id}
                      className="rounded-2xl border border-stone-200 bg-stone-50 p-4"
                    >
                      <div className="flex flex-wrap items-center gap-2">
                        <span className="text-amber-500">
                          {renderStars(review.score)}
                        </span>
                        <span className="text-sm font-semibold text-slate-700">
                          {review.score} / 5
                        </span>
                        <span className="text-xs text-slate-400">
                          {formatDateTime(review.createdAtUtc)}
                        </span>
                      </div>

                      <p className="mt-3 whitespace-pre-wrap text-sm leading-6 text-slate-700">
                        {review.content}
                      </p>

                      <p className="mt-3 text-xs text-slate-400">
                        投稿者: {review.userName || "ユーザー"}
                      </p>
                    </article>
                  ))}
                </div>
              )}

              <Pagination
                page={data.page}
                totalPages={data.totalPages}
                onMovePage={handleMovePage}
              />
            </section>
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
    <div className="mt-5 flex flex-wrap items-center justify-center gap-2">
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
      <div className="mx-auto max-w-5xl px-4 py-12">
        <div className="rounded-2xl border border-stone-200 bg-white px-5 py-6 shadow-sm">
          <p className="text-sm text-slate-500">レビュー画面を読み込み中...</p>
        </div>
      </div>
    </main>
  );
}