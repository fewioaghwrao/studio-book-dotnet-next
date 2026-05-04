import { render, screen, waitFor } from "@testing-library/react";
import HostRoomDetailPage from "./page";

const mockFetch = jest.fn();

jest.mock("next/navigation", () => ({
  useParams: () => ({
    id: "1",
  }),
}));

jest.mock("@fullcalendar/react", () => {
  return function MockFullCalendar(props: {
    initialView?: string;
    events?: Array<{
      id: string;
      title: string;
      start: string;
      end: string;
      allDay: boolean;
    }>;
  }) {
    return (
      <div data-testid="full-calendar">
        <div>initialView: {props.initialView}</div>
        <div>events: {props.events?.length ?? 0}</div>
        {props.events?.map((event) => (
          <div key={event.id}>{event.title}</div>
        ))}
      </div>
    );
  };
});

jest.mock("@fullcalendar/daygrid", () => ({}));
jest.mock("@fullcalendar/timegrid", () => ({}));
jest.mock("@fullcalendar/interaction", () => ({}));
jest.mock("@fullcalendar/core/locales/ja", () => ({}));

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;

  Object.defineProperty(window, "innerWidth", {
    writable: true,
    configurable: true,
    value: 1024,
  });
});

function mockRoomResponse() {
  return {
    id: 1,
    name: "池袋ダンスルーム",
    imageName: "room01.jpg",
    description: "駅近のダンス向けスタジオです。",
    price: 3000,
    capacity: 6,
    postalCode: "170-0013",
    address: "東京都豊島区東池袋",
  };
}

function mockBusinessHoursResponse() {
  return {
    roomId: 1,
    roomName: "池袋ダンスルーム",
    rows: [
      {
        dayOfWeek: 1,
        startTime: "09:00:00",
        endTime: "18:00:00",
        isHoliday: false,
      },
      {
        dayOfWeek: 2,
        startTime: "10:00:00",
        endTime: "19:00:00",
        isHoliday: false,
      },
      {
        dayOfWeek: 7,
        startTime: null,
        endTime: null,
        isHoliday: true,
      },
    ],
  };
}

function mockPriceRulesResponse() {
  return {
    roomId: 1,
    roomName: "池袋ダンスルーム",
    rules: [
      {
        id: 1,
        ruleType: "flat_fee",
        weekday: null,
        startHour: null,
        endHour: null,
        multiplier: null,
        flatFee: 1000,
        note: "清掃費",
      },
      {
        id: 2,
        ruleType: "multiplier",
        weekday: 5,
        startHour: "18:00:00",
        endHour: "22:00:00",
        multiplier: 1.5,
        flatFee: null,
        note: "夜間料金",
      },
    ],
  };
}

function mockEventsResponse() {
  return [
    {
      id: 1,
      title: "休館日",
      start: "2026-05-10",
      end: "2026-05-11",
      allDay: true,
    },
  ];
}

function setupSuccessResponses() {
  mockFetch
    .mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockRoomResponse(),
    })
    .mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockBusinessHoursResponse(),
    })
    .mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockPriceRulesResponse(),
    })
    .mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockEventsResponse(),
    });
}

describe("HostRoomDetailPage", () => {
  test("読み込み中メッセージを表示する", () => {
    setupSuccessResponses();

    render(<HostRoomDetailPage />);

    expect(
      screen.getByText("スタジオ詳細を読み込み中...")
    ).toBeInTheDocument();
  });

  test("API成功時、スタジオ詳細を表示する", async () => {
    setupSuccessResponses();

    render(<HostRoomDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "池袋ダンスルーム" })
      ).toBeInTheDocument();
    });

    expect(screen.getByText("ROOM DETAIL")).toBeInTheDocument();

    expect(
      screen.getByText("スタジオ情報、営業時間、料金ルール、休館予定を確認できます。")
    ).toBeInTheDocument();

    expect(screen.getByAltText("池袋ダンスルーム")).toHaveAttribute(
      "src",
      "/storage/room01.jpg"
    );

    expect(screen.getByText("ID")).toBeInTheDocument();
    expect(screen.getByText("スタジオ名")).toBeInTheDocument();
    expect(screen.getByText("説明")).toBeInTheDocument();
    expect(screen.getByText("基本料金")).toBeInTheDocument();
    expect(screen.getByText("固定費")).toBeInTheDocument();
    expect(screen.getByText("加算料金")).toBeInTheDocument();
    expect(screen.getByText("最大定員")).toBeInTheDocument();
    expect(screen.getByText("郵便番号")).toBeInTheDocument();
    expect(screen.getByText("住所")).toBeInTheDocument();

    expect(screen.getAllByText("1").length).toBeGreaterThan(0);
    expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    expect(
      screen.getByText("駅近のダンス向けスタジオです。")
    ).toBeInTheDocument();
    expect(screen.getByText("3,000円")).toBeInTheDocument();
    expect(screen.getByText("6人")).toBeInTheDocument();
    expect(screen.getByText("170-0013")).toBeInTheDocument();
    expect(screen.getByText("東京都豊島区東池袋")).toBeInTheDocument();
  });

  test("営業時間を表示する", async () => {
    setupSuccessResponses();

    render(<HostRoomDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "池袋ダンスルーム" })
      ).toBeInTheDocument();
    });

    expect(screen.getByText("営業時間")).toBeInTheDocument();

    expect(screen.getAllByText("月曜").length).toBeGreaterThan(0);
    expect(screen.getAllByText("火曜").length).toBeGreaterThan(0);
    expect(screen.getAllByText("日曜").length).toBeGreaterThan(0);

    expect(screen.getAllByText("09:00〜18:00").length).toBeGreaterThan(0);
    expect(screen.getAllByText("10:00〜19:00").length).toBeGreaterThan(0);
    expect(screen.getAllByText("休み").length).toBeGreaterThan(0);
  });

  test("料金ルールを表示する", async () => {
    setupSuccessResponses();

    render(<HostRoomDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "池袋ダンスルーム" })
      ).toBeInTheDocument();
    });

    expect(screen.getByText(/全て：1,000円/)).toBeInTheDocument();
    expect(screen.getByText(/清掃費/)).toBeInTheDocument();

    expect(screen.getByText(/金曜 18:00〜22:00：1.5倍/)).toBeInTheDocument();
    expect(screen.getByText(/夜間料金/)).toBeInTheDocument();
  });

  test("休館カレンダーを表示する", async () => {
    setupSuccessResponses();

    render(<HostRoomDetailPage />);

    await waitFor(() => {
      expect(screen.getByTestId("full-calendar")).toBeInTheDocument();
    });

    expect(screen.getByText("休館カレンダー")).toBeInTheDocument();
    expect(
      screen.getByText("登録済みの休館日を確認できます。")
    ).toBeInTheDocument();

    expect(screen.getByText("initialView: dayGridMonth")).toBeInTheDocument();
    expect(screen.getByText("events: 1")).toBeInTheDocument();
    expect(screen.getByText("休館日")).toBeInTheDocument();
  });

  test("画像名が空の場合、noImageを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          ...mockRoomResponse(),
          imageName: "",
        }),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockBusinessHoursResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockPriceRulesResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockEventsResponse(),
      });

    render(<HostRoomDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "池袋ダンスルーム" })
      ).toBeInTheDocument();
    });

    expect(screen.getByAltText("池袋ダンスルーム")).toHaveAttribute(
      "src",
      "/images/noImage.png"
    );
  });

  test("営業時間と料金ルールが未設定の場合、未設定を表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockRoomResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          roomId: 1,
          roomName: "池袋ダンスルーム",
          rows: [],
        }),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          roomId: 1,
          roomName: "池袋ダンスルーム",
          rules: [],
        }),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => [],
      });

    render(<HostRoomDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "池袋ダンスルーム" })
      ).toBeInTheDocument();
    });

    expect(screen.getAllByText("未設定").length).toBeGreaterThan(0);
  });

  test("403の場合、ホストユーザーのみアクセス可能メッセージを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: false,
        status: 403,
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockBusinessHoursResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockPriceRulesResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockEventsResponse(),
      });

    render(<HostRoomDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByText("ホストユーザーのみアクセスできます。")
      ).toBeInTheDocument();
    });

    expect(screen.getByRole("link", { name: "スタジオ一覧へ戻る" }))
      .toHaveAttribute("href", "/host/rooms");
  });

  test("404の場合、スタジオが見つからないメッセージを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: false,
        status: 404,
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockBusinessHoursResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockPriceRulesResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockEventsResponse(),
      });

    render(<HostRoomDetailPage />);

    await waitFor(() => {
      expect(screen.getByText("スタジオが見つかりません。")).toBeInTheDocument();
    });

    expect(screen.getByRole("link", { name: "スタジオ一覧へ戻る" }))
      .toHaveAttribute("href", "/host/rooms");
  });

  test("API失敗時、取得失敗メッセージを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: false,
        status: 500,
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockBusinessHoursResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockPriceRulesResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockEventsResponse(),
      });

    render(<HostRoomDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByText("スタジオ詳細の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<HostRoomDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("各API呼び出しURLが正しい", async () => {
    setupSuccessResponses();

    render(<HostRoomDetailPage />);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(4);
    });

    expect(mockFetch.mock.calls[0][0]).toBe(
      "https://localhost:7226/api/host/rooms/1"
    );

    expect(mockFetch.mock.calls[1][0]).toBe(
      "https://localhost:7226/api/host/rooms/1/business-hours"
    );

    expect(mockFetch.mock.calls[2][0]).toBe(
      "https://localhost:7226/api/host/rooms/1/price-rules"
    );

    expect(mockFetch.mock.calls[3][0]).toBe(
      "https://localhost:7226/api/host/rooms/1/closures/events"
    );

    for (const call of mockFetch.mock.calls) {
      expect(call[1]).toEqual(
        expect.objectContaining({
          method: "GET",
          credentials: "include",
          cache: "no-store",
        })
      );
    }
  });

  test("設定リンクの遷移先が正しい", async () => {
    setupSuccessResponses();

    render(<HostRoomDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "池袋ダンスルーム" })
      ).toBeInTheDocument();
    });

    expect(screen.getByRole("link", { name: "休館日設定" }))
      .toHaveAttribute("href", "/host/rooms/1/closures");

    expect(screen.getByRole("link", { name: "営業時間設定" }))
      .toHaveAttribute("href", "/host/rooms/1/business-hours");

    expect(screen.getByRole("link", { name: "料金ルール設定" }))
      .toHaveAttribute("href", "/host/rooms/1/price-rules");

    expect(screen.getByRole("link", { name: "一覧へ戻る" }))
      .toHaveAttribute("href", "/host/rooms");
  });
});