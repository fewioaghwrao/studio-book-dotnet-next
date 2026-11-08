/**
 * 予約ステータスの表示（全画面共通）。
 *
 * API の status は booked / paid / canceled の 3 種類。
 * 決済済み（paid）は API の isUsed（終了日時が日本時間の現在以前か）で「決済済み」と「利用済み」に分けて表示する。
 *
 * | status   | isUsed | 表示       |
 * |----------|--------|------------|
 * | booked   | -      | 予約済み   |
 * | paid     | false  | 決済済み   |
 * | paid     | true   | 利用済み   |
 * | canceled | -      | キャンセル済み |
 */

export const RESERVATION_STATUS_FILTER_OPTIONS = [
  { value: "booked", label: "予約済み（未決済）" },
  { value: "paid", label: "決済済み・利用済み" },
  { value: "canceled", label: "キャンセル済み" },
] as const;

export function getReservationStatusLabel(status: string, isUsed = false): string {
  switch (status) {
    case "booked":
      return "予約済み";
    case "paid":
      return isUsed ? "利用済み" : "決済済み";
    case "canceled":
      return "キャンセル済み";
    default:
      return status || "-";
  }
}

/** バッジ用の配色（ring-1 と組み合わせると枠線も付く）。 */
export function getReservationStatusClassName(status: string, isUsed = false): string {
  switch (status) {
    case "booked":
      return "bg-slate-50 text-slate-700 ring-slate-200";
    case "paid":
      return isUsed
        ? "bg-emerald-50 text-emerald-700 ring-emerald-200"
        : "bg-sky-50 text-sky-700 ring-sky-200";
    case "canceled":
      return "bg-rose-50 text-rose-700 ring-rose-200";
    default:
      return "bg-stone-100 text-slate-600 ring-stone-200";
  }
}
