"use client";

import FullCalendar from "@fullcalendar/react";
import dayGridPlugin from "@fullcalendar/daygrid";
import timeGridPlugin from "@fullcalendar/timegrid";
import interactionPlugin from "@fullcalendar/interaction";
import jaLocale from "@fullcalendar/core/locales/ja";
import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useParams } from "next/navigation";

type HostRoomDetail = {
  id: number;
  name: string;
  imageName: string;
  description: string;
  price: number;
  capacity: number;
  postalCode: string;
  address: string;
};

type BusinessHourRow = {
  dayOfWeek: number;
  startTime: string | null;
  endTime: string | null;
  isHoliday: boolean;
};

type BusinessHoursResponse = {
  roomId: number;
  roomName: string;
  rows: BusinessHourRow[];
};

type PriceRule = {
  id: number;
  ruleType: string;
  weekday: number | null;
  startHour: string | null;
  endHour: string | null;
  multiplier: number | null;
  flatFee: number | null;
  note: string | null;
};

type PriceRulesResponse = {
  roomId: number;
  roomName: string;
  rules: PriceRule[];
};

type ClosureEvent = {
  id: number;
  title: string;
  start: string;
  end: string;
  allDay: boolean;
};

const businessWeekdayLabels: Record<number, string> = {
  1: "月",
  2: "火",
  3: "水",
  4: "木",
  5: "金",
  6: "土",
  7: "日",
};

const priceWeekdayLabels: Record<number, string> = {
  0: "日",
  1: "月",
  2: "火",
  3: "水",
  4: "木",
  5: "金",
  6: "土",
};

function normalizeTime(value: string | null) {
  if (!value) return null;
  return value.slice(0, 5);
}

function BusinessHoursDisplay({ rows }: { rows: BusinessHourRow[] }) {
  const sortedRows = [...rows].sort((a, b) => a.dayOfWeek - b.dayOfWeek);

  if (sortedRows.length === 0) {
    return <p className="text-sm text-slate-500">未設定</p>;
  }

  const formatTime = (value: string | null) => {
    return normalizeTime(value) ?? "-";
  };

  const getDisplayValue = (row: BusinessHourRow, type: "start" | "end") => {
    if (row.isHoliday) {
      return "休";
    }

    if (type === "start") {
      return formatTime(row.startTime);
    }

    return formatTime(row.endTime);
  };

  return (
    <>
      {/* スマホ：カード/縦並び */}
      <div className="grid gap-2 md:hidden">
        {sortedRows.map((row) => {
          const label = businessWeekdayLabels[row.dayOfWeek] ?? "-";

          return (
            <div
              key={row.dayOfWeek}
              className={
                row.isHoliday
                  ? "rounded-xl border border-stone-200 bg-stone-50 px-3 py-3"
                  : "rounded-xl border border-sky-100 bg-sky-50/40 px-3 py-3"
              }
            >
              <div className="flex items-center justify-between gap-3">
                <div className="text-sm font-semibold text-slate-700">
                  {label}曜
                </div>

                {row.isHoliday ? (
                  <span className="rounded-full bg-stone-200 px-3 py-1 text-xs font-medium text-slate-600">
                    休み
                  </span>
                ) : (
                  <div className="text-sm font-medium text-slate-700">
                    {formatTime(row.startTime)}〜{formatTime(row.endTime)}
                  </div>
                )}
              </div>
            </div>
          );
        })}
      </div>

      {/* PC/タブレット：表 */}
      <div className="hidden overflow-x-auto md:block">
        <table className="w-full min-w-[640px] overflow-hidden rounded-xl border border-stone-200 text-sm">
          <thead>
            <tr className="bg-stone-50">
              <th className="border-b border-stone-200 px-3 py-3 text-left font-semibold text-slate-600">
                区分
              </th>
              {sortedRows.map((row) => (
                <th
                  key={row.dayOfWeek}
                  className="border-b border-stone-200 px-3 py-3 text-center font-semibold text-slate-600"
                >
                  {businessWeekdayLabels[row.dayOfWeek] ?? "-"}曜
                </th>
              ))}
            </tr>
          </thead>

          <tbody>
            <tr>
              <td className="border-b border-stone-200 px-3 py-3 font-semibold text-slate-600">
                開始
              </td>
              {sortedRows.map((row) => (
                <td
                  key={row.dayOfWeek}
                  className="border-b border-stone-200 px-3 py-3 text-center text-slate-700"
                >
                  {getDisplayValue(row, "start")}
                </td>
              ))}
            </tr>

            <tr>
              <td className="px-3 py-3 font-semibold text-slate-600">終了</td>
              {sortedRows.map((row) => (
                <td
                  key={row.dayOfWeek}
                  className="px-3 py-3 text-center text-slate-700"
                >
                  {getDisplayValue(row, "end")}
                </td>
              ))}
            </tr>
          </tbody>
        </table>
      </div>
    </>
  );
}

function formatFlatFees(rules: PriceRule[]) {
  const flatFees = rules.filter((rule) => rule.ruleType === "flat_fee");

  if (flatFees.length === 0) {
    return "未設定";
  }

  return flatFees
    .map((rule) => {
      const weekday =
        rule.weekday === null || rule.weekday === undefined
          ? "全て"
          : `${priceWeekdayLabels[rule.weekday] ?? "-"}曜`;

      const fee =
        rule.flatFee != null ? `${rule.flatFee.toLocaleString()}円` : "-";

      return `${weekday}：${fee}${rule.note ? `（${rule.note}）` : ""}`;
    })
    .join("\n");
}

function formatAdditionalFees(rules: PriceRule[]) {
  const multipliers = rules.filter((rule) => rule.ruleType === "multiplier");

  if (multipliers.length === 0) {
    return "未設定";
  }

  return multipliers
    .map((rule) => {
      const weekday =
        rule.weekday === null || rule.weekday === undefined
          ? "全て"
          : `${priceWeekdayLabels[rule.weekday] ?? "-"}曜`;

      const start = normalizeTime(rule.startHour) ?? "-";
      const end = normalizeTime(rule.endHour) ?? "-";
      const multiplier =
        rule.multiplier != null ? `${rule.multiplier}倍` : "-";

      return `${weekday} ${start}〜${end}：${multiplier}${
        rule.note ? `（${rule.note}）` : ""
      }`;
    })
    .join("\n");
}

export default function HostRoomDetailPage() {
  const params = useParams<{ id: string }>();

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [room, setRoom] = useState<HostRoomDetail | null>(null);
  const [businessHours, setBusinessHours] = useState<BusinessHourRow[]>([]);
  const [priceRules, setPriceRules] = useState<PriceRule[]>([]);
  const [calendarEvents, setCalendarEvents] = useState<ClosureEvent[]>([]);

  const [isLoading, setIsLoading] = useState(true);
  const [isCalendarLoading, setIsCalendarLoading] = useState(true);
  const [isMobile, setIsMobile] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    const updateIsMobile = () => {
      setIsMobile(window.innerWidth < 640);
    };

    updateIsMobile();
    window.addEventListener("resize", updateIsMobile);

    return () => {
      window.removeEventListener("resize", updateIsMobile);
    };
  }, []);

  useEffect(() => {
    let ignore = false;

    const fetchDetailData = async () => {
      setIsLoading(true);
      setIsCalendarLoading(true);
      setErrorMessage("");

      try {
        const [
          roomResponse,
          businessHoursResponse,
          priceRulesResponse,
          eventsResponse,
        ] = await Promise.all([
          fetch(`${apiBaseUrl}/api/host/rooms/${params.id}`, {
            method: "GET",
            credentials: "include",
            cache: "no-store",
          }),
          fetch(`${apiBaseUrl}/api/host/rooms/${params.id}/business-hours`, {
            method: "GET",
            credentials: "include",
            cache: "no-store",
          }),
          fetch(`${apiBaseUrl}/api/host/rooms/${params.id}/price-rules`, {
            method: "GET",
            credentials: "include",
            cache: "no-store",
          }),
          fetch(`${apiBaseUrl}/api/host/rooms/${params.id}/closures/events`, {
            method: "GET",
            credentials: "include",
            cache: "no-store",
          }),
        ]);

        if (
          roomResponse.status === 401 ||
          businessHoursResponse.status === 401 ||
          priceRulesResponse.status === 401 ||
          eventsResponse.status === 401
        ) {
          window.location.href = "/auth/login";
          return;
        }

        if (
          roomResponse.status === 403 ||
          businessHoursResponse.status === 403 ||
          priceRulesResponse.status === 403 ||
          eventsResponse.status === 403
        ) {
          setErrorMessage("ホストユーザーのみアクセスできます。");
          return;
        }

        if (roomResponse.status === 404) {
          setErrorMessage("スタジオが見つかりません。");
          return;
        }

        if (!roomResponse.ok) {
          setErrorMessage("スタジオ詳細の取得に失敗しました。");
          return;
        }

        const roomData = (await roomResponse.json()) as HostRoomDetail;

        let businessHourRows: BusinessHourRow[] = [];
        if (businessHoursResponse.ok) {
          const data =
            (await businessHoursResponse.json()) as BusinessHoursResponse;
          businessHourRows = data.rows ?? [];
        }

        let rules: PriceRule[] = [];
        if (priceRulesResponse.ok) {
          const data = (await priceRulesResponse.json()) as PriceRulesResponse;
          rules = data.rules ?? [];
        }

        let events: ClosureEvent[] = [];
        if (eventsResponse.ok) {
          events = (await eventsResponse.json()) as ClosureEvent[];
        }

        if (ignore) return;

        setRoom(roomData);
        setBusinessHours(
          businessHourRows.map((row) => ({
            ...row,
            startTime: normalizeTime(row.startTime),
            endTime: normalizeTime(row.endTime),
          }))
        );
        setPriceRules(
          rules.map((rule) => ({
            ...rule,
            startHour: normalizeTime(rule.startHour),
            endHour: normalizeTime(rule.endHour),
          }))
        );
        setCalendarEvents(events ?? []);
      } catch {
        if (!ignore) {
          setErrorMessage(
            "通信エラーが発生しました。時間をおいて再度お試しください。"
          );
        }
      } finally {
        if (!ignore) {
          setIsLoading(false);
          setIsCalendarLoading(false);
        }
      }
    };

    if (params.id) {
      fetchDetailData();
    }

    return () => {
      ignore = true;
    };
  }, [apiBaseUrl, params.id]);

  if (isLoading) {
    return (
      <div className="mx-auto max-w-5xl px-4 py-12">
        <p className="text-sm text-slate-500">スタジオ詳細を読み込み中...</p>
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
          <Link
            href="/host/rooms"
            className="text-sm font-medium text-sky-700 hover:text-sky-800"
          >
            スタジオ一覧へ戻る
          </Link>
        </div>
      </div>
    );
  }

  if (!room) {
    return null;
  }

  const imageSrc = room.imageName
    ? `/storage/${room.imageName}`
    : "/images/noImage.png";

  const topRows = [
    { label: "ID", value: String(room.id) },
    { label: "スタジオ名", value: room.name },
    { label: "説明", value: room.description },
  ];

  const bottomRows = [
    { label: "基本料金", value: `${room.price.toLocaleString()}円` },
    { label: "固定費", value: formatFlatFees(priceRules) },
    { label: "加算料金", value: formatAdditionalFees(priceRules) },
    { label: "最大定員", value: `${room.capacity}人` },
    { label: "郵便番号", value: room.postalCode },
    { label: "住所", value: room.address },
  ];

  return (
    <div className="mx-auto max-w-5xl px-4 py-10">
      <nav className="mb-6 text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <Link href="/host/rooms" className="hover:text-sky-700">
          スタジオ一覧
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">スタジオ詳細</span>
      </nav>

      <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
        <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-6 py-8 text-center md:px-8">
          <p className="text-sm font-medium tracking-[0.2em] text-sky-700">
            ROOM DETAIL
          </p>
          <h1 className="mt-2 text-2xl font-semibold text-slate-800 md:text-3xl">
            {room.name}
          </h1>
          <p className="mt-3 text-sm text-slate-500">
            スタジオ情報、営業時間、料金ルール、休館予定を確認できます。
          </p>
        </div>

        <div className="p-6 md:p-8">
          <div className="mb-6 overflow-hidden rounded-2xl border border-stone-200 bg-stone-100">
            <img
              src={imageSrc}
              alt={room.name || "スタジオ画像"}
              className="h-72 w-full object-cover"
            />
          </div>

          <div className="divide-y divide-stone-200 rounded-2xl border border-stone-200 bg-white">
            {topRows.map((row) => (
              <div
                key={row.label}
                className="grid gap-2 px-4 py-4 md:grid-cols-[160px_1fr] md:px-5"
              >
                <div className="text-sm font-semibold text-slate-700">
                  {row.label}
                </div>
                <div className="whitespace-pre-wrap text-sm leading-6 text-slate-700">
                  {row.value || "未登録"}
                </div>
              </div>
            ))}

            <div className="grid gap-3 px-4 py-4 md:grid-cols-[160px_1fr] md:px-5">
              <div className="text-sm font-semibold text-slate-700">
                営業時間
              </div>
              <BusinessHoursDisplay rows={businessHours} />
            </div>

            {bottomRows.map((row) => (
              <div
                key={row.label}
                className="grid gap-2 px-4 py-4 md:grid-cols-[160px_1fr] md:px-5"
              >
                <div className="text-sm font-semibold text-slate-700">
                  {row.label}
                </div>
                <div className="whitespace-pre-wrap text-sm leading-6 text-slate-700">
                  {row.value || "未登録"}
                </div>
              </div>
            ))}
          </div>

          <div className="mt-8 rounded-2xl border border-stone-200 bg-white p-4 shadow-sm md:p-5">
            <div className="mb-4 flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
              <div>
                <h2 className="text-lg font-semibold text-slate-800">
                  休館カレンダー
                </h2>
                <p className="mt-1 text-sm text-slate-500">
                  登録済みの休館日を確認できます。
                </p>
              </div>

              {isCalendarLoading && (
                <p className="text-sm text-slate-500">カレンダー読込中...</p>
              )}
            </div>

            <div className="overflow-x-auto">
              <div className={isMobile ? "min-w-0" : "min-w-[720px]"}>
                <FullCalendar
                  plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
                  initialView={isMobile ? "timeGridDay" : "dayGridMonth"}
                  key={isMobile ? "mobile-calendar" : "desktop-calendar"}
                  locales={[jaLocale]}
                  locale="ja"
                  height="auto"
                  events={calendarEvents.map((event) => ({
                    id: String(event.id),
                    title: event.title,
                    start: event.start,
                    end: event.end,
                    allDay: event.allDay,
                  }))}
                  headerToolbar={
                    isMobile
                      ? {
                          left: "prev,next",
                          center: "title",
                          right: "today",
                        }
                      : {
                          left: "prev,next today",
                          center: "title",
                          right: "dayGridMonth,timeGridWeek,timeGridDay",
                        }
                  }
                  buttonText={{
                    today: "今日",
                    month: "月",
                    week: "週",
                    day: "日",
                  }}
                  eventTimeFormat={{
                    hour: "2-digit",
                    minute: "2-digit",
                    hour12: false,
                  }}
                  displayEventTime={true}
                />
              </div>
            </div>
          </div>

          <div className="mt-8 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <Link
              href={`/host/rooms/${room.id}/closures`}
              className="rounded-xl border border-sky-200 px-4 py-3 text-center text-sm font-medium text-sky-700 hover:bg-sky-50"
            >
              休館日設定
            </Link>

            <Link
              href={`/host/rooms/${room.id}/business-hours`}
              className="rounded-xl border border-sky-200 px-4 py-3 text-center text-sm font-medium text-sky-700 hover:bg-sky-50"
            >
              営業時間設定
            </Link>

            <Link
              href={`/host/rooms/${room.id}/price-rules`}
              className="rounded-xl border border-sky-200 px-4 py-3 text-center text-sm font-medium text-sky-700 hover:bg-sky-50"
            >
              料金ルール設定
            </Link>

            <Link
              href="/host/rooms"
              className="rounded-xl border border-stone-300 px-4 py-3 text-center text-sm font-medium text-slate-700 hover:border-sky-300 hover:text-sky-700"
            >
              一覧へ戻る
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}