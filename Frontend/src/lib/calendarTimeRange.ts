export type BusinessHourInput = {
  startTime: string | null;
  endTime: string | null;
  isHoliday: boolean;
};

export type CalendarEventInput = {
  start: string;
  end: string;
  allDay: boolean;
};

export type CalendarTimeRange = {
  slotMinTime: string;
  slotMaxTime: string;
};

type Options = {
  /** 営業時間の前後に余白として表示する時間数 */
  paddingHours?: number;
  /** 営業時間・予定がまったくない場合の表示範囲（時） */
  fallback?: { startHour: number; endHour: number };
};

const MINUTES_PER_DAY = 24 * 60;
const FULL_DAY: CalendarTimeRange = {
  slotMinTime: "00:00:00",
  slotMaxTime: "24:00:00",
};

/** "09:30" / "09:30:00" を 0:00 からの分に変換する。不正な値は null。 */
export function parseTimeToMinutes(value: string | null | undefined): number | null {
  if (!value) return null;

  const match = /^(\d{1,2}):(\d{2})(?::\d{2})?$/.exec(value.trim());
  if (!match) return null;

  const hours = Number(match[1]);
  const minutes = Number(match[2]);

  if (hours > 24 || minutes > 59 || (hours === 24 && minutes > 0)) {
    return null;
  }

  return hours * 60 + minutes;
}

function toSlotTime(hour: number) {
  return `${String(hour).padStart(2, "0")}:00:00`;
}

function sameLocalDate(a: Date, b: Date) {
  return (
    a.getFullYear() === b.getFullYear() &&
    a.getMonth() === b.getMonth() &&
    a.getDate() === b.getDate()
  );
}

/**
 * 週・日表示カレンダーの縦方向の表示範囲を、営業時間と予定から求める。
 *
 * - 営業日の最も早い開始〜最も遅い終了に、前後の余白（既定 1 時間）を付ける
 * - 予約などの時間指定の予定は必ず範囲内に収める（予定が隠れないようにする）
 * - 日付をまたぐ営業時間・予定がある場合は 0:00〜24:00 をそのまま表示する
 */
export function getCalendarTimeRange(
  businessHours: BusinessHourInput[],
  events: CalendarEventInput[],
  { paddingHours = 1, fallback = { startHour: 8, endHour: 22 } }: Options = {}
): CalendarTimeRange {
  let minMinutes = Number.POSITIVE_INFINITY;
  let maxMinutes = Number.NEGATIVE_INFINITY;

  for (const row of businessHours) {
    if (row.isHoliday) continue;

    const start = parseTimeToMinutes(row.startTime);
    let end = parseTimeToMinutes(row.endTime);

    if (start === null || end === null) continue;

    // 終了 00:00 は当日 24:00 として扱う。それ以外で開始以前に終わる場合は日付またぎ。
    if (end === 0 && start > 0) {
      end = MINUTES_PER_DAY;
    } else if (end <= start) {
      return FULL_DAY;
    }

    minMinutes = Math.min(minMinutes, start);
    maxMinutes = Math.max(maxMinutes, end);
  }

  for (const event of events) {
    if (event.allDay) continue;

    const start = new Date(event.start);
    const end = new Date(event.end);

    if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) continue;

    const startMinutes = start.getHours() * 60 + start.getMinutes();
    let endMinutes = end.getHours() * 60 + end.getMinutes();

    if (!sameLocalDate(start, end)) {
      const endsAtNextMidnight =
        endMinutes === 0 && end.getTime() - start.getTime() <= MINUTES_PER_DAY * 60 * 1000;

      if (!endsAtNextMidnight) {
        return FULL_DAY;
      }

      endMinutes = MINUTES_PER_DAY;
    }

    minMinutes = Math.min(minMinutes, startMinutes);
    maxMinutes = Math.max(maxMinutes, endMinutes);
  }

  if (!Number.isFinite(minMinutes) || !Number.isFinite(maxMinutes)) {
    return {
      slotMinTime: toSlotTime(fallback.startHour),
      slotMaxTime: toSlotTime(fallback.endHour),
    };
  }

  const startHour = Math.max(0, Math.floor(minMinutes / 60) - paddingHours);
  const endHour = Math.min(24, Math.ceil(maxMinutes / 60) + paddingHours);

  return {
    slotMinTime: toSlotTime(startHour),
    slotMaxTime: toSlotTime(endHour),
  };
}
