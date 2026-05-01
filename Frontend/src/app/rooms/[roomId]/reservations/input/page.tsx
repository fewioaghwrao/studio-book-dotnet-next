"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { useParams, useRouter } from "next/navigation";

type CurrentUserResponse = {
  isAuthenticated: boolean;
  user?: {
    id: number;
    name: string;
    roles: string[];
  };
};

type RoomBusinessHour = {
  dayOfWeek: number;
  startTime: string | null;
  endTime: string | null;
  isHoliday: boolean;
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
  businessHours: RoomBusinessHour[];
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

function getImageSrc(imageName: string | null) {
  return imageName ? `/storage/${imageName}` : "/images/noImage.png";
}

function normalizeTime(value: string | null) {
  if (!value) return null;
  return value.slice(0, 5);
}

function formatYenPerHour(value: number) {
  return `${new Intl.NumberFormat("ja-JP").format(value)}円 / h`;
}

function toDateTimeLocal(date: string, time: string) {
  return `${date}T${time}:00`;
}

function getTodayString() {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
}

function BusinessHoursSummary({ rows }: { rows: RoomBusinessHour[] }) {
  const sortedRows = [...rows].sort((a, b) => a.dayOfWeek - b.dayOfWeek);

  if (sortedRows.length === 0) {
    return (
      <p className="text-sm text-slate-500">
        営業時間の設定がありません。
      </p>
    );
  }

  return (
    <div className="grid gap-2">
      {sortedRows.map((row) => {
        const label = weekdayLabels[row.dayOfWeek] ?? "-";

        return (
          <div
            key={row.dayOfWeek}
            className={
              row.isHoliday
                ? "flex items-center justify-between rounded-xl border border-stone-200 bg-stone-50 px-3 py-2 text-sm"
                : "flex items-center justify-between rounded-xl border border-sky-100 bg-sky-50/40 px-3 py-2 text-sm"
            }
          >
            <span className="font-medium text-slate-700">{label}曜</span>

            {row.isHoliday ? (
              <span className="rounded-full bg-stone-200 px-3 py-1 text-xs text-slate-600">
                休み
              </span>
            ) : (
              <span className="text-slate-700">
                {normalizeTime(row.startTime) ?? "-"}〜
                {normalizeTime(row.endTime) ?? "-"}
              </span>
            )}
          </div>
        );
      })}
    </div>
  );
}

export default function ReservationInputPage() {
  const params = useParams<{ roomId: string }>();
  const router = useRouter();

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [room, setRoom] = useState<RoomDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  const [startDate, setStartDate] = useState("");
  const [startTime, setStartTime] = useState("");
  const [endDate, setEndDate] = useState("");
  const [endTime, setEndTime] = useState("");

  useEffect(() => {
    let ignore = false;

    const fetchInitialData = async () => {
      setIsLoading(true);
      setErrorMessage("");

      try {
        const [meResponse, roomResponse] = await Promise.all([
          fetch(`${apiBaseUrl}/api/auth/me`, {
            method: "GET",
            credentials: "include",
            cache: "no-store",
          }),
          fetch(`${apiBaseUrl}/api/rooms/${params.roomId}`, {
            method: "GET",
            cache: "no-store",
          }),
        ]);

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

        if (roomResponse.status === 404) {
          setErrorMessage("スタジオが見つかりません。");
          return;
        }

        if (!roomResponse.ok) {
          setErrorMessage("スタジオ情報の取得に失敗しました。");
          return;
        }

        const roomData = (await roomResponse.json()) as RoomDetail;

        if (!ignore) {
          setRoom(roomData);
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
      fetchInitialData();
    }

    return () => {
      ignore = true;
    };
  }, [apiBaseUrl, params.roomId]);

  const validate = () => {
    if (!startDate) return "開始日を入力してください。";
    if (!startTime) return "開始時刻を入力してください。";
    if (!endDate) return "終了日を入力してください。";
    if (!endTime) return "終了時刻を入力してください。";

    const startAt = new Date(toDateTimeLocal(startDate, startTime));
    const endAt = new Date(toDateTimeLocal(endDate, endTime));
    const now = new Date();

    if (Number.isNaN(startAt.getTime()) || Number.isNaN(endAt.getTime())) {
      return "日時の形式が正しくありません。";
    }

    if (startAt < now) {
      return "開始日時は現在時刻より後にしてください。";
    }

    if (startAt >= endAt) {
      return "終了日時は開始日時より後にしてください。";
    }

    return "";
  };

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setErrorMessage("");

    const validationMessage = validate();

    if (validationMessage) {
      setErrorMessage(validationMessage);
      return;
    }

    const query = new URLSearchParams({
      startDate,
      startTime,
      endDate,
      endTime,
    });

    router.push(
      `/rooms/${params.roomId}/reservations/confirm?${query.toString()}`
    );
  };

  if (isLoading) {
    return (
      <main className="mx-auto max-w-5xl px-4 py-12">
        <p className="text-sm text-slate-500">予約入力画面を読み込み中...</p>
      </main>
    );
  }

  if (errorMessage && !room) {
    return (
      <main className="mx-auto max-w-5xl px-4 py-12">
        <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
          {errorMessage}
        </div>

        <div className="mt-6">
          <Link
            href={`/rooms/${params.roomId}`}
            className="text-sm font-medium text-sky-700 hover:text-sky-800"
          >
            スタジオ詳細へ戻る
          </Link>
        </div>
      </main>
    );
  }

  if (!room) {
    return null;
  }

  return (
    <main className="bg-stone-50">
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
          <Link href={`/rooms/${room.id}`} className="hover:text-sky-700">
            スタジオ詳細
          </Link>
          <span className="mx-2">&gt;</span>
          <span className="text-slate-700">予約入力</span>
        </nav>

        <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
          <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 text-center sm:px-8 sm:py-9">
            <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
              RESERVATION INPUT
            </p>
            <h1 className="mt-3 text-2xl font-semibold text-slate-800 md:text-3xl">
              予約入力
            </h1>
            <p className="mt-3 text-sm leading-6 text-slate-500">
              利用日時を入力して、予約内容確認へ進みます。
            </p>
          </div>

          <div className="grid gap-8 p-5 md:p-8 lg:grid-cols-[1fr_340px]">
            <form className="space-y-5" onSubmit={handleSubmit}>
              {errorMessage && (
                <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
                  {errorMessage}
                </div>
              )}

              <div className="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm leading-6 text-amber-800">
                本スタジオは営業時間内のご利用のみ可能です。日をまたぐ連続利用をご希望の場合は、1日ごとに予約してください。
              </div>

              <div className="grid gap-5 md:grid-cols-2">
                <InputField
                  label="開始日"
                  type="date"
                  value={startDate}
                  onChange={setStartDate}
                  min={getTodayString()}
                />

                <InputField
                  label="開始時刻"
                  type="time"
                  value={startTime}
                  onChange={setStartTime}
                  step="1800"
                />
              </div>

              <div className="grid gap-5 md:grid-cols-2">
                <InputField
                  label="終了日"
                  type="date"
                  value={endDate}
                  onChange={setEndDate}
                  min={startDate || getTodayString()}
                />

                <InputField
                  label="終了時刻"
                  type="time"
                  value={endTime}
                  onChange={setEndTime}
                  step="1800"
                />
              </div>

              <div className="flex flex-col-reverse gap-3 pt-4 sm:flex-row sm:justify-center">
                <Link
                  href={`/rooms/${room.id}`}
                  className="inline-flex justify-center rounded-xl border border-stone-300 px-6 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
                >
                  戻る
                </Link>

                <button
                  type="submit"
                  className="inline-flex justify-center rounded-xl bg-sky-700 px-8 py-3 text-sm font-medium text-white transition hover:opacity-90"
                >
                  予約内容を確認する
                </button>
              </div>
            </form>

            <aside className="space-y-4">
              <div className="overflow-hidden rounded-2xl border border-stone-200 bg-white">
                <img
                  src={getImageSrc(room.imageName)}
                  alt={room.name}
                  className="h-48 w-full object-cover"
                />

                <div className="p-4">
                  <h2 className="text-base font-semibold text-slate-800">
                    {room.name}
                  </h2>
                  <p className="mt-2 text-sm leading-6 text-slate-500">
                    {room.address}
                  </p>
                  <p className="mt-3 text-sm font-semibold text-slate-800">
                    {formatYenPerHour(room.price)}
                  </p>
                </div>
              </div>

              <div className="rounded-2xl border border-stone-200 bg-white p-4">
                <h2 className="text-base font-semibold text-slate-800">
                  営業時間
                </h2>
                <div className="mt-3">
                  <BusinessHoursSummary rows={room.businessHours} />
                </div>
              </div>
            </aside>
          </div>
        </section>
      </div>
    </main>
  );
}

function InputField({
  label,
  type,
  value,
  onChange,
  step,
  min,
}: {
  label: string;
  type: "date" | "time";
  value: string;
  onChange: (value: string) => void;
  step?: string;
  min?: string;
}) {
  return (
    <div>
      <label className="mb-2 block text-sm font-medium text-slate-700">
        {label}
      </label>
      <input
        type={type}
        value={value}
        step={step}
        min={min}
        onChange={(e) => onChange(e.target.value)}
        className="w-full rounded-xl border border-stone-300 px-4 py-3 text-sm outline-none transition focus:border-sky-500"
      />
    </div>
  );
}