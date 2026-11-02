import { getCalendarTimeRange, parseTimeToMinutes } from "./calendarTimeRange";

const open = (startTime: string | null, endTime: string | null) => ({
  startTime,
  endTime,
  isHoliday: false,
});

const event = (start: string, end: string, allDay = false) => ({
  start,
  end,
  allDay,
});

describe("parseTimeToMinutes", () => {
  test("HH:mm と HH:mm:ss を分に変換する", () => {
    expect(parseTimeToMinutes("09:30")).toBe(570);
    expect(parseTimeToMinutes("18:00:00")).toBe(1080);
    expect(parseTimeToMinutes("24:00:00")).toBe(1440);
  });

  test("不正な値は null", () => {
    expect(parseTimeToMinutes(null)).toBeNull();
    expect(parseTimeToMinutes("")).toBeNull();
    expect(parseTimeToMinutes("25:00")).toBeNull();
    expect(parseTimeToMinutes("abc")).toBeNull();
  });
});

describe("getCalendarTimeRange", () => {
  test("営業時間の最早開始〜最遅終了に前後1時間を付ける", () => {
    const range = getCalendarTimeRange(
      [open("09:00:00", "18:00:00"), open("10:00:00", "21:30:00")],
      []
    );

    expect(range).toEqual({ slotMinTime: "08:00:00", slotMaxTime: "23:00:00" });
  });

  test("休業日の行は無視する", () => {
    const range = getCalendarTimeRange(
      [open("10:00:00", "18:00:00"), { startTime: "00:00:00", endTime: "23:00:00", isHoliday: true }],
      []
    );

    expect(range).toEqual({ slotMinTime: "09:00:00", slotMaxTime: "19:00:00" });
  });

  test("営業時間外の予約も隠れないよう範囲を広げる", () => {
    const range = getCalendarTimeRange(
      [open("10:00:00", "18:00:00")],
      [event("2026-05-12T06:30:00", "2026-05-12T08:00:00")]
    );

    expect(range).toEqual({ slotMinTime: "05:00:00", slotMaxTime: "19:00:00" });
  });

  test("終日の予定（休館日など）は範囲に影響しない", () => {
    const range = getCalendarTimeRange(
      [open("10:00:00", "18:00:00")],
      [event("2026-05-11T00:00:00", "2026-05-12T00:00:00", true)]
    );

    expect(range).toEqual({ slotMinTime: "09:00:00", slotMaxTime: "19:00:00" });
  });

  test("0:00 や 24:00 を超えて広げない", () => {
    const range = getCalendarTimeRange([open("00:00:00", "24:00:00")], []);

    expect(range).toEqual({ slotMinTime: "00:00:00", slotMaxTime: "24:00:00" });
  });

  test("終了 00:00 は当日 24:00 として扱う", () => {
    const range = getCalendarTimeRange([open("18:00:00", "00:00:00")], []);

    expect(range).toEqual({ slotMinTime: "17:00:00", slotMaxTime: "24:00:00" });
  });

  test("深夜 0:00 に終わる予約は当日 24:00 まで表示する", () => {
    const range = getCalendarTimeRange(
      [open("10:00:00", "18:00:00")],
      [event("2026-05-12T22:00:00", "2026-05-13T00:00:00")]
    );

    expect(range).toEqual({ slotMinTime: "09:00:00", slotMaxTime: "24:00:00" });
  });

  test("日付をまたぐ営業時間・予約がある場合は24時間表示にする", () => {
    expect(getCalendarTimeRange([open("20:00:00", "03:00:00")], [])).toEqual({
      slotMinTime: "00:00:00",
      slotMaxTime: "24:00:00",
    });

    expect(
      getCalendarTimeRange(
        [open("10:00:00", "18:00:00")],
        [event("2026-05-12T23:00:00", "2026-05-13T02:00:00")]
      )
    ).toEqual({ slotMinTime: "00:00:00", slotMaxTime: "24:00:00" });
  });

  test("営業時間も予定もない場合は既定範囲（8:00〜22:00）", () => {
    expect(getCalendarTimeRange([], [])).toEqual({
      slotMinTime: "08:00:00",
      slotMaxTime: "22:00:00",
    });
  });
});
