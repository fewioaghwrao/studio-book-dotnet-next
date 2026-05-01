"use client";

import Link from "next/link";
import { FormEvent, useMemo, useState } from "react";

type CapacityCondition = "min" | "max" | "exact" | null;

type AiRoomSearchConditions = {
  keyword: string | null;
  area: string | null;
  price: number | null;
  capacity: number | null;
  capacityCondition: CapacityCondition;
  purpose: string | null;
  atmosphere: string | null;
  timePreference: string | null;
  keywords: string[];
};

type AiRoomSearchResult = {
  id: number;
  name: string;
  imageName: string | null;
  description: string;
  postalCode: string;
  address: string;
  price: number;
  capacity: number;
  averageScore: number | null;
  reviewCount: number;
  reason: string;
};

type AiRoomSearchResponse = {
  query: string;
  interpretedConditions: AiRoomSearchConditions;
  rooms: AiRoomSearchResult[];
};

const examples = [
  "落ち着いた雰囲気で、夜に使える撮影向けのスタジオを探したい",
  "10人くらいで使える、駅近っぽい配信向けスタジオ",
  "10人以下の小さめなスタジオを探したい",
  "ヨガや少人数レッスンに向いている明るい部屋",
  "新宿区周辺で、10000円以内の撮影向けスタジオ",
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

function formatCapacityCondition(
  capacity: number | null,
  condition: CapacityCondition
) {
  if (capacity == null || capacity <= 0) {
    return "指定なし";
  }

  switch (condition) {
    case "max":
      return `${capacity}人以下`;
    case "exact":
      return `${capacity}人`;
    case "min":
    default:
      return `${capacity}人以上`;
  }
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

function displayValue(value: string | number | null | undefined) {
  if (value == null || value === "") return "指定なし";
  return String(value);
}

export default function AiRoomSearchPage() {
  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [query, setQuery] = useState(
    "落ち着いた雰囲気で、夜に使える撮影向けのスタジオを探したい"
  );
  const [result, setResult] = useState<AiRoomSearchResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  const handleSearch = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    const trimmedQuery = query.trim();

    if (!trimmedQuery) {
      setErrorMessage("検索したい内容を入力してください。");
      return;
    }

    if (trimmedQuery.length > 200) {
      setErrorMessage("検索文は200文字以内で入力してください。");
      return;
    }

    setIsLoading(true);
    setErrorMessage("");
    setResult(null);

    try {
      const response = await fetch(`${apiBaseUrl}/api/ai/room-search`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        cache: "no-store",
        body: JSON.stringify({
          query: trimmedQuery,
        }),
      });

if (response.status === 429) {
  setErrorMessage(
    "AI検索の利用回数が一時的に上限に達しました。少し時間をおいてから再度お試しください。"
  );
  return;
}

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);

        setErrorMessage(
          errorData?.message ??
            "AI検索の実行に失敗しました。時間をおいて再度お試しください。"
        );
        return;
      }

      const data = (await response.json()) as AiRoomSearchResponse;
      setResult(data);
    } catch {
      setErrorMessage(
        "通信エラーが発生しました。バックエンドの起動状態を確認してください。"
      );
    } finally {
      setIsLoading(false);
    }
  };

  const handleExampleClick = (example: string) => {
    setQuery(example);
    setErrorMessage("");
  };

  return (
    <main className="bg-stone-50">
      <div className="mx-auto max-w-7xl px-4 py-6 sm:py-10">
        <nav className="mb-5 text-sm text-slate-500">
          <Link href="/" className="hover:text-sky-700">
            ホーム
          </Link>
          <span className="mx-2">&gt;</span>
          <Link href="/rooms" className="hover:text-sky-700">
            スタジオ一覧
          </Link>
          <span className="mx-2">&gt;</span>
          <span className="text-slate-700">AIスタジオ検索</span>
        </nav>

        <section className="mb-6 overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
          <div className="bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 sm:px-8 sm:py-9">
            <p className="text-xs font-semibold tracking-[0.24em] text-sky-700">
              AI STUDIO SEARCH
            </p>

            <h1 className="mt-2 text-2xl font-semibold text-slate-800 sm:text-3xl">
              AIでスタジオを探す
            </h1>

            <p className="mt-3 max-w-3xl text-sm leading-6 text-slate-500">
              「落ち着いた雰囲気」「夜に使いたい」「撮影向け」など、通常検索では指定しづらい希望を自然文で入力できます。
              AIが検索意図を読み取り、条件に合いそうなスタジオを提案します。
            </p>

<p className="mt-3 max-w-3xl rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-xs leading-5 text-amber-800">
  ※この機能はポートフォリオ用のデモ機能です。AIによる条件解釈・おすすめ理由には不正確な内容が含まれる場合があります。
  実際の利用条件はスタジオ詳細画面でご確認ください。
  また、API連打防止のため、AI検索は短時間に連続して利用できる回数を制限しています。
</p>
          </div>

          <div className="p-5 sm:p-8">
            <form onSubmit={handleSearch} className="space-y-4">
              <div>
                <label
                  htmlFor="ai-query"
                  className="text-sm font-semibold text-slate-700"
                >
                  探したいスタジオの希望条件
                </label>

                <textarea
                  id="ai-query"
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  rows={4}
                  maxLength={200}
                  placeholder="例：落ち着いた雰囲気で、夜に使える撮影向けのスタジオを探したい"
                  className="mt-3 w-full rounded-2xl border border-stone-300 bg-white px-4 py-3 text-sm leading-6 text-slate-700 outline-none transition focus:border-sky-500"
                />

                <div className="mt-2 flex items-center justify-between gap-3">
                  <p className="text-xs text-slate-400">
                    最大200文字まで入力できます。
                  </p>
                  <p className="text-xs text-slate-400">
                    {query.length} / 200
                  </p>
                </div>
              </div>

              <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
                <button
                  type="submit"
                  disabled={isLoading}
                  className="rounded-xl bg-sky-700 px-6 py-3 text-sm font-medium text-white transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {isLoading ? "AI検索中..." : "AIで探す"}
                </button>

                <Link
                  href="/rooms"
                  className="rounded-xl border border-stone-300 bg-white px-6 py-3 text-center text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
                >
                  通常検索へ戻る
                </Link>
              </div>
            </form>

            <div className="mt-6">
              <p className="text-sm font-semibold text-slate-700">入力例</p>

              <div className="mt-3 flex flex-wrap gap-2">
                {examples.map((example) => (
                  <button
                    key={example}
                    type="button"
                    onClick={() => handleExampleClick(example)}
                    className="rounded-full border border-sky-100 bg-sky-50 px-3 py-2 text-xs text-sky-700 transition hover:border-sky-300 hover:bg-sky-100"
                  >
                    {example}
                  </button>
                ))}
              </div>
            </div>
          </div>
        </section>

        {errorMessage && (
          <div className="mb-6 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
            {errorMessage}
          </div>
        )}

        {isLoading && (
          <div className="rounded-3xl border border-stone-200 bg-white px-4 py-12 text-center shadow-sm">
            <p className="text-sm text-slate-500">
              AIが検索条件を解析しています...
            </p>
          </div>
        )}

        {!isLoading && result && (
          <div className="grid gap-6 lg:grid-cols-[320px_1fr]">
            <aside className="space-y-4">
              <section className="rounded-3xl border border-stone-200 bg-white p-5 shadow-sm">
                <h2 className="text-base font-semibold text-slate-800">
                  AIが解釈した条件
                </h2>

                <div className="mt-4 space-y-3 text-sm">
                  <ConditionRow
                    label="用途"
                    value={displayValue(result.interpretedConditions.purpose)}
                  />

                  <ConditionRow
                    label="雰囲気"
                    value={displayValue(result.interpretedConditions.atmosphere)}
                  />

                  <ConditionRow
                    label="エリア"
                    value={displayValue(result.interpretedConditions.area)}
                  />

                  <ConditionRow
                    label="予算"
                    value={
                      result.interpretedConditions.price
                        ? `${result.interpretedConditions.price.toLocaleString()}円以内`
                        : "指定なし"
                    }
                  />

                  <ConditionRow
                    label="人数"
                    value={formatCapacityCondition(
                      result.interpretedConditions.capacity,
                      result.interpretedConditions.capacityCondition
                    )}
                  />

                  <ConditionRow
                    label="時間帯"
                    value={displayValue(
                      result.interpretedConditions.timePreference
                    )}
                  />
                </div>

                {result.interpretedConditions.keywords.length > 0 && (
                  <div className="mt-5">
                    <p className="text-sm font-semibold text-slate-700">
                      キーワード
                    </p>

                    <div className="mt-3 flex flex-wrap gap-2">
                      {result.interpretedConditions.keywords.map((keyword) => (
                        <span
                          key={keyword}
                          className="rounded-full bg-stone-100 px-3 py-1 text-xs text-slate-600"
                        >
                          {keyword}
                        </span>
                      ))}
                    </div>
                  </div>
                )}
              </section>

              <section className="rounded-3xl border border-sky-100 bg-sky-50 p-5">
                <h2 className="text-base font-semibold text-slate-800">
                  検索文
                </h2>
                <p className="mt-3 whitespace-pre-wrap text-sm leading-6 text-slate-600">
                  {result.query}
                </p>
              </section>
            </aside>

            <section className="min-w-0">
              <div className="mb-4 rounded-3xl border border-stone-200 bg-white p-4 shadow-sm">
                <p className="text-base font-medium text-slate-700">
                  AIおすすめ結果：{result.rooms.length}件
                </p>
                <p className="mt-1 text-sm text-slate-500">
                  条件に近いスタジオを最大5件表示しています。
                </p>
              </div>

              {result.rooms.length === 0 ? (
                <div className="rounded-3xl border border-dashed border-stone-300 bg-white px-4 py-12 text-center shadow-sm">
                  <p className="text-sm text-slate-500">
                    条件に近いスタジオは見つかりませんでした。
                  </p>
                  <p className="mt-2 text-xs text-slate-400">
                    条件を少し広げて、もう一度検索してみてください。
                  </p>
                </div>
              ) : (
                <div className="space-y-4">
                  {result.rooms.map((room) => (
                    <AiRoomCard key={room.id} room={room} />
                  ))}
                </div>
              )}
            </section>
          </div>
        )}
      </div>
    </main>
  );
}

function ConditionRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-start justify-between gap-3 border-b border-stone-100 pb-2 last:border-b-0 last:pb-0">
      <span className="text-slate-500">{label}</span>
      <span className="text-right font-medium text-slate-700">{value}</span>
    </div>
  );
}

function AiRoomCard({ room }: { room: AiRoomSearchResult }) {
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
          <div className="mb-3 inline-flex rounded-full bg-sky-50 px-3 py-1 text-xs font-medium text-sky-700">
            AIおすすめ
          </div>

          <h2 className="text-xl font-semibold text-slate-800">
            {room.name}
          </h2>

          <div className="mt-4 rounded-2xl border border-sky-100 bg-sky-50 px-4 py-3">
            <p className="text-xs font-semibold text-sky-700">おすすめ理由</p>
            <p className="mt-2 text-sm leading-6 text-slate-700">
              {room.reason ||
                "入力された希望条件に関連するスタジオとして候補に入りました。"}
            </p>
          </div>

          <hr className="my-4 border-stone-200" />

          <p className="line-clamp-3 text-sm leading-6 text-slate-600">
            {room.description || "説明はありません。"}
          </p>

          <p className="mt-4 text-sm text-slate-500">
            <span>〒{room.postalCode}</span>
            <span className="ml-2">{room.address}</span>
          </p>

          <div className="mt-3 flex flex-wrap items-center gap-x-5 gap-y-2">
            <p className="text-base font-semibold text-slate-800">
              {formatYenPerHour(room.price)}
            </p>
            <p className="text-sm text-slate-500">定員：{room.capacity}人</p>
          </div>

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