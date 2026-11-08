import {
  getReservationStatusClassName,
  getReservationStatusLabel,
} from "./reservationStatus";

describe("getReservationStatusLabel", () => {
  test("決済済みは利用済みかどうかで表示を分ける", () => {
    expect(getReservationStatusLabel("paid", false)).toBe("決済済み");
    expect(getReservationStatusLabel("paid", true)).toBe("利用済み");
  });

  test("予約済み・キャンセルは isUsed に関係なく同じ表示", () => {
    expect(getReservationStatusLabel("booked")).toBe("予約済み");
    expect(getReservationStatusLabel("booked", true)).toBe("予約済み");
    expect(getReservationStatusLabel("canceled", true)).toBe("キャンセル済み");
  });

  test("不明な値はそのまま、空は - を返す", () => {
    expect(getReservationStatusLabel("unknown")).toBe("unknown");
    expect(getReservationStatusLabel("")).toBe("-");
  });
});

describe("getReservationStatusClassName", () => {
  test("決済済み（利用前）と利用済みで色を分ける", () => {
    expect(getReservationStatusClassName("paid", false)).toContain("sky");
    expect(getReservationStatusClassName("paid", true)).toContain("emerald");
    expect(getReservationStatusClassName("canceled")).toContain("rose");
  });
});
