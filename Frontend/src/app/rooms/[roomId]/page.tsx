"use client";

import FullCalendar from "@fullcalendar/react";
import dayGridPlugin from "@fullcalendar/daygrid";
import timeGridPlugin from "@fullcalendar/timegrid";
import interactionPlugin from "@fullcalendar/interaction";
import jaLocale from "@fullcalendar/core/locales/ja";
import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useParams } from "next/navigation";
import { apiFetch } from "@/lib/apiFetch";

type RoomBusinessHour = {
  dayOfWeek: number;
  startTime: string | null;
  endTime: string | null;
  isHoliday: boolean;
};

type RoomPriceRule = {
  id: number;
  ruleType: string;
  weekday: number | null;
  startHour: string | null;
  endHour: string | null;
  multiplier: number | null;
  flatFee: number | null;
  note: string | null;
};

type RoomReview = {
  id: number;
  score: number | null;
  content: string;
  userName: string;
  hostReply: string | null;
  hostReplyAt: string | null;
  createdAtUtc: string;
};

type RoomCalendarEvent = {
  id: string;
  title: string;
  start: string;
  end: string;
  allDay: boolean;
  type: "open" | "closure" | "reservation";
};

type RoomDetail = {
  id: number;
  name: string;
  imageName: string | null;
  description: string;
  price: number;
  capacity: number;
  postalCode: string;
  address: string;
  hostName: string;
  averageScore: number | null;
  reviewCount: number;
  businessHours: RoomBusinessHour[];
  priceRules: RoomPriceRule[];
  reviews: RoomReview[];
  hiddenHostReplies: RoomReview[];
  calendarEvents: RoomCalendarEvent[];
};

type CurrentUserResponse = {
  isAuthenticated: boolean;
  user?: {
    id: number;
    name: string;
    roles: string[];
  };
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

function formatYen(value: number) {
  return `${new Intl.NumberFormat("ja-JP").format(value)}円`;
}

function formatYenPerHour(value: number) {
  return `${new Intl.NumberFormat("ja-JP").format(value)}円 / h`;
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

function getImageSrc(imageName: string | null) {
  return imageName ? `/storage/${imageName}` : "/images/noImage.png";
}

function normalizeTime(value: string | null) {
  if (!value) return null;
  return value.slice(0, 5);
}

function Stars({
  score,
  size = "text-base",
}: {
  score: number | null;
  size?: string;
}) {
  if (score == null) {
    return <span className="text-sm text-slate-400">レビューなし</span>;
  }

  const width = `${Math.max(0, Math.min(100, (score / 5) * 100))}%`;

  return (
    <div className="flex flex-wrap items-center gap-2">
      <div className={`relative inline-block leading-none ${size}`}>
        <div className="text-gray-300">★★★★★</div>
        <div
          className="absolute left-0 top-0 overflow-hidden whitespace-nowrap text-amber-400"
          style={{ width }}
        >
          ★★★★★
        </div>
      </div>
      <span className="text-sm text-slate-600">{score.toFixed(1)} / 5</span>
    </div>
  );
}

function BusinessHoursTable({ rows }: { rows: RoomBusinessHour[] }) {
  const sortedRows = [...rows].sort((a, b) => a.dayOfWeek - b.dayOfWeek);

  if (sortedRows.length === 0) {
    return <p className="text-sm text-slate-500">営業時間の設定がありません。</p>;
  }

  const formatTime = (value: string | null) => normalizeTime(value) ?? "-";

  return (
    <>
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
                <span className="text-sm font-semibold text-slate-700">
                  {label}曜
                </span>
                {row.isHoliday ? (
                  <span className="rounded-full bg-stone-200 px-3 py-1 text-xs font-medium text-slate-600">
                    休み
                  </span>
                ) : (
                  <span className="text-sm font-medium text-slate-700">
                    {formatTime(row.startTime)}〜{formatTime(row.endTime)}
                  </span>
                )}
              </div>
            </div>
          );
        })}
      </div>

      <div className="hidden md:block">
  <table className="w-full table-fixed overflow-hidden rounded-xl border border-stone-200 text-sm">
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
                  {row.isHoliday ? "休" : formatTime(row.startTime)}
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
                  {row.isHoliday ? "休" : formatTime(row.endTime)}
                </td>
              ))}
            </tr>
          </tbody>
        </table>
      </div>
    </>
  );
}

function PriceRulesDisplay({
  rules,
  basePrice,
}: {
  rules: RoomPriceRule[];
  basePrice: number;
}) {
  const flatFeeRules = rules.filter((rule) => rule.ruleType === "flat_fee");
  const multiplierRules = rules.filter((rule) => rule.ruleType === "multiplier");

  const formatWeekday = (weekday: number | null) => {
    if (weekday == null) return "全て";
    return `${priceWeekdayLabels[weekday] ?? "-"}曜`;
  };

  return (
    <div className="space-y-4">
      <div>
        <div className="mb-2 text-sm font-semibold text-slate-700">固定費</div>
        {flatFeeRules.length === 0 ? (
          <p className="text-sm text-slate-500">未設定</p>
        ) : (
          <div className="space-y-2">
            {flatFeeRules.map((rule) => (
              <div
                key={rule.id}
                className="rounded-xl border border-stone-200 bg-stone-50 px-3 py-2 text-sm text-slate-700"
              >
                {formatWeekday(rule.weekday)}：
                {rule.flatFee != null ? formatYen(rule.flatFee) : "-"}
                {rule.note ? `（${rule.note}）` : ""}
              </div>
            ))}
          </div>
        )}
      </div>

      <div>
        <div className="mb-2 text-sm font-semibold text-slate-700">加算料金</div>
        {multiplierRules.length === 0 ? (
          <p className="text-sm text-slate-500">未設定</p>
        ) : (
          <div className="space-y-2">
            {multiplierRules.map((rule) => {
              const start = normalizeTime(rule.startHour) ?? "-";
              const end = normalizeTime(rule.endHour) ?? "-";
              const multiplier = rule.multiplier ?? 1;
              const amount = Math.round(basePrice * multiplier);

              return (
                <div
                  key={rule.id}
                  className="rounded-xl border border-sky-100 bg-sky-50/40 px-3 py-2 text-sm text-slate-700"
                >
                  {formatWeekday(rule.weekday)} {start}〜{end}：
                  {multiplier}倍（{formatYen(amount)} / h）
                  {rule.note ? `（${rule.note}）` : ""}
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}

function ReservationNotice({
  roomId,
  isAuthenticated,
  roles,
}: {
  roomId: number;
  isAuthenticated: boolean;
  roles: string[];
}) {
  const isGeneralUser = roles.includes("GeneralUser");
  const isHostOrAdmin = roles.includes("Host") || roles.includes("Admin");

  if (!isAuthenticated) {
    return (
      <div className="rounded-2xl border border-sky-100 bg-sky-50 p-5">
        <h2 className="text-base font-semibold text-slate-800">予約について</h2>
        <p className="mt-3 text-sm leading-6 text-slate-600">
          予約するには一般会員としてログインしてください。
        </p>

        <div className="mt-4 grid gap-2">
          <Link
            href="/auth/login"
            className="rounded-xl bg-sky-700 px-4 py-3 text-center text-sm font-medium text-white transition hover:opacity-90"
          >
            ログインして予約する
          </Link>
          <Link
            href="/signup"
            className="rounded-xl border border-sky-200 bg-white px-4 py-3 text-center text-sm font-medium text-sky-700 transition hover:bg-sky-50"
          >
            会員登録する
          </Link>
        </div>
      </div>
    );
  }

  if (isHostOrAdmin && !isGeneralUser) {
    return (
      <div className="rounded-2xl border border-amber-200 bg-amber-50 p-5">
        <h2 className="text-base font-semibold text-slate-800">予約について</h2>
        <p className="mt-3 text-sm leading-6 text-amber-800">
          予約は一般会員のみ可能です。ホスト・管理者アカウントでは予約できません。
        </p>

        <button
          type="button"
          disabled
          className="mt-4 w-full cursor-not-allowed rounded-xl bg-stone-300 px-4 py-3 text-sm font-medium text-white"
        >
          予約する
        </button>
      </div>
    );
  }

  return (
    <div className="rounded-2xl border border-sky-100 bg-sky-50 p-5">
      <h2 className="text-base font-semibold text-slate-800">予約について</h2>
      <p className="mt-3 text-sm leading-6 text-slate-600">
        営業時間・空き状況を確認し、予約入力へ進んでください。
      </p>

      <div className="mt-4 rounded-xl border border-white bg-white/80 px-3 py-3 text-xs leading-5 text-slate-500">
        日をまたぐ連続利用をご希望の場合は、1日ごとに予約してください。
      </div>

      <Link
        href={`/rooms/${roomId}/reservations/input`}
        className="mt-4 block rounded-xl bg-sky-700 px-4 py-3 text-center text-sm font-medium text-white transition hover:opacity-90"
      >
        予約する（確認へ）
      </Link>
    </div>
  );
}

function ReviewsSection({ room }: { room: RoomDetail }) {
  return (
    <section className="rounded-[28px] border border-stone-200 bg-white p-5 shadow-sm md:p-8">
      <div className="mb-5">
        <h2 className="text-xl font-semibold text-slate-800">
          この部屋のレビュー
        </h2>

        <div className="mt-3 flex flex-wrap items-center gap-2">
          <Stars score={room.averageScore} size="text-xl" />
          <span className="text-sm text-slate-500">
            （{room.reviewCount}件）
          </span>
        </div>
      </div>

      {room.reviews.length === 0 ? (
        <p className="text-sm text-slate-500">まだレビューはありません。</p>
      ) : (
        <div className="space-y-4">
          {room.reviews.map((review) => (
            <article
              key={review.id}
              className="rounded-2xl border border-stone-200 bg-stone-50 p-4"
            >
              <div className="mb-2 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                <Stars score={review.score} />
                <span className="text-xs text-slate-500">
                  {formatDateTime(review.createdAtUtc)}
                </span>
              </div>

              <p className="whitespace-pre-wrap text-sm leading-6 text-slate-700">
                {review.content}
              </p>

              <p className="mt-2 text-xs text-slate-500">
                投稿者：{review.userName || "ユーザー"}
              </p>

              {review.hostReply && (
                <div className="mt-3 rounded-xl border border-sky-100 bg-white p-3">
                  <span className="rounded-full bg-sky-100 px-2 py-1 text-xs font-medium text-sky-700">
                    スタジオ提供者
                  </span>
                  <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-700">
                    {review.hostReply}
                  </p>
                </div>
              )}
            </article>
          ))}
        </div>
      )}

      {room.hiddenHostReplies.length > 0 && (
        <div className="mt-8">
          <h3 className="mb-3 text-base font-semibold text-slate-800">
            ホストからのお知らせ
          </h3>

          <div className="space-y-3">
            {room.hiddenHostReplies.map((reply) => (
              <article
                key={reply.id}
                className="rounded-2xl border border-sky-100 bg-sky-50/50 p-4"
              >
                <div className="mb-2 flex flex-wrap gap-2">
                  <span className="rounded-full bg-stone-200 px-2 py-1 text-xs font-medium text-slate-600">
                    非公開レビュー
                  </span>
                  <span className="rounded-full bg-sky-100 px-2 py-1 text-xs font-medium text-sky-700">
                    スタジオ提供者
                  </span>
                </div>
                <p className="whitespace-pre-wrap text-sm leading-6 text-slate-700">
                  {reply.hostReply}
                </p>
              </article>
            ))}
          </div>
        </div>
      )}
    </section>
  );
}

export default function RoomDetailPage() {
  const params = useParams<{ roomId: string }>();

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [room, setRoom] = useState<RoomDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isMobile, setIsMobile] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [roles, setRoles] = useState<string[]>([]);

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

  const fetchCurrentUser = async () => {
    try {
const response = await apiFetch(`${apiBaseUrl}/api/auth/me`, {
  method: "GET",
  cache: "no-store",
});

      if (!response.ok) {
        if (!ignore) {
          setIsAuthenticated(false);
          setRoles([]);
        }
        return;
      }

      const data = (await response.json()) as CurrentUserResponse;

      if (!ignore) {
        setIsAuthenticated(data.isAuthenticated);
        setRoles(data.user?.roles ?? []);
      }
    } catch {
      if (!ignore) {
        setIsAuthenticated(false);
        setRoles([]);
      }
    }
  };

  fetchCurrentUser();

  return () => {
    ignore = true;
  };
}, [apiBaseUrl]);

  useEffect(() => {
    let ignore = false;

    const fetchRoom = async () => {
      setIsLoading(true);
      setErrorMessage("");

      try {
        const response = await fetch(
          `${apiBaseUrl}/api/rooms/${params.roomId}`,
          {
            method: "GET",
            cache: "no-store",
          }
        );

        if (response.status === 404) {
          setErrorMessage("スタジオが見つかりません。");
          return;
        }

        if (!response.ok) {
          setErrorMessage("スタジオ詳細の取得に失敗しました。");
          return;
        }

        const data = (await response.json()) as RoomDetail;

        if (!ignore) {
          setRoom(data);
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

    if (params.roomId) {
      fetchRoom();
    }

    return () => {
      ignore = true;
    };
  }, [apiBaseUrl, params.roomId]);

  if (isLoading) {
    return (
      <main className="mx-auto max-w-6xl px-4 py-12">
        <p className="text-sm text-slate-500">スタジオ詳細を読み込み中...</p>
      </main>
    );
  }

  if (errorMessage) {
    return (
      <main className="mx-auto max-w-6xl px-4 py-12">
        <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
          {errorMessage}
        </div>

        <div className="mt-6">
          <Link
            href="/rooms"
            className="text-sm font-medium text-sky-700 hover:text-sky-800"
          >
            スタジオ一覧へ戻る
          </Link>
        </div>
      </main>
    );
  }

  if (!room) {
    return null;
  }

  const imageSrc = getImageSrc(room.imageName);

  return (
    <main className="bg-stone-50">
      <div className="mx-auto max-w-6xl px-4 py-6 sm:py-10">
        <nav className="mb-5 text-sm text-slate-500">
          <Link href="/" className="hover:text-sky-700">
            ホーム
          </Link>
          <span className="mx-2">&gt;</span>
          <Link href="/rooms" className="hover:text-sky-700">
            スタジオ一覧
          </Link>
          <span className="mx-2">&gt;</span>
          <span className="text-slate-700">スタジオ詳細</span>
        </nav>

        <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
          <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 text-center sm:px-8 sm:py-9">
            <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
              ROOM DETAIL
            </p>
            <h1 className="mt-3 text-2xl font-semibold text-slate-800 md:text-3xl">
              {room.name}
            </h1>
            <p className="mt-3 text-sm leading-6 text-slate-500">
              スタジオ情報、営業時間、料金ルール、空き状況、レビューを確認できます。
            </p>
          </div>

          <div className="p-5 md:p-8">
            <div className="overflow-hidden rounded-2xl border border-stone-200 bg-stone-100">
              <img
                src={imageSrc}
                alt={room.name}
                className="h-72 w-full object-cover md:h-[420px]"
              />
            </div>

            <div className="mt-8 grid gap-6 lg:grid-cols-[1fr_320px]">
              <div className="space-y-6">
                <div className="divide-y divide-stone-200 rounded-2xl border border-stone-200 bg-white">
                  <InfoRow label="スタジオ名" value={room.name} />
                  <InfoRow label="説明" value={room.description} multiline />
                  <div className="grid gap-3 px-4 py-4 md:grid-cols-[160px_1fr] md:px-5">
                    <div className="text-sm font-semibold text-slate-700">
                      営業時間
                    </div>
                    <BusinessHoursTable rows={room.businessHours} />
                  </div>
                  <InfoRow label="基本料金" value={formatYenPerHour(room.price)} />
                  <div className="grid gap-3 px-4 py-4 md:grid-cols-[160px_1fr] md:px-5">
                    <div className="text-sm font-semibold text-slate-700">
                      料金ルール
                    </div>
                    <PriceRulesDisplay
                      rules={room.priceRules}
                      basePrice={room.price}
                    />
                  </div>
                  <InfoRow label="定員" value={`${room.capacity}人`} />
                  <InfoRow label="郵便番号" value={`〒${room.postalCode}`} />
                  <InfoRow label="住所" value={room.address} />
                  <InfoRow label="スタジオ提供者" value={room.hostName} />
                </div>
              </div>

              <aside className="space-y-4">
<ReservationNotice
  roomId={room.id}
  isAuthenticated={isAuthenticated}
  roles={roles}
/>

                <div className="rounded-2xl border border-stone-200 bg-white p-5">
                  <h2 className="text-base font-semibold text-slate-800">
                    評価
                  </h2>
                  <div className="mt-3">
                    <Stars score={room.averageScore} size="text-xl" />
                    <p className="mt-2 text-sm text-slate-500">
                      公開レビュー {room.reviewCount} 件
                    </p>
                  </div>
                </div>
              </aside>
            </div>
          </div>
        </section>

        <section className="mt-8 rounded-[28px] border border-stone-200 bg-white p-5 shadow-sm md:p-8">
          <div className="mb-4">
            <h2 className="text-xl font-semibold text-slate-800">
              カレンダー
            </h2>
            <p className="mt-2 text-sm text-slate-500">
              営業時間・休館・予約済みの予定を確認できます。
            </p>
          </div>

          <div className="mb-4 flex flex-wrap gap-4 text-xs text-slate-600">
            <Legend color="bg-emerald-100 border-emerald-200" label="営業時間" />
            <Legend color="bg-slate-200 border-slate-300" label="休業・休館" />
            <Legend color="bg-rose-400 border-rose-500" label="予約済み" />
          </div>

          <div className="overflow-x-auto">
            <div className={isMobile ? "min-w-0" : "min-w-[760px]"}>
              <FullCalendar
                plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
                initialView={isMobile ? "timeGridDay" : "timeGridWeek"}
                key={isMobile ? "mobile-calendar" : "desktop-calendar"}
                locales={[jaLocale]}
                locale="ja"
                timeZone="local"
                height="auto"
                nowIndicator
                slotMinTime="00:00:00"
                slotMaxTime="24:00:00"
                events={room.calendarEvents.map((event) => ({
                  id: event.id,
                  title: event.title,
                  start: event.start,
                  end: event.end,
                  allDay: event.allDay,
                  display:
                    event.type === "open" || event.type === "closure"
                      ? "background"
                      : "auto",
                  classNames:
                    event.type === "open"
                      ? ["bg-open"]
                      : event.type === "closure"
                      ? ["bg-closure"]
                      : ["evt-reservation"],
                  extendedProps: {
                    type: event.type,
                  },
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
                        right: "timeGridWeek,timeGridDay",
                      }
                }
                buttonText={{
                  today: "今日",
                  week: "週",
                  day: "日",
                }}
                eventDidMount={(info) => {
                  const type = info.event.extendedProps.type;

                  if (info.event.display === "background") {
                    info.el.style.opacity = "0.6";

                    if (type === "open") {
                      info.el.style.backgroundColor = "#bff0bf";
                    }

                    if (type === "closure") {
                      info.el.style.backgroundColor = "#d9dde2";
                    }
                  }

                  if (type === "reservation") {
                    info.el.style.backgroundColor = "#ff6b6b";
                    info.el.style.borderColor = "#dc5a5a";
                    info.el.style.color = "#fff";
                  }
                }}
                eventTimeFormat={{
                  hour: "2-digit",
                  minute: "2-digit",
                  hour12: false,
                }}
              />
            </div>
          </div>
        </section>

        <div className="mt-8">
          <ReviewsSection room={room} />
        </div>

        <div className="mt-8 flex justify-center">
          <Link
            href="/rooms"
            className="rounded-xl border border-stone-300 bg-white px-6 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
          >
            スタジオ一覧へ戻る
          </Link>
        </div>
      </div>
    </main>
  );
}

function InfoRow({
  label,
  value,
  multiline = false,
}: {
  label: string;
  value: string;
  multiline?: boolean;
}) {
  return (
    <div className="grid gap-2 px-4 py-4 md:grid-cols-[160px_1fr] md:px-5">
      <div className="text-sm font-semibold text-slate-700">{label}</div>
      <div
        className={
          multiline
            ? "whitespace-pre-wrap text-sm leading-6 text-slate-700"
            : "text-sm leading-6 text-slate-700"
        }
      >
        {value || "未登録"}
      </div>
    </div>
  );
}

function Legend({ color, label }: { color: string; label: string }) {
  return (
    <span className="inline-flex items-center gap-2">
      <span className={`h-3 w-3 rounded-sm border ${color}`} />
      {label}
    </span>
  );
}