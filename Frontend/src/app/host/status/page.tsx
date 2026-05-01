"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

type RoomOption = {
  id: number;
  name: string;
};

type HostStatusResponse = {
  labels: string[];
  booked: number[];
  paid: number[];
  utilizationPercents: Array<number | null>;
  reviewAvgAny: number | null;
  reviewAvgPublic: number | null;
  roomOptions: RoomOption[];
};

type ChartRow = {
  label: string;
  booked: number;
  paid: number;
};

function formatYen(value: number) {
  return new Intl.NumberFormat("ja-JP", {
    style: "currency",
    currency: "JPY",
    maximumFractionDigits: 0,
  }).format(value);
}

function formatAverage(value: number | null) {
  if (value === null || value === undefined) {
    return "—";
  }

  return (Math.round(value * 10) / 10).toFixed(1);
}

function formatPercent(value: number | null) {
  if (value === null || value === undefined) {
    return "—";
  }

  return `${(Math.round(value * 10) / 10).toFixed(1)}%`;
}

function formatBaseMonth(year: number, month: number) {
  return `${year}年${month}月`;
}

function moveMonth(year: number, month: number, diff: number) {
  const date = new Date(year, month - 1 + diff, 1);

  return {
    year: date.getFullYear(),
    month: date.getMonth() + 1,
  };
}

export default function HostStatusPage() {
  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [selectedRoomId, setSelectedRoomId] = useState("0");
  const today = new Date();

const [baseYear, setBaseYear] = useState(today.getFullYear());
const [baseMonth, setBaseMonth] = useState(today.getMonth() + 1);
  const [data, setData] = useState<HostStatusResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

const fetchStatus = async (
  roomId: string,
  year: number,
  month: number
) => {
  setIsLoading(true);
  setErrorMessage("");

  try {
    const params = new URLSearchParams();

    if (roomId && roomId !== "0") {
      params.set("roomId", roomId);
    }

    params.set("year", String(year));
    params.set("month", String(month));

    const response = await fetch(
      `${apiBaseUrl}/api/host/status?${params.toString()}`,
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
      setErrorMessage("ホストユーザーのみアクセスできます。");
      return;
    }

    if (!response.ok) {
      setErrorMessage("統計情報の取得に失敗しました。");
      return;
    }

    const json = (await response.json()) as HostStatusResponse;
    setData(json);
  } catch {
    setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
  } finally {
    setIsLoading(false);
  }
};

useEffect(() => {
  fetchStatus(selectedRoomId, baseYear, baseMonth);
  // eslint-disable-next-line react-hooks/exhaustive-deps
}, [apiBaseUrl]);

const handleRoomChange = (roomId: string) => {
  setSelectedRoomId(roomId);
  fetchStatus(roomId, baseYear, baseMonth);
};
const handleMoveMonth = (diff: number) => {
  const next = moveMonth(baseYear, baseMonth, diff);

  setBaseYear(next.year);
  setBaseMonth(next.month);

  fetchStatus(selectedRoomId, next.year, next.month);
};

  const chartRows: ChartRow[] =
    data?.labels.map((label, index) => ({
      label,
      booked: data.booked[index] ?? 0,
      paid: data.paid[index] ?? 0,
    })) ?? [];

  const utilizationRows =
    data?.labels.map((label, index) => ({
      label,
      value: data.utilizationPercents[index] ?? null,
    })) ?? [];

  return (
    <div className="mx-auto max-w-7xl px-4 py-6 sm:py-10">
      <nav className="mb-5 text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">統計一覧</span>
      </nav>

      <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
        <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 text-center sm:px-8 sm:py-9">
          <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
            HOST STATUS
          </p>
          <h1 className="mt-3 text-xl font-semibold text-slate-800 sm:text-2xl md:text-3xl">
            統計一覧
          </h1>
          <p className="mt-3 text-sm leading-6 text-slate-500">
            表示基準月を含む3か月分の売上、稼働率、レビュー平均を確認できます。
          </p>
        </div>

        <div className="px-5 py-5 sm:px-8 sm:py-7">
          {errorMessage && (
            <div className="mb-5 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
              {errorMessage}
            </div>
          )}

<div className="mb-7 rounded-3xl border border-stone-200 bg-stone-50 p-4 sm:p-5">
  <div className="grid gap-4 lg:grid-cols-[1fr_auto] lg:items-end">
    <div>
      <label className="mb-1 block text-xs font-semibold text-slate-500">
        対象スタジオ
      </label>
      <select
        value={selectedRoomId}
        onChange={(e) => handleRoomChange(e.target.value)}
        className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
      >
        <option value="0">全体（すべてのスタジオ）</option>
        {data?.roomOptions.map((room) => (
          <option key={room.id} value={room.id}>
            {room.name}
          </option>
        ))}
      </select>
    </div>

    <div>
      <label className="mb-1 block text-xs font-semibold text-slate-500">
        表示基準月
      </label>

      <div className="flex items-center gap-2">
        <button
          type="button"
          onClick={() => handleMoveMonth(-1)}
          className="rounded-xl border border-stone-300 bg-white px-4 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
        >
          前月
        </button>

        <div className="min-w-[140px] rounded-xl border border-stone-300 bg-white px-4 py-3 text-center text-sm font-semibold text-slate-800">
          {formatBaseMonth(baseYear, baseMonth)}
        </div>

        <button
          type="button"
          onClick={() => handleMoveMonth(1)}
          className="rounded-xl border border-stone-300 bg-white px-4 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
        >
          次月
        </button>
      </div>
    </div>
  </div>
</div>

          {isLoading ? (
            <p className="text-sm text-slate-500">統計情報を読み込み中...</p>
          ) : !data ? (
            <div className="rounded-2xl border border-dashed border-stone-300 bg-stone-50 px-4 py-10 text-center">
              <p className="text-sm text-slate-500">統計情報がありません。</p>
            </div>
          ) : (
            <div className="space-y-8">
              <section>
                <div className="mb-4">
                  <h2 className="text-lg font-semibold text-slate-800">
                    売上一覧（表示基準月を含む3か月）
                  </h2>
                  <p className="mt-1 text-sm text-slate-500">
                    予約済みを見込み売上、利用済みを確定売上として表示します。
                  </p>
                </div>

                <div className="h-[320px] rounded-3xl border border-stone-200 bg-white p-4 shadow-sm">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={chartRows}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="label" />
                      <YAxis tickFormatter={(value) => `${Number(value) / 10000}万`} />
                      <Tooltip
                        formatter={(value) => formatYen(Number(value))}
                      />
                      <Legend />
                      <Bar dataKey="booked" name="見込み売上" />
                      <Line
                        type="monotone"
                        dataKey="paid"
                        name="確定売上"
                        strokeWidth={2}
                      />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              </section>

              <section>
                <div className="mb-4 text-center">
                  <h2 className="text-lg font-semibold text-slate-800">
                    稼働率（直近3か月）
                  </h2>
                  <p className="mt-1 text-sm text-slate-500">
                    営業時間に対する予約時間の割合です。
                  </p>
                </div>

                <div className="grid gap-4 md:grid-cols-3">
                  {utilizationRows.map((row) => (
                    <div
                      key={row.label}
                      className="rounded-3xl border border-stone-200 bg-white p-5 shadow-sm"
                    >
                      <p className="text-sm text-slate-500">{row.label}</p>
                      <p className="mt-2 text-3xl font-semibold text-slate-800">
                        {formatPercent(row.value)}
                      </p>
                      <p className="mt-2 text-sm text-slate-500">
                        営業時間ベース
                      </p>
                    </div>
                  ))}
                </div>
              </section>

              <section>
                <div className="mb-4 text-center">
                  <h2 className="text-lg font-semibold text-slate-800">
                    レビュー
                  </h2>
                  <p className="mt-1 text-sm text-slate-500">
                    投稿済みレビューの平均スコアです。
                  </p>
                </div>

                <div className="grid gap-4 md:grid-cols-2">
                  <div className="rounded-3xl border border-stone-200 bg-white p-5 shadow-sm">
                    <p className="text-sm font-medium text-slate-600">
                      平均レビュー（公開・非公開すべて）
                    </p>
                    <div className="mt-3 flex items-end gap-2">
                      <p className="text-4xl font-semibold text-slate-800">
                        {formatAverage(data.reviewAvgAny)}
                      </p>
                      <p className="pb-1 text-sm text-slate-500">/ 5</p>
                    </div>
                  </div>

                  <div className="rounded-3xl border border-stone-200 bg-white p-5 shadow-sm">
                    <p className="text-sm font-medium text-slate-600">
                      平均レビュー（公開のみ）
                    </p>
                    <div className="mt-3 flex items-end gap-2">
                      <p className="text-4xl font-semibold text-slate-800">
                        {formatAverage(data.reviewAvgPublic)}
                      </p>
                      <p className="pb-1 text-sm text-slate-500">/ 5</p>
                    </div>
                  </div>
                </div>
              </section>
            </div>
          )}
        </div>
      </section>
    </div>
  );
}