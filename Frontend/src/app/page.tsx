"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";

type TopRoomItem = {
  id: number;
  name: string;
  address: string;
  price: number;
  imageName: string | null;
  averageScore: number | null;
  reviewCount: number;
};

type TopPageResponse = {
  popularRooms: TopRoomItem[];
  newRooms: TopRoomItem[];
};

function formatYenPerHour(value: number) {
  return `${new Intl.NumberFormat("ja-JP").format(value)}円 / h`;
}

function getRoomImageSrc(imageName: string | null) {
  return imageName ? `/storage/${imageName}` : "/images/noImage.png";
}

function renderStars(rating: number | null) {
  if (rating == null) {
    return <span className="text-sm text-slate-400">レビューなし</span>;
  }

  const width = `${Math.max(0, Math.min(100, (rating / 5) * 100))}%`;

  return (
    <div className="flex items-center gap-2">
      <div className="relative inline-block text-sm leading-none">
        <div className="text-gray-300">★★★★★</div>
        <div
          className="absolute left-0 top-0 overflow-hidden whitespace-nowrap text-amber-400"
          style={{ width }}
        >
          ★★★★★
        </div>
      </div>
      <span className="text-sm text-slate-600">
        {rating.toFixed(1)} / 5
      </span>
    </div>
  );
}

export default function StudioBookTopPage() {
  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const router = useRouter();

  const [keyword, setKeyword] = useState("");
  const [popularRooms, setPopularRooms] = useState<TopRoomItem[]>([]);
  const [newRooms, setNewRooms] = useState<TopRoomItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    let ignore = false;

    const fetchTopRooms = async () => {
      setIsLoading(true);
      setErrorMessage("");

      try {
const response = await fetch(`${apiBaseUrl}/api/home`, {
  method: "GET",
  cache: "no-store",
});
        if (!response.ok) {
          setErrorMessage("トップページ情報の取得に失敗しました。");
          return;
        }

        const data = (await response.json()) as TopPageResponse;

        if (ignore) return;

        setPopularRooms(data.popularRooms ?? []);
        setNewRooms(data.newRooms ?? []);
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

    fetchTopRooms();

    return () => {
      ignore = true;
    };
  }, [apiBaseUrl]);

  const handleSearch = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    const trimmedKeyword = keyword.trim();

if (!trimmedKeyword) {
  router.push("/rooms");
  return;
}

router.push(`/rooms?keyword=${encodeURIComponent(trimmedKeyword)}`);
  };

  return (
    <main>
      <section className="relative h-[440px] overflow-hidden">
        <img
          src="/images/main1.jpg"
          alt="Studio Book メインビジュアル"
          className="h-full w-full object-cover"
        />
        <div className="absolute inset-0 bg-slate-900/45" />

        <div className="absolute inset-0 flex items-center">
          <div className="mx-auto max-w-6xl px-4 text-white">
            <div className="max-w-2xl">
              <p className="text-sm font-medium tracking-[0.3em] text-white/80">
                STUDIO BOOK
              </p>

              <h1 className="mt-4 text-4xl font-semibold leading-tight md:text-5xl">
                架空のスタジオ予約サイトを、
                <br />
                実務想定で使いやすく。
              </h1>

              <p className="mt-5 text-sm leading-7 text-white/85 md:text-base">
                ダンス、撮影、楽器練習、ワークショップなど、
                用途に合わせたスタジオ予約フローを再現したポートフォリオ用デモサイトです。
              </p>

              <div className="mt-4 inline-flex max-w-full rounded-2xl border border-white/25 bg-white/10 px-4 py-3 text-xs leading-6 text-white/85 backdrop-blur-sm sm:text-sm">
                ※掲載しているスタジオ名・住所・レビュー等は架空のデモデータです。写真はAI生成画像を使用しています。
              </div>

              <div className="mt-8 flex flex-wrap gap-3">
                <Link
                  href="/rooms"
                  className="rounded-xl bg-sky-700 px-5 py-3 text-sm font-medium text-white hover:opacity-90"
                >
                  スタジオ一覧を見る
                </Link>
                <Link
                  href="/signup"
                  className="rounded-xl border border-white/40 px-5 py-3 text-sm font-medium text-white hover:bg-white/10"
                >
                  会員登録
                </Link>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="border-b border-stone-200 bg-white">
        <div className="mx-auto max-w-6xl px-4 py-6">
          <form onSubmit={handleSearch} className="mx-auto max-w-3xl">
            <div className="flex overflow-hidden rounded-2xl border border-stone-300 bg-white shadow-sm focus-within:border-sky-500">
              <input
                type="text"
                value={keyword}
                onChange={(e) => setKeyword(e.target.value)}
                placeholder="スタジオ名・住所で検索"
                className="w-full px-4 py-3 text-sm outline-none"
              />
              <button
                type="submit"
                className="bg-sky-700 px-5 py-3 text-sm font-medium text-white transition hover:opacity-90"
              >
                検索
              </button>
            </div>
          </form>
        </div>
      </section>

      <section className="mx-auto max-w-6xl px-4 py-10">
        {errorMessage && (
          <div className="mb-6 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
            {errorMessage}
          </div>
        )}

        {isLoading ? (
          <p className="text-sm text-slate-500">スタジオ情報を読み込み中...</p>
        ) : (
          <div className="space-y-12">
            <section>
              <div className="mb-4 flex items-end justify-between">
                <h2 className="text-2xl font-semibold text-slate-800">
                  人気のスタジオ
                </h2>
                <Link
                  href="/rooms"
                  className="text-sm text-sky-700 hover:text-sky-800"
                >
                  もっと見る
                </Link>
              </div>

              {popularRooms.length === 0 ? (
                <EmptyMessage message="人気のスタジオはまだありません。" />
              ) : (
                <div className="grid gap-6 md:grid-cols-2 xl:grid-cols-3">
                  {popularRooms.map((room) => (
                    <RoomCard key={room.id} room={room} showReview />
                  ))}
                </div>
              )}
            </section>

            <section>
              <div className="mb-4 flex items-end justify-between">
                <h2 className="text-2xl font-semibold text-slate-800">
                  新着スタジオ
                </h2>
                <Link
                  href="/rooms"
                  className="text-sm text-sky-700 hover:text-sky-800"
                >
                  一覧へ
                </Link>
              </div>

              {newRooms.length === 0 ? (
                <EmptyMessage message="新着スタジオはまだありません。" />
              ) : (
                <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
                  {newRooms.map((room) => (
                    <RoomCard key={room.id} room={room} showReview />
                  ))}
                </div>
              )}
            </section>
          </div>
        )}
      </section>
    </main>
  );
}

function RoomCard({
  room,
  showReview,
}: {
  room: TopRoomItem;
  showReview?: boolean;
}) {
  return (
    <Link
      href={`/rooms/${room.id}`}
      className="block overflow-hidden rounded-3xl border border-stone-200 bg-white shadow-sm transition hover:opacity-95"
    >
      <img
        src={getRoomImageSrc(room.imageName)}
        alt={room.name}
        className="h-52 w-full object-cover"
      />

      <div className="p-5">
        <h3 className="text-lg font-semibold text-slate-800">
          {room.name}
        </h3>

        <p className="mt-1 line-clamp-2 text-sm text-slate-500">
          {room.address}
        </p>

        <p className="mt-3 text-sm font-medium text-slate-800">
          {formatYenPerHour(room.price)}
        </p>

        {showReview && (
          <div className="mt-3">
            {room.averageScore == null ? (
              <span className="text-sm text-slate-400">レビューなし</span>
            ) : (
              <div className="flex flex-wrap items-center gap-2">
                {renderStars(room.averageScore)}
                <span className="text-xs text-slate-400">
                  （{room.reviewCount}件）
                </span>
              </div>
            )}
          </div>
        )}
      </div>
    </Link>
  );
}

function EmptyMessage({ message }: { message: string }) {
  return (
    <div className="rounded-2xl border border-dashed border-stone-300 bg-stone-50 px-4 py-10 text-center">
      <p className="text-sm text-slate-500">{message}</p>
    </div>
  );
}