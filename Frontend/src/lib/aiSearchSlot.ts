/**
 * AI 検索で空き状況を判定した日時（API の requestedSlot）の表示と、予約確認画面への URL。
 * API の日時は日本時間の壁時計時刻（オフセットなし。例：2026-11-14T18:00:00）のため、
 * Date に変換せず文字列のまま日付・時刻を取り出す（ブラウザのタイムゾーンに左右されない）。
 */

export type AiRequestedSlot = {
  startAt: string;
  endAt: string;
  durationAssumed: boolean;
};

const WEEKDAYS = ["日", "月", "火", "水", "木", "金", "土"];

function splitDateTime(value: string): { date: string; time: string } | null {
  const match = /^(\d{4}-\d{2}-\d{2})T(\d{2}:\d{2})/.exec(value);
  return match ? { date: match[1], time: match[2] } : null;
}

function formatMonthDayWithWeekday(date: string): string {
  const [year, month, day] = date.split("-").map(Number);
  // 年月日だけから曜日を求める（UTC で計算し、タイムゾーンの影響を受けない）
  const weekday = WEEKDAYS[new Date(Date.UTC(year, month - 1, day)).getUTCDay()];
  return `${month}/${day}（${weekday}）`;
}

/** 例：11/14（土）18:00〜20:00 */
export function formatSlotLabel(slot: AiRequestedSlot): string {
  const start = splitDateTime(slot.startAt);
  const end = splitDateTime(slot.endAt);

  if (!start || !end) {
    return "-";
  }

  return `${formatMonthDayWithWeekday(start.date)}${start.time}〜${end.time}`;
}

/** その日時のまま予約確認画面へ進む URL。日時が読めない場合は null。 */
export function buildReservationConfirmHref(roomId: number, slot: AiRequestedSlot): string | null {
  const start = splitDateTime(slot.startAt);
  const end = splitDateTime(slot.endAt);

  if (!start || !end) {
    return null;
  }

  const query = new URLSearchParams({
    startDate: start.date,
    startTime: start.time,
    endDate: end.date,
    endTime: end.time,
  });

  return `/rooms/${roomId}/reservations/confirm?${query.toString()}`;
}
