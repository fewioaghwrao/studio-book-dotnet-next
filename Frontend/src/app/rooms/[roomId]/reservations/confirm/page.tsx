"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useParams, useRouter, useSearchParams } from "next/navigation";

type CurrentUserResponse = {
  isAuthenticated: boolean;
  user?: {
    id: number;
    name: string;
    roles: string[];
  };
};

type ReservationConfirmItem = {
  label: string;
  amount: number;
  sliceStart: string | null;
  sliceEnd: string | null;
  unitRatePerHour: number | null;
};

type ReservationConfirmResponse = {
  roomId: number;
  roomName: string;
  startAt: string;
  endAt: string;
  hourlyPrice: number;
  hours: number;
  subtotal: number;

  taxRatePercent: number;
  tax: number | null;

  platformFeeRatePercent: number;
  platformFee: number | null;

  amount: number;
  items: ReservationConfirmItem[];
  stripePublishableKey: string;
  sessionId: string;
  checkoutUrl: string;
};

function toDateTimeIso(date: string | null, time: string | null) {
  if (!date || !time) return null;
  return `${date}T${time}:00`;
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

function formatShortDateTime(value: string | null) {
  if (!value) return "";

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "";
  }

  return new Intl.DateTimeFormat("ja-JP", {
    month: "numeric",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}

function formatYen(value: number | null | undefined) {
  if (value == null) return "- 円";
  return `${new Intl.NumberFormat("ja-JP").format(value)} 円`;
}

function formatPercent(value: number | null | undefined) {
  if (value == null) return "-%";

  return `${new Intl.NumberFormat("ja-JP", {
    maximumFractionDigits: 2,
  }).format(value)}%`;
}

function formatHours(value: number) {
  if (Number.isInteger(value)) {
    return `${value}`;
  }

  return value.toFixed(1).replace(/\.0$/, "");
}

export default function ReservationConfirmPage() {
  const params = useParams<{ roomId: string }>();
  const searchParams = useSearchParams();
  const router = useRouter();

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [confirm, setConfirm] = useState<ReservationConfirmResponse | null>(
    null
  );
  const [isLoading, setIsLoading] = useState(true);
  const [isPaying, setIsPaying] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  const startDate = searchParams.get("startDate");
  const startTime = searchParams.get("startTime");
  const endDate = searchParams.get("endDate");
  const endTime = searchParams.get("endTime");

  useEffect(() => {
    let ignore = false;

    const fetchConfirm = async () => {
      setIsLoading(true);
      setErrorMessage("");

      const startAt = toDateTimeIso(startDate, startTime);
      const endAt = toDateTimeIso(endDate, endTime);

      if (!params.roomId || !startAt || !endAt) {
        setErrorMessage("予約日時が正しく指定されていません。");
        setIsLoading(false);
        return;
      }

      try {
        const meResponse = await fetch(`${apiBaseUrl}/api/auth/me`, {
          method: "GET",
          credentials: "include",
          cache: "no-store",
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
        const roles = me.user?.roles ?? [];

        if (!me.isAuthenticated) {
          window.location.href = "/auth/login";
          return;
        }

        if (!roles.includes("GeneralUser")) {
          setErrorMessage("予約は一般会員のみ可能です。");
          return;
        }

        const response = await fetch(`${apiBaseUrl}/api/reservations/confirm`, {
          method: "POST",
          credentials: "include",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            roomId: Number(params.roomId),
            startAt,
            endAt,
          }),
        });

        if (response.status === 400) {
          const data = await response.json().catch(() => null);
          setErrorMessage(
            data?.message ?? "予約内容を確認できませんでした。"
          );
          return;
        }

        if (response.status === 401) {
          window.location.href = "/auth/login";
          return;
        }

        if (response.status === 403) {
          setErrorMessage("予約は一般会員のみ可能です。");
          return;
        }

        if (response.status === 404) {
          setErrorMessage("スタジオが見つかりません。");
          return;
        }

        if (!response.ok) {
          setErrorMessage("予約確認情報の取得に失敗しました。");
          return;
        }

        const data = (await response.json()) as ReservationConfirmResponse;

        if (!ignore) {
          setConfirm(data);
        }
      } catch {
        if (!ignore) {
          setErrorMessage(
            "通信エラーが発生しました。時間をおいて再度お試しください。"
          );
        }
      } finally {
        if (!ignore) {
          setIsLoading(false);
        }
      }
    };

    fetchConfirm();

    return () => {
      ignore = true;
    };
  }, [
    apiBaseUrl,
    params.roomId,
    startDate,
    startTime,
    endDate,
    endTime,
  ]);

const handlePayment = () => {
  if (!confirm) return;

  if (!confirm.checkoutUrl) {
    setErrorMessage("決済画面のURL取得に失敗しました。");
    return;
  }

  setIsPaying(true);
  setErrorMessage("");

  window.location.href = confirm.checkoutUrl;
};

  if (isLoading) {
    return (
      <main className="min-h-screen bg-stone-50">
        <div className="mx-auto max-w-5xl px-4 py-12">
          <div className="rounded-2xl border border-stone-200 bg-white px-5 py-6 shadow-sm">
            <p className="text-sm text-slate-500">予約内容を確認中...</p>
          </div>
        </div>
      </main>
    );
  }

  if (errorMessage && !confirm) {
    return (
      <main className="min-h-screen bg-stone-50">
        <div className="mx-auto max-w-5xl px-4 py-12">
          <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
            {errorMessage}
          </div>

          <div className="mt-6 flex gap-3">
            <Link
              href={`/rooms/${params.roomId}`}
              className="inline-flex rounded-xl border border-stone-300 px-5 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
            >
              スタジオ詳細へ戻る
            </Link>

            <button
              type="button"
              onClick={() => router.back()}
              className="inline-flex rounded-xl bg-sky-700 px-5 py-3 text-sm font-medium text-white transition hover:opacity-90"
            >
              前の画面へ戻る
            </button>
          </div>
        </div>
      </main>
    );
  }

  if (!confirm) {
    return null;
  }

  return (
    <main className="min-h-screen bg-stone-50">
      <div className="mx-auto max-w-5xl px-4 py-6 sm:py-10">
        <nav className="mb-5 text-sm text-slate-500">
          <Link href="/" className="hover:text-sky-700">
            ホーム
          </Link>
          <span className="mx-2">&gt;</span>
          <Link href="/rooms" className="hover:text-sky-700">
            スタジオ一覧
          </Link>
          <span className="mx-2">&gt;</span>
          <Link
            href={`/rooms/${confirm.roomId}`}
            className="hover:text-sky-700"
          >
            スタジオ詳細
          </Link>
          <span className="mx-2">&gt;</span>
          <span className="text-slate-700">予約内容確認</span>
        </nav>

        <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
          <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 text-center sm:px-8 sm:py-9">
            <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
              RESERVATION CONFIRM
            </p>

            <h1 className="mt-3 text-2xl font-semibold text-slate-800 md:text-3xl">
              予約内容確認
            </h1>

            <p className="mt-3 text-sm leading-6 text-slate-500">
              内容をご確認のうえ、決済へお進みください。
            </p>
          </div>

          <div className="grid gap-6 p-5 md:p-8">
            {errorMessage && (
              <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
                {errorMessage}
              </div>
            )}

            <div className="rounded-2xl border border-stone-200 bg-white">
              <ConfirmRow label="スタジオ" value={confirm.roomName} />

              <ConfirmRow
                label="利用開始時刻"
                value={formatDateTime(confirm.startAt)}
              />

              <ConfirmRow
                label="利用完了時刻"
                value={formatDateTime(confirm.endAt)}
              />

              <ConfirmRow
                label="料金"
                value={`${formatYen(confirm.hourlyPrice)} × ${formatHours(
                  confirm.hours
                )} 時間 ＝ ${formatYen(confirm.amount)}`}
                emphasize
              />
            </div>

            <div className="overflow-hidden rounded-2xl border border-stone-200 bg-white">
              <div className="border-b border-stone-100 px-4 py-3">
                <h2 className="text-base font-semibold text-slate-800">
                  料金内訳
                </h2>
              </div>

              <div className="overflow-x-auto">
                <table className="w-full min-w-[560px] text-sm">
                  <thead className="bg-stone-50 text-slate-600">
                    <tr>
                      <th className="w-[60%] px-4 py-3 text-left font-medium">
                        内訳
                      </th>
                      <th className="px-4 py-3 text-right font-medium">
                        金額
                      </th>
                    </tr>
                  </thead>

                  <tbody className="divide-y divide-stone-100">
                    {confirm.items.length === 0 ? (
                      <tr>
                        <td
                          colSpan={2}
                          className="px-4 py-6 text-center text-slate-500"
                        >
                          料金内訳がありません。
                        </td>
                      </tr>
                    ) : (
                      confirm.items.map((item, index) => (
                        <tr key={`${item.label}-${index}`}>
                          <td className="px-4 py-3 text-slate-700">
                            <div className="font-medium">{item.label}</div>

                            {item.unitRatePerHour != null && (
                              <div className="mt-1 text-xs text-slate-400">
                                単価: {formatYen(item.unitRatePerHour)} / 時間
                              </div>
                            )}

                            {item.sliceStart && item.sliceEnd && (
                              <div className="mt-1 text-xs text-slate-400">
                                {formatShortDateTime(item.sliceStart)} 〜{" "}
                                {formatShortDateTime(item.sliceEnd)}
                              </div>
                            )}
                          </td>

                          <td className="px-4 py-3 text-right text-slate-700">
                            {formatYen(item.amount)}
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>

<tfoot className="border-t border-stone-200 bg-stone-50">
  <tr>
    <th className="px-4 py-3 text-right font-medium text-slate-700">
      小計
    </th>
    <th className="px-4 py-3 text-right font-semibold text-slate-800">
      {formatYen(confirm.subtotal)}
    </th>
  </tr>

  {confirm.tax != null && (
    <tr>
      <th className="px-4 py-3 text-right font-medium text-slate-700">
        消費税（{formatPercent(confirm.taxRatePercent)}）
      </th>
      <th className="px-4 py-3 text-right font-semibold text-slate-800">
        {formatYen(confirm.tax)}
      </th>
    </tr>
  )}

  {confirm.platformFee != null && (
    <tr>
      <th className="px-4 py-3 text-right font-medium text-slate-700">
        プラットフォーム使用料（
        {formatPercent(confirm.platformFeeRatePercent)}）
      </th>
      <th className="px-4 py-3 text-right font-semibold text-slate-800">
        {formatYen(confirm.platformFee)}
      </th>
    </tr>
  )}

  <tr>
    <th className="px-4 py-4 text-right text-base font-semibold text-slate-800">
      合計
    </th>
    <th className="px-4 py-4 text-right text-lg font-bold text-sky-700">
      {formatYen(confirm.amount)}
    </th>
  </tr>
</tfoot>
                </table>
              </div>
            </div>

            <div className="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm leading-6 text-amber-800">
              決済完了後、予約一覧に反映されます。ブラウザの戻るボタンや二重クリックによる重複操作にご注意ください。
            </div>

            <div className="flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-center">
              <Link
                href={`/rooms/${confirm.roomId}`}
                className="inline-flex justify-center rounded-xl border border-stone-300 px-6 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
              >
                戻る
              </Link>

              <button
                type="button"
                onClick={handlePayment}
                disabled={isPaying}
                className="inline-flex justify-center rounded-xl bg-sky-700 px-10 py-3 text-sm font-medium text-white shadow-sm transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isPaying ? "決済画面へ移動中..." : "決済する"}
              </button>
            </div>
          </div>
        </section>
      </div>
    </main>
  );
}

function ConfirmRow({
  label,
  value,
  emphasize = false,
}: {
  label: string;
  value: string;
  emphasize?: boolean;
}) {
  return (
    <div className="grid gap-2 border-b border-stone-100 px-4 py-4 last:border-b-0 sm:grid-cols-[180px_1fr]">
      <div className="text-sm font-semibold text-slate-700">{label}</div>

      <div
        className={
          emphasize
            ? "text-sm font-semibold text-slate-900"
            : "text-sm text-slate-700"
        }
      >
        {value}
      </div>
    </div>
  );
}