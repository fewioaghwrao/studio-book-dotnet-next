"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/apiFetch";

type SalesItem = {
  kind: string;
  description: string;
  sliceStart: string | null;
  sliceEnd: string | null;
  unitRatePerHour: number | null;
  sliceAmount: number;
};

type SalesDetail = {
  reservationId: number;
  roomId: number;
  roomName: string;
  guestName: string;
  startAt: string;
  endAt: string;
  amount: number;
  status: string;
  items: SalesItem[];
};

function formatDateTime(value: string | null) {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat("ja-JP", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}

function formatPeriod(start: string | null, end: string | null) {
  if (!start || !end) return "-";
  return `${formatDateTime(start)} 〜 ${formatDateTime(end)}`;
}

function formatStatus(status: string) {
  if (status === "booked") return "予約済み";
  if (status === "paid") return "利用済み";
  if (status === "canceled") return "キャンセル済み";
  return status;
}

function formatKind(kind: string) {
  if (kind === "base") return "基本料金";
  if (kind === "multiplier") return "加算料金";
  if (kind === "tax") return "消費税";
  if (kind === "platform_fee") return "手数料";
  if (kind === "基本料金") return "基本料金";
  if (kind === "加算料金") return "加算料金";
  return kind;
}

function kindBadgeClassName(kind: string) {
  if (kind === "tax") return "bg-amber-50 text-amber-700 ring-amber-200";
  if (kind === "platform_fee") return "bg-violet-50 text-violet-700 ring-violet-200";
  if (kind === "multiplier" || kind === "加算料金") return "bg-sky-50 text-sky-700 ring-sky-200";
  return "bg-stone-100 text-slate-700 ring-stone-200";
}

export default function HostSalesDetailPage() {
  const params = useParams<{ id: string }>();

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [detail, setDetail] = useState<SalesDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    let ignore = false;

    const fetchDetail = async () => {
      setIsLoading(true);
      setErrorMessage("");

      try {
        const response = await apiFetch(
          `${apiBaseUrl}/api/host/sales/${params.id}`,
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
        if (response.status === 404) {
          setErrorMessage("売上明細が見つかりません。");
          return;
        }
        if (!response.ok) {
          setErrorMessage("売上明細の取得に失敗しました。");
          return;
        }

        const data = (await response.json()) as SalesDetail;
        if (!ignore) setDetail(data);
      } catch {
        if (!ignore) {
          setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
        }
      } finally {
        if (!ignore) setIsLoading(false);
      }
    };

    if (params.id) fetchDetail();

    return () => { ignore = true; };
  }, [apiBaseUrl, params.id]);

  const handleCsvDownload = async () => {
    try {
      const response = await apiFetch(
        `${apiBaseUrl}/api/host/sales/${params.id}/items.csv`,
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
      a.download = `sales-${params.id}.csv`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    }
  };

  const handlePdfDownload = async () => {
    try {
      const response = await apiFetch(
        `${apiBaseUrl}/api/host/sales/${params.id}/invoice.pdf`,
        { method: "GET" }
      );
      if (response.status === 401) {
        window.location.href = "/auth/login";
        return;
      }
      if (!response.ok) {
        setErrorMessage("PDFのダウンロードに失敗しました。");
        return;
      }
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `invoice-${params.id}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    }
  };

  if (isLoading) {
    return (
      <div className="mx-auto max-w-5xl px-4 py-12">
        <p className="text-sm text-slate-500">売上明細を読み込み中...</p>
      </div>
    );
  }

  if (errorMessage) {
    return (
      <div className="mx-auto max-w-5xl px-4 py-12">
        <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
          {errorMessage}
        </div>
        <div className="mt-6">
          <Link href="/host/sales" className="text-sm font-medium text-sky-700 hover:text-sky-800">
            売上明細一覧へ戻る
          </Link>
        </div>
      </div>
    );
  }

  if (!detail) return null;

  return (
    <div className="mx-auto max-w-5xl px-4 py-6 sm:py-10">
      <nav className="mb-5 text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">ホーム</Link>
        <span className="mx-2">&gt;</span>
        <Link href="/host/sales" className="hover:text-sky-700">売上明細一覧</Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">売上明細</span>
      </nav>

      <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
        <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 text-center sm:px-8 sm:py-9">
          <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
            SALES DETAIL
          </p>
          <h1 className="mt-3 text-xl font-semibold text-slate-800 sm:text-2xl md:text-3xl">
            売上明細
          </h1>
          <p className="mt-3 text-sm leading-6 text-slate-500">
            予約単位の料金・税・手数料の内訳を確認できます。
          </p>
        </div>

        <div className="px-5 py-5 sm:px-8 sm:py-7">
          <div className="mb-8 divide-y divide-stone-200 rounded-2xl border border-stone-200 bg-white">
            {[
              { label: "予約ID", value: String(detail.reservationId) },
              { label: "スタジオ", value: detail.roomName },
              { label: "予約者", value: detail.guestName },
              {
                label: "期間",
                value: `${formatDateTime(detail.startAt)} 〜 ${formatDateTime(detail.endAt)}`,
              },
              { label: "合計", value: `${detail.amount.toLocaleString()}円` },
              { label: "状態", value: formatStatus(detail.status) },
            ].map((row) => (
              <div
                key={row.label}
                className="grid gap-2 px-4 py-4 md:grid-cols-[160px_1fr] md:px-5"
              >
                <div className="text-sm font-semibold text-slate-700">{row.label}</div>
                <div className="whitespace-pre-wrap text-sm leading-6 text-slate-700">{row.value}</div>
              </div>
            ))}
          </div>

          <div className="mb-4 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <h2 className="text-lg font-semibold text-slate-800">料金明細</h2>
              <p className="mt-1 text-sm text-slate-500">
                基本料金、加算料金、消費税、プラットフォーム手数料の内訳です。
              </p>
            </div>

            <div className="flex flex-col gap-2 sm:flex-row">
              <button
                type="button"
                onClick={handleCsvDownload}
                className="rounded-xl border border-sky-200 bg-white px-4 py-3 text-center text-sm font-medium text-sky-700 transition hover:bg-sky-50"
              >
                明細CSV
              </button>
              <button
                type="button"
                onClick={handlePdfDownload}
                className="rounded-xl border border-rose-200 bg-white px-4 py-3 text-center text-sm font-medium text-rose-700 transition hover:bg-rose-50"
              >
                明細請求書PDF
              </button>
            </div>
          </div>

          {detail.items.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-stone-300 bg-stone-50 px-4 py-10 text-center">
              <p className="text-sm text-slate-500">明細はありません。</p>
            </div>
          ) : (
            <>
              <div className="grid gap-4 md:hidden">
                {detail.items.map((item, index) => (
                  <article
                    key={`${item.kind}-${item.sliceStart ?? "none"}-${item.sliceEnd ?? "none"}-${index}`}
                    className="rounded-2xl border border-stone-200 bg-white p-4 shadow-sm"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-xs font-medium tracking-[0.18em] text-sky-700">
                          ITEM #{index + 1}
                        </p>
                        <h3 className="mt-1 text-base font-semibold text-slate-800">
                          {item.description || formatKind(item.kind)}
                        </h3>
                      </div>
                      <span className={`rounded-full px-3 py-1 text-xs font-medium ring-1 ${kindBadgeClassName(item.kind)}`}>
                        {formatKind(item.kind)}
                      </span>
                    </div>

                    <dl className="mt-4 grid gap-3 text-sm">
                      <div>
                        <dt className="text-xs font-medium text-slate-400">適用期間</dt>
                        <dd className="mt-1 text-slate-700">{formatPeriod(item.sliceStart, item.sliceEnd)}</dd>
                      </div>
                      <div>
                        <dt className="text-xs font-medium text-slate-400">単価</dt>
                        <dd className="mt-1 text-slate-700">
                          {item.unitRatePerHour != null ? `${item.unitRatePerHour.toLocaleString()}円/時` : "-"}
                        </dd>
                      </div>
                      <div>
                        <dt className="text-xs font-medium text-slate-400">金額</dt>
                        <dd className="mt-1 font-semibold text-slate-800">
                          {item.sliceAmount.toLocaleString()}円
                        </dd>
                      </div>
                    </dl>
                  </article>
                ))}
              </div>

              <div className="hidden overflow-x-auto md:block">
                <table className="w-full min-w-[760px] border-separate border-spacing-y-3">
                  <thead>
                    <tr className="text-left text-xs font-semibold tracking-wide text-slate-500">
                      <th className="px-4 py-2">種別</th>
                      <th className="px-4 py-2">説明</th>
                      <th className="px-4 py-2">適用期間</th>
                      <th className="px-4 py-2">単価</th>
                      <th className="px-4 py-2 text-right">金額</th>
                    </tr>
                  </thead>
                  <tbody>
                    {detail.items.map((item, index) => (
                      <tr
                        key={`${item.kind}-${item.sliceStart ?? "none"}-${item.sliceEnd ?? "none"}-${index}`}
                        className="bg-stone-50"
                      >
                        <td className="rounded-l-2xl px-4 py-4 text-sm text-slate-700">
                          <span className={`rounded-full px-3 py-1 text-xs font-medium ring-1 ${kindBadgeClassName(item.kind)}`}>
                            {formatKind(item.kind)}
                          </span>
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">{item.description || "-"}</td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {formatPeriod(item.sliceStart, item.sliceEnd)}
                        </td>
                        <td className="px-4 py-4 text-sm text-slate-700">
                          {item.unitRatePerHour != null ? `${item.unitRatePerHour.toLocaleString()}円/時` : "-"}
                        </td>
                        <td className="rounded-r-2xl px-4 py-4 text-right text-sm font-semibold text-slate-800">
                          {item.sliceAmount.toLocaleString()}円
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="mt-6 rounded-2xl border border-stone-200 bg-stone-50 px-5 py-4 text-right">
                <p className="text-sm text-slate-500">合計</p>
                <p className="mt-1 text-2xl font-semibold text-slate-800">
                  {detail.amount.toLocaleString()}円
                </p>
              </div>
            </>
          )}

          <div className="mt-8">
            <Link
              href="/host/sales"
              className="inline-flex rounded-xl border border-stone-300 px-5 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
            >
              戻る
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}