"use client";

import FullCalendar from "@fullcalendar/react";
import dayGridPlugin from "@fullcalendar/daygrid";
import timeGridPlugin from "@fullcalendar/timegrid";
import interactionPlugin from "@fullcalendar/interaction";
import jaLocale from "@fullcalendar/core/locales/ja";
import type { EventClickArg } from "@fullcalendar/core";
import Link from "next/link";
import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { useParams } from "next/navigation";

type Closure = {
  id: number;
  roomId: number;
  startAt: string;
  endAt: string;
  reason?: string | null;
};

type ClosureEvent = {
  id: number;
  title: string;
  start: string;
  end: string;
  allDay: boolean;
};

type CreateClosureRequest = {
  startAt: string;
  endAt: string;
  reason?: string;
};

export default function HostRoomClosuresPage() {
  const params = useParams<{ id: string }>();
  const roomId = params.id;

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [closures, setClosures] = useState<Closure[]>([]);
  const [calendarEvents, setCalendarEvents] = useState<ClosureEvent[]>([]);

  const [startAt, setStartAt] = useState("");
  const [endAt, setEndAt] = useState("");
  const [reason, setReason] = useState("");

  const [isLoading, setIsLoading] = useState(true);
  const [isCalendarLoading, setIsCalendarLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [isMobile, setIsMobile] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  const fetchClosures = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const response = await fetch(
        `${apiBaseUrl}/api/host/rooms/${roomId}/closures`,
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
        setErrorMessage("このスタジオの休館日を操作する権限がありません。");
        return;
      }

      if (!response.ok) {
        setErrorMessage("休館日の取得に失敗しました。");
        return;
      }

      const data = (await response.json()) as Closure[];
      setClosures(data ?? []);
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    } finally {
      setIsLoading(false);
    }
  }, [apiBaseUrl, roomId]);

  const fetchCalendarEvents = useCallback(async () => {
    setIsCalendarLoading(true);

    try {
      const response = await fetch(
        `${apiBaseUrl}/api/host/rooms/${roomId}/closures/events`,
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
        setErrorMessage("このスタジオの休館日を表示する権限がありません。");
        return;
      }

      if (!response.ok) {
        setErrorMessage("カレンダー情報の取得に失敗しました。");
        return;
      }

      const data = (await response.json()) as ClosureEvent[];
      setCalendarEvents(data ?? []);
    } catch {
      setErrorMessage("カレンダー情報の取得中に通信エラーが発生しました。");
    } finally {
      setIsCalendarLoading(false);
    }
  }, [apiBaseUrl, roomId]);

  const reloadData = useCallback(async () => {
    await Promise.all([fetchClosures(), fetchCalendarEvents()]);
  }, [fetchClosures, fetchCalendarEvents]);

  useEffect(() => {
    if (!roomId) return;

    reloadData();
  }, [roomId, reloadData]);

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

  const handleCreate = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    setErrorMessage("");
    setSuccessMessage("");

    if (!startAt || !endAt) {
      setErrorMessage("開始日時と終了日時を入力してください。");
      return;
    }

    if (new Date(startAt) >= new Date(endAt)) {
      setErrorMessage("終了日時は開始日時より後にしてください。");
      return;
    }

    const request: CreateClosureRequest = {
      startAt,
      endAt,
      reason: reason.trim() || undefined,
    };

    setIsSubmitting(true);

    try {
      const response = await fetch(
        `${apiBaseUrl}/api/host/rooms/${roomId}/closures`,
        {
          method: "POST",
          credentials: "include",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(request),
        }
      );

      if (response.status === 401) {
        window.location.href = "/auth/login";
        return;
      }

      if (response.status === 403) {
        setErrorMessage("このスタジオの休館日を追加する権限がありません。");
        return;
      }

      if (!response.ok) {
        setErrorMessage("休館日の追加に失敗しました。");
        return;
      }

      setStartAt("");
      setEndAt("");
      setReason("");
      setSuccessMessage("休館日を追加しました。");

      await reloadData();
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (closureId: number) => {
    const confirmed = window.confirm("この休館日を削除しますか？");

    if (!confirmed) {
      return;
    }

    setErrorMessage("");
    setSuccessMessage("");

    try {
      const response = await fetch(
        `${apiBaseUrl}/api/host/rooms/${roomId}/closures/${closureId}`,
        {
          method: "DELETE",
          credentials: "include",
        }
      );

      if (response.status === 401) {
        window.location.href = "/auth/login";
        return;
      }

      if (response.status === 403) {
        setErrorMessage("この休館日を削除する権限がありません。");
        return;
      }

      if (!response.ok) {
        setErrorMessage("休館日の削除に失敗しました。");
        return;
      }

      setSuccessMessage("休館日を削除しました。");
      await reloadData();
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    }
  };

  const handleCalendarEventClick = async (arg: EventClickArg) => {
    const closureId = Number(arg.event.id);

    if (!closureId) {
      return;
    }

    await handleDelete(closureId);
  };

  const handleAllDay = () => {
    if (!startAt) {
      setErrorMessage("終日設定を使う場合は、先に開始日時を入力してください。");
      return;
    }

    const startDate = new Date(startAt);
    startDate.setHours(0, 0, 0, 0);

    const endDate = new Date(startDate);
    endDate.setDate(endDate.getDate() + 1);

    setStartAt(toDatetimeLocalValue(startDate));
    setEndAt(toDatetimeLocalValue(endDate));
    setErrorMessage("");
  };

  const formatDateTime = (value: string) => {
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
  };

  const isAllDay = (closure: Closure) => {
    const start = new Date(closure.startAt);
    const end = new Date(closure.endAt);

    return (
      start.getHours() === 0 &&
      start.getMinutes() === 0 &&
      end.getHours() === 0 &&
      end.getMinutes() === 0
    );
  };

  return (
    <div className="mx-auto max-w-6xl px-4 py-10">
      <nav className="mb-6 text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <Link href="/host/rooms" className="hover:text-sky-700">
          スタジオ一覧
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">休館日設定</span>
      </nav>

      <section className="rounded-[28px] border border-stone-200 bg-white p-6 shadow-sm md:p-8">
        <div className="mb-6 text-center">
          <p className="text-sm font-medium tracking-[0.2em] text-sky-700">
            CLOSURE SETTINGS
          </p>
          <h1 className="mt-2 text-2xl font-semibold text-slate-800">
            休館日設定
          </h1>
          <p className="mt-2 text-sm text-slate-500">
            スタジオの休館日時を登録・確認・削除できます。
          </p>
        </div>

        {successMessage && (
          <div className="mb-5 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
            {successMessage}
          </div>
        )}

        {errorMessage && (
          <div className="mb-5 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
            {errorMessage}
          </div>
        )}

        <div className="mb-8 rounded-2xl border border-stone-200 bg-white p-4 shadow-sm md:p-5">
          <div className="mb-4 flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <h2 className="text-lg font-semibold text-slate-800">
                休館日カレンダー
              </h2>
              <p className="mt-1 text-sm text-slate-500">
                登録済みの休館日を月・週・日単位で確認できます。イベントをクリックすると削除できます。
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
      eventClick={handleCalendarEventClick}
    />
  </div>
</div>
        </div>

        <div className="mb-8 rounded-2xl border border-stone-200 bg-stone-50 p-5">
          <h2 className="text-lg font-semibold text-slate-800">
            休館日を追加
          </h2>

          <form onSubmit={handleCreate} className="mt-4" noValidate>
            <div className="grid gap-4 md:grid-cols-3">
              <div>
                <label className="mb-1 block text-sm font-medium text-slate-600">
                  開始日時
                </label>
                <input
                  type="datetime-local"
                  value={startAt}
                  onChange={(e) => setStartAt(e.target.value)}
                  step="900"
                  required
                  className="w-full rounded-xl border border-stone-300 bg-white px-4 py-3 text-sm outline-none transition focus:border-sky-500"
                />
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium text-slate-600">
                  終了日時
                </label>
                <input
                  type="datetime-local"
                  value={endAt}
                  onChange={(e) => setEndAt(e.target.value)}
                  step="900"
                  required
                  className="w-full rounded-xl border border-stone-300 bg-white px-4 py-3 text-sm outline-none transition focus:border-sky-500"
                />
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium text-slate-600">
                  理由
                </label>
                <input
                  type="text"
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  maxLength={255}
                  placeholder="メンテナンスなど"
                  className="w-full rounded-xl border border-stone-300 bg-white px-4 py-3 text-sm outline-none transition focus:border-sky-500"
                />
              </div>
            </div>

            <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-center">
              <button
                type="submit"
                disabled={isSubmitting}
                className="rounded-xl bg-sky-700 px-5 py-3 text-sm font-medium text-white transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {isSubmitting ? "追加中..." : "追加"}
              </button>

              <button
                type="button"
                onClick={handleAllDay}
                className="rounded-xl border border-stone-300 bg-white px-5 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
              >
                終日設定
              </button>
            </div>
          </form>
        </div>

        <div>
          <div className="mb-4 flex items-center justify-between">
            <h2 className="text-lg font-semibold text-slate-800">
              登録済みの休館日
            </h2>
            <p className="text-sm text-slate-500">全 {closures.length} 件</p>
          </div>

          {isLoading ? (
            <p className="text-sm text-slate-500">休館日を読み込み中...</p>
          ) : closures.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-stone-300 bg-stone-50 px-4 py-10 text-center">
              <p className="text-sm text-slate-500">
                登録済みの休館日はありません。
              </p>
            </div>
          ) : (
            <div className="grid gap-4 md:grid-cols-2">
              {closures.map((closure) => (
                <article
                  key={closure.id}
                  className="rounded-2xl border border-stone-200 bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md"
                >
                  <div className="mb-4 flex items-start justify-between gap-3">
                    <div>
                      <p className="text-xs font-medium tracking-[0.18em] text-sky-700">
                        CLOSURE #{closure.id}
                      </p>
                      <h3 className="mt-1 text-lg font-semibold text-slate-800">
                        {closure.reason || "休館"}
                      </h3>
                    </div>

                    {isAllDay(closure) && (
                      <span className="rounded-full bg-sky-50 px-3 py-1 text-xs font-medium text-sky-700">
                        終日
                      </span>
                    )}
                  </div>

                  <div className="space-y-3 text-sm">
                    <div>
                      <p className="text-xs font-medium text-slate-400">
                        開始日時
                      </p>
                      <p className="mt-1 text-slate-700">
                        {formatDateTime(closure.startAt)}
                      </p>
                    </div>

                    <div>
                      <p className="text-xs font-medium text-slate-400">
                        終了日時
                      </p>
                      <p className="mt-1 text-slate-700">
                        {formatDateTime(closure.endAt)}
                      </p>
                    </div>
                  </div>

                  <div className="mt-5 flex justify-end">
                    <button
                      type="button"
                      onClick={() => handleDelete(closure.id)}
                      className="rounded-xl border border-rose-200 px-4 py-2 text-sm font-medium text-rose-700 transition hover:bg-rose-50"
                    >
                      削除
                    </button>
                  </div>
                </article>
              ))}
            </div>
          )}
        </div>
      </section>
    </div>
  );
}

function toDatetimeLocalValue(date: Date) {
  const pad = (value: number) => String(value).padStart(2, "0");

  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(
    date.getDate()
  )}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}