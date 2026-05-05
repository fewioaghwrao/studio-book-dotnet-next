"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { apiFetch } from "@/lib/apiFetch";

type RoomListItem = {
  id: number;
  name: string;
  imageName: string | null;
  description: string;
  postalCode: string;
  address: string;
  price: number;
  averageScore: number | null;
  reviewCount: number;
};

type RoomListResponse = {
  items: RoomListItem[];
  keyword: string;
  area: string;
  price: number | null;
  order: string;
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

const areaGroups = [
  {
    label: "都心エリア",
    areas: ["千代田区", "中央区", "港区", "新宿区", "渋谷区"],
  },
  {
    label: "城西・城南エリア",
    areas: ["目黒区", "品川区", "大田区", "世田谷区", "中野区", "杉並区"],
  },
  {
    label: "城東・城北エリア",
    areas: ["文京区", "台東区", "墨田区", "江東区", "豊島区", "北区", "荒川区"],
  },
  {
    label: "その他23区",
    areas: ["板橋区", "練馬区", "足立区", "葛飾区", "江戸川区"],
  },
];

const priceOptions = [
  { value: "4000", label: "4,000円以内" },
  { value: "5000", label: "5,000円以内" },
  { value: "6000", label: "6,000円以内" },
  { value: "7000", label: "7,000円以内" },
  { value: "8000", label: "8,000円以内" },
  { value: "10000", label: "10,000円以内" },
];

function formatYenPerHour(value: number) {
  return `${new Intl.NumberFormat("ja-JP").format(value)}円 / h`;
}

function getRoomImageSrc(imageName: string | null) {
  return imageName ? `/storage/${imageName}` : "/images/noImage.png";
}

function formatScore(score: number | null) {
  if (score == null) return "レビューなし";
  return `${score.toFixed(1)} / 5.0`;
}

function Stars({ score }: { score: number | null }) {
  if (score == null) {
    return <span className="text-sm text-slate-400">レビューなし</span>;
  }

  const width = `${Math.max(0, Math.min(100, (score / 5) * 100))}%`;

  return (
    <div className="flex flex-wrap items-center gap-2">
      <div className="relative inline-block text-base leading-none">
        <div className="text-gray-300">★★★★★</div>
        <div
          className="absolute left-0 top-0 overflow-hidden whitespace-nowrap text-amber-400"
          style={{ width }}
        >
          ★★★★★
        </div>
      </div>
      <span className="text-sm text-slate-600">{formatScore(score)}</span>
    </div>
  );
}

export default function RoomsPage() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const currentKeyword = searchParams.get("keyword") ?? "";
  const currentArea = searchParams.get("area") ?? "";
  const currentPrice = searchParams.get("price") ?? "";
  const currentOrder = searchParams.get("order") ?? "createdAtDesc";
  const currentPage = Number(searchParams.get("page") ?? "1");

  const [keyword, setKeyword] = useState(currentKeyword);
  const [area, setArea] = useState(currentArea);
  const [price, setPrice] = useState(currentPrice);
  const [order, setOrder] = useState(currentOrder);
  const [items, setItems] = useState<RoomListItem[]>([]);
  const [pageInfo, setPageInfo] = useState({
    page: currentPage,
    pageSize: 10,
    totalCount: 0,
    totalPages: 1,
  });

  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  const buildQueryString = (page: number, nextOrder = order) => {
    const params = new URLSearchParams();

    if (keyword.trim()) params.set("keyword", keyword.trim());
    if (area) params.set("area", area);
    if (price) params.set("price", price);
    if (nextOrder) params.set("order", nextOrder);

    params.set("page", String(page));
    params.set("pageSize", "10");

    return params.toString();
  };

  const fetchRooms = async () => {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const params = new URLSearchParams();

      if (currentKeyword.trim()) params.set("keyword", currentKeyword.trim());
      if (currentArea) params.set("area", currentArea);
      if (currentPrice) params.set("price", currentPrice);
      if (currentOrder) params.set("order", currentOrder);

      params.set("page", String(currentPage > 0 ? currentPage : 1));
      params.set("pageSize", "10");

      const response = await apiFetch(
        `${apiBaseUrl}/api/rooms?${params.toString()}`,
        {
          method: "GET",
          cache: "no-store",
        }
      );

      if (!response.ok) {
        setErrorMessage("スタジオ一覧の取得に失敗しました。");
        return;
      }

      const data = (await response.json()) as RoomListResponse;

      setItems(data.items ?? []);
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
    setArea(currentArea);
    setPrice(currentPrice);
    setOrder(currentOrder);
  }, [currentKeyword, currentArea, currentPrice, currentOrder]);

  useEffect(() => {
    fetchRooms();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    apiBaseUrl,
    currentKeyword,
    currentArea,
    currentPrice,
    currentOrder,
    currentPage,
  ]);

  const handleKeywordSearch = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    router.push(`/rooms?${buildQueryString(1)}`);
  };

  const handleAreaSearch = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    router.push(`/rooms?${buildQueryString(1)}`);
  };

  const handlePriceSearch = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    router.push(`/rooms?${buildQueryString(1)}`);
  };

  const handleOrderChange = (nextOrder: string) => {
    setOrder(nextOrder);
    router.push(`/rooms?${buildQueryString(1, nextOrder)}`);
  };

  const handleClear = () => {
    router.push("/rooms");
  };

  const movePage = (page: number) => {
    router.push(`/rooms?${buildQueryString(page)}`);
  };

  return (
    <main className="bg-stone-50">
      <div className="mx-auto max-w-7xl px-4 py-6 sm:py-10">
        <nav className="mb-5 text-sm text-slate-500">
          <Link href="/" className="hover:text-sky-700">
            ホーム
          </Link>
          <span className="mx-2">&gt;</span>
          <span className="text-slate-700">スタジオ一覧</span>
        </nav>

<div className="mb-6 rounded-[28px] border border-stone-200 bg-white px-5 py-6 shadow-sm sm:px-8">
  <p className="text-xs font-semibold tracking-[0.24em] text-sky-700">
    STUDIO LIST
  </p>

  <h1 className="mt-2 text-2xl font-semibold text-slate-800 sm:text-3xl">
    スタジオ一覧
  </h1>

  <p className="mt-3 text-sm leading-6 text-slate-500">
    架空のスタジオデータをもとに、キーワード・エリア・予算で検索できます。
  </p>

  <div className="mt-5 flex flex-col gap-3 sm:flex-row sm:items-center">
    <Link
      href="/rooms/ai-search"
      className="inline-flex w-full justify-center rounded-xl bg-sky-700 px-5 py-3 text-sm font-medium text-white transition hover:opacity-90 sm:w-auto"
    >
      AIで希望に合うスタジオを探す
    </Link>

    <span className="text-xs text-slate-400">
      自然文で「夜に使える撮影向け」などの希望を入力できます。
    </span>
  </div>
</div>
        <div className="grid gap-6 lg:grid-cols-[300px_1fr]">
          <aside className="space-y-4">
            <form
              onSubmit={handleKeywordSearch}
              className="rounded-3xl border border-stone-200 bg-white p-5 shadow-sm"
            >
              <h2 className="text-sm font-semibold text-slate-700">
                キーワード検索
              </h2>
              <div className="mt-4 flex overflow-hidden rounded-xl border border-stone-300 bg-white focus-within:border-sky-500">
                <input
                  type="text"
                  value={keyword}
                  onChange={(e) => setKeyword(e.target.value)}
                  placeholder="スタジオ名・所在地"
                  className="w-full px-3 py-3 text-sm outline-none"
                />
                <button
                  type="submit"
                  className="bg-sky-700 px-4 py-3 text-sm font-medium text-white transition hover:opacity-90"
                >
                  検索
                </button>
              </div>
            </form>

            <form
              onSubmit={handleAreaSearch}
              className="rounded-3xl border border-stone-200 bg-white p-5 shadow-sm"
            >
              <h2 className="text-sm font-semibold text-slate-700">
                エリアから探す
              </h2>
              <select
                value={area}
                onChange={(e) => setArea(e.target.value)}
                className="mt-4 w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
              >
                <option value="">選択してください</option>
                {areaGroups.map((group) => (
                  <optgroup key={group.label} label={group.label}>
                    {group.areas.map((areaName) => (
                      <option key={areaName} value={areaName}>
                        {areaName}
                      </option>
                    ))}
                  </optgroup>
                ))}
              </select>

              <button
                type="submit"
                className="mt-4 w-full rounded-xl bg-sky-700 px-4 py-3 text-sm font-medium text-white transition hover:opacity-90"
              >
                検索
              </button>
            </form>

            <form
              onSubmit={handlePriceSearch}
              className="rounded-3xl border border-stone-200 bg-white p-5 shadow-sm"
            >
              <h2 className="text-sm font-semibold text-slate-700">
                1時間あたりの予算から探す
              </h2>
              <select
                value={price}
                onChange={(e) => setPrice(e.target.value)}
                className="mt-4 w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
              >
                <option value="">選択してください</option>
                {priceOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>

              <button
                type="submit"
                className="mt-4 w-full rounded-xl bg-sky-700 px-4 py-3 text-sm font-medium text-white transition hover:opacity-90"
              >
                検索
              </button>
            </form>

            {(currentKeyword || currentArea || currentPrice) && (
              <button
                type="button"
                onClick={handleClear}
                className="w-full rounded-xl border border-stone-300 bg-white px-4 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
              >
                条件をクリア
              </button>
            )}
          </aside>

          <section className="min-w-0">
            {errorMessage && (
              <div className="mb-5 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
                {errorMessage}
              </div>
            )}

            <div className="mb-4 flex flex-col gap-3 rounded-3xl border border-stone-200 bg-white p-4 shadow-sm sm:flex-row sm:items-center sm:justify-between">
              <div>
                {pageInfo.totalPages > 1 ? (
                  <p className="text-base font-medium text-slate-700">
                    検索結果：{pageInfo.totalCount}件（{pageInfo.page} /{" "}
                    {pageInfo.totalPages} ページ）
                  </p>
                ) : (
                  <p className="text-base font-medium text-slate-700">
                    検索結果：{pageInfo.totalCount}件
                  </p>
                )}

                {(currentKeyword || currentArea || currentPrice) && (
                  <p className="mt-1 text-sm text-slate-500">
                    {currentKeyword && <>キーワード：{currentKeyword} </>}
                    {currentArea && <>エリア：{currentArea} </>}
                    {currentPrice && <>予算：{Number(currentPrice).toLocaleString()}円以内</>}
                  </p>
                )}
              </div>

              <select
                value={order}
                onChange={(e) => handleOrderChange(e.target.value)}
                className="w-full rounded-xl border border-stone-300 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-sky-500 sm:w-44"
              >
                <option value="createdAtDesc">新着順</option>
                <option value="priceAsc">料金が安い順</option>
              </select>
            </div>

            {isLoading ? (
              <div className="rounded-3xl border border-stone-200 bg-white px-4 py-10 text-center shadow-sm">
                <p className="text-sm text-slate-500">
                  スタジオ一覧を読み込み中...
                </p>
              </div>
            ) : items.length === 0 ? (
              <div className="rounded-3xl border border-dashed border-stone-300 bg-white px-4 py-12 text-center shadow-sm">
                <p className="text-sm text-slate-500">
                  条件に一致するスタジオはありません。
                </p>
              </div>
            ) : (
              <>
                <div className="space-y-4">
                  {items.map((room) => (
                    <RoomCard key={room.id} room={room} />
                  ))}
                </div>

                {pageInfo.totalPages > 1 && (
                  <div className="mt-8 flex flex-wrap items-center justify-center gap-2">
                    <button
                      type="button"
                      disabled={pageInfo.page <= 1}
                      onClick={() => movePage(pageInfo.page - 1)}
                      className="rounded-lg border border-stone-300 bg-white px-4 py-2 text-sm text-slate-700 disabled:cursor-not-allowed disabled:opacity-40"
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
                              : "rounded-lg border border-stone-300 bg-white px-4 py-2 text-sm text-slate-700 hover:border-sky-300 hover:text-sky-700"
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
                      className="rounded-lg border border-stone-300 bg-white px-4 py-2 text-sm text-slate-700 disabled:cursor-not-allowed disabled:opacity-40"
                    >
                      次
                    </button>
                  </div>
                )}
              </>
            )}
          </section>
        </div>
      </div>
    </main>
  );
}

function RoomCard({ room }: { room: RoomListItem }) {
  return (
    <Link
      href={`/rooms/${room.id}`}
      className="block overflow-hidden rounded-3xl border border-stone-200 bg-white shadow-sm transition hover:opacity-95"
    >
      <div className="grid md:grid-cols-[260px_1fr]">
        <img
          src={getRoomImageSrc(room.imageName)}
          alt={room.name}
          className="h-56 w-full object-cover md:h-full"
        />

        <div className="p-5">
          <h2 className="text-xl font-semibold text-slate-800">
            {room.name}
          </h2>

          <hr className="my-4 border-stone-200" />

          <p className="line-clamp-3 text-sm leading-6 text-slate-600">
            {room.description || "説明はありません。"}
          </p>

          <p className="mt-4 text-sm text-slate-500">
            <span>〒{room.postalCode}</span>
            <span className="ml-2">{room.address}</span>
          </p>

          <p className="mt-3 text-base font-semibold text-slate-800">
            {formatYenPerHour(room.price)}
          </p>

          <div className="mt-3">
            <Stars score={room.averageScore} />
            {room.averageScore != null && (
              <span className="ml-1 text-xs text-slate-400">
                （{room.reviewCount}件）
              </span>
            )}
          </div>
        </div>
      </div>
    </Link>
  );
}