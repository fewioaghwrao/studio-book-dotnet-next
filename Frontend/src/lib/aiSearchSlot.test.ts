import { buildReservationConfirmHref, formatSlotLabel } from "./aiSearchSlot";

const slot = {
  startAt: "2026-11-14T18:00:00",
  endAt: "2026-11-14T20:00:00",
  durationAssumed: false,
};

describe("formatSlotLabel", () => {
  test("日付・曜日・時刻を表示する", () => {
    expect(formatSlotLabel(slot)).toBe("11/14（土）18:00〜20:00");
  });

  test("読み取れない値は - を返す", () => {
    expect(formatSlotLabel({ ...slot, startAt: "invalid" })).toBe("-");
  });
});

describe("buildReservationConfirmHref", () => {
  test("予約確認画面の URL に日時を渡す", () => {
    expect(buildReservationConfirmHref(3, slot)).toBe(
      "/rooms/3/reservations/confirm?startDate=2026-11-14&startTime=18%3A00&endDate=2026-11-14&endTime=20%3A00"
    );
  });

  test("日時が読めない場合は null", () => {
    expect(buildReservationConfirmHref(3, { ...slot, endAt: "" })).toBeNull();
  });
});
