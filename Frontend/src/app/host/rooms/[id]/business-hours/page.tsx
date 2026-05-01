"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useParams, useRouter } from "next/navigation";

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

const weekdayLabels: Record<number, string> = {
  1: "月",
  2: "火",
  3: "水",
  4: "木",
  5: "金",
  6: "土",
  7: "日",
};

function createTimeOptions() {
  const options: string[] = [];

  for (let h = 0; h < 24; h++) {
    for (let m = 0; m < 60; m += 15) {
      options.push(`${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}`);
    }
  }

  return options;
}

function normalizeTime(value: string | null) {
  if (!value) return null;
  return value.slice(0, 5);
}

function toMinutes(value: string | null) {
  if (!value) return null;

  const [hour, minute] = value.split(":").map(Number);

  if (Number.isNaN(hour) || Number.isNaN(minute)) {
    return null;
  }

  return hour * 60 + minute;
}

export default function HostBusinessHoursPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const timeOptions = useMemo(() => createTimeOptions(), []);

  const [roomName, setRoomName] = useState("");
  const [rows, setRows] = useState<BusinessHourRow[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  useEffect(() => {
    let ignore = false;

    const fetchBusinessHours = async () => {
      setIsLoading(true);
      setErrorMessage("");
      setSuccessMessage("");

      try {
        const response = await fetch(
          `${apiBaseUrl}/api/host/rooms/${params.id}/business-hours`,
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

        if (response.status === 404) {
          setErrorMessage("スタジオが見つかりません。");
          return;
        }

        if (!response.ok) {
          setErrorMessage("営業時間の取得に失敗しました。");
          return;
        }

        const data = (await response.json()) as BusinessHoursResponse;

        if (ignore) return;

        setRoomName(data.roomName);
        setRows(
          data.rows.map((row) => ({
            ...row,
            startTime: normalizeTime(row.startTime),
            endTime: normalizeTime(row.endTime),
          }))
        );
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

    if (params.id) {
      fetchBusinessHours();
    }

    return () => {
      ignore = true;
    };
  }, [apiBaseUrl, params.id]);

  const updateRow = (index: number, patch: Partial<BusinessHourRow>) => {
    setRows((current) =>
      current.map((row, i) => {
        if (i !== index) return row;

        const next = {
          ...row,
          ...patch,
        };

        if (patch.isHoliday === true) {
          next.startTime = null;
          next.endTime = null;
        }

        return next;
      })
    );

    setErrorMessage("");
    setSuccessMessage("");
  };

  const validate = () => {
    for (const row of rows) {
      if (row.isHoliday) {
        continue;
      }

      if (!row.startTime) {
        return `${weekdayLabels[row.dayOfWeek]}曜の開始時刻を選択してください。`;
      }

      if (!row.endTime) {
        return `${weekdayLabels[row.dayOfWeek]}曜の終了時刻を選択してください。`;
      }

      const start = toMinutes(row.startTime);
      const end = toMinutes(row.endTime);

      if (start === null || end === null) {
        return `${weekdayLabels[row.dayOfWeek]}曜の時刻形式が不正です。`;
      }

      if (end <= start) {
        return `${weekdayLabels[row.dayOfWeek]}曜の終了時刻は開始時刻より後にしてください。`;
      }
    }

    return "";
  };

  const handleSave = async () => {
    const validationMessage = validate();

    if (validationMessage) {
      setErrorMessage(validationMessage);
      setSuccessMessage("");
      return;
    }

    setIsSaving(true);
    setErrorMessage("");
    setSuccessMessage("");

    try {
      const response = await fetch(
        `${apiBaseUrl}/api/host/rooms/${params.id}/business-hours`,
        {
          method: "PUT",
          credentials: "include",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            rows,
          }),
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
        setErrorMessage("スタジオが見つかりません。");
        return;
      }

      if (!response.ok) {
        const data = await response.json().catch(() => null);
        setErrorMessage(data?.message ?? "営業時間の保存に失敗しました。");
        return;
      }

      setSuccessMessage("営業時間を保存しました。");
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <div className="mx-auto max-w-6xl px-4 py-12">
        <div className="rounded-[28px] border border-stone-200 bg-white p-8 shadow-sm">
          <p className="text-sm text-slate-500">営業時間を読み込み中...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-6xl px-4 py-6 sm:py-10">
      <nav className="mb-5 overflow-x-auto whitespace-nowrap text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <Link href="/host/rooms" className="hover:text-sky-700">
          スタジオ一覧
        </Link>
        <span className="mx-2">&gt;</span>
        <Link href={`/host/rooms/${params.id}`} className="hover:text-sky-700">
          スタジオ詳細
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">営業時間設定</span>
      </nav>

      <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
        <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 sm:px-8 sm:py-9">
          <div className="mx-auto max-w-3xl text-center">
            <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
              BUSINESS HOURS
            </p>

            <h1 className="mt-3 text-xl font-semibold text-slate-800 sm:text-2xl md:text-3xl">
              {roomName ? `${roomName} の営業時間設定` : "営業時間設定"}
            </h1>

            <p className="mt-3 text-sm leading-6 text-slate-500">
              曜日ごとの営業開始・終了時刻を設定できます。
              休みを選択した曜日は予約受付対象外になります。
            </p>
          </div>
        </div>

        <div className="px-5 py-5 sm:px-8 sm:py-7">
          {errorMessage && (
            <div className="mb-5 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
              {errorMessage}
            </div>
          )}

          {successMessage && (
            <div className="mb-5 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
              {successMessage}
            </div>
          )}

          {/* mobile: card layout */}
          <div className="grid gap-4 md:hidden">
            {rows.map((row, index) => (
              <div
                key={row.dayOfWeek}
                className={
                  row.isHoliday
                    ? "rounded-2xl border border-stone-200 bg-stone-50 p-4"
                    : "rounded-2xl border border-sky-100 bg-white p-4 shadow-sm"
                }
              >
                <div className="mb-4 flex items-center justify-between gap-3">
                  <div>
                    <p className="text-xs font-medium tracking-[0.18em] text-sky-700">
                      DAY {row.dayOfWeek}
                    </p>
                    <h2 className="mt-1 text-lg font-semibold text-slate-800">
                      {weekdayLabels[row.dayOfWeek]}曜日
                    </h2>
                  </div>

                  <label className="inline-flex cursor-pointer items-center gap-2 rounded-full bg-white px-3 py-2 text-sm text-slate-700 shadow-sm ring-1 ring-stone-200">
                    <input
                      type="checkbox"
                      checked={row.isHoliday}
                      onChange={(e) =>
                        updateRow(index, {
                          isHoliday: e.target.checked,
                        })
                      }
                      className="h-4 w-4 rounded border-stone-300 text-sky-700"
                    />
                    休み
                  </label>
                </div>

                <div className="grid grid-cols-1 gap-3">
                  <div>
                    <label className="mb-1 block text-xs font-semibold text-slate-500">
                      開始時刻
                    </label>
                    <select
                      value={row.startTime ?? ""}
                      disabled={row.isHoliday}
                      onChange={(e) =>
                        updateRow(index, {
                          startTime: e.target.value || null,
                        })
                      }
                      className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500 disabled:bg-stone-100 disabled:text-slate-400"
                    >
                      <option value="">-- 選択 --</option>
                      {timeOptions.map((time) => (
                        <option key={time} value={time}>
                          {time}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="mb-1 block text-xs font-semibold text-slate-500">
                      終了時刻
                    </label>
                    <select
                      value={row.endTime ?? ""}
                      disabled={row.isHoliday}
                      onChange={(e) =>
                        updateRow(index, {
                          endTime: e.target.value || null,
                        })
                      }
                      className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500 disabled:bg-stone-100 disabled:text-slate-400"
                    >
                      <option value="">-- 選択 --</option>
                      {timeOptions.map((time) => (
                        <option key={time} value={time}>
                          {time}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* desktop/tablet: table-like layout */}
          <div className="hidden md:block">
            <div className="grid grid-cols-[120px_1fr_1fr_120px] gap-3 rounded-2xl bg-slate-50 px-4 py-3 text-xs font-semibold tracking-wide text-slate-500">
              <div>曜日</div>
              <div>開始時刻</div>
              <div>終了時刻</div>
              <div>休み</div>
            </div>

            <div className="mt-3 space-y-3">
              {rows.map((row, index) => (
                <div
                  key={row.dayOfWeek}
                  className={
                    row.isHoliday
                      ? "grid grid-cols-[120px_1fr_1fr_120px] items-center gap-3 rounded-2xl border border-stone-200 bg-stone-50 px-4 py-4"
                      : "grid grid-cols-[120px_1fr_1fr_120px] items-center gap-3 rounded-2xl border border-stone-200 bg-white px-4 py-4 shadow-sm"
                  }
                >
                  <div>
                    <p className="text-xs font-medium tracking-[0.18em] text-sky-700">
                      {row.dayOfWeek}
                    </p>
                    <p className="mt-1 text-sm font-semibold text-slate-800">
                      {weekdayLabels[row.dayOfWeek]}曜
                    </p>
                  </div>

                  <select
                    value={row.startTime ?? ""}
                    disabled={row.isHoliday}
                    onChange={(e) =>
                      updateRow(index, {
                        startTime: e.target.value || null,
                      })
                    }
                    className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500 disabled:bg-stone-100 disabled:text-slate-400"
                  >
                    <option value="">-- 選択 --</option>
                    {timeOptions.map((time) => (
                      <option key={time} value={time}>
                        {time}
                      </option>
                    ))}
                  </select>

                  <select
                    value={row.endTime ?? ""}
                    disabled={row.isHoliday}
                    onChange={(e) =>
                      updateRow(index, {
                        endTime: e.target.value || null,
                      })
                    }
                    className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500 disabled:bg-stone-100 disabled:text-slate-400"
                  >
                    <option value="">-- 選択 --</option>
                    {timeOptions.map((time) => (
                      <option key={time} value={time}>
                        {time}
                      </option>
                    ))}
                  </select>

                  <label className="inline-flex cursor-pointer items-center gap-2 text-sm text-slate-700">
                    <input
                      type="checkbox"
                      checked={row.isHoliday}
                      onChange={(e) =>
                        updateRow(index, {
                          isHoliday: e.target.checked,
                        })
                      }
                      className="h-4 w-4 rounded border-stone-300 text-sky-700"
                    />
                    休み
                  </label>
                </div>
              ))}
            </div>
          </div>
        </div>

        <div className="sticky bottom-0 border-t border-stone-100 bg-white/95 px-5 py-4 backdrop-blur sm:px-8">
          <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={() => router.push(`/host/rooms/${params.id}`)}
              className="rounded-xl border border-stone-300 px-5 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
            >
              詳細へ戻る
            </button>

            <button
              type="button"
              onClick={handleSave}
              disabled={isSaving}
              className="rounded-xl bg-sky-700 px-6 py-3 text-sm font-medium text-white shadow-sm transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {isSaving ? "保存中..." : "保存する"}
            </button>
          </div>
        </div>
      </section>
    </div>
  );
}