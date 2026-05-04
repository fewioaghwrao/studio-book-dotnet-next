import { render, screen, waitFor } from "@testing-library/react";
import RoomDetailPage from "./page";

const mockFetch = jest.fn();

jest.mock("next/navigation", () => ({
  useParams: () => ({
    roomId: "1",
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

type MockRoomDetail = {
  id: number;
  name: string;
  imageName: string | null;
  description: string;
  price: number;
  capacity: number;
  postalCode: string;
  address: string;
  hostName: string;
  averageScore: number | null;
  reviewCount: number;
  businessHours: Array<{
    dayOfWeek: number;
    startTime: string | null;
    endTime: string | null;
    isHoliday: boolean;
  }>;
  priceRules: Array<{
    id: number;
    ruleType: string;
    weekday: number | null;
    startHour: string | null;
    endHour: string | null;
    multiplier: number | null;
    flatFee: number | null;
    note: string | null;
  }>;
  reviews: Array<{
    id: number;
    score: number | null;
    content: string;
    userName: string;
    hostReply: string | null;
    hostReplyAt: string | null;
    createdAtUtc: string;
  }>;
  hiddenHostReplies: Array<{
    id: number;
    score: number | null;
    content: string;
    userName: string;
    hostReply: string | null;
    hostReplyAt: string | null;
    createdAtUtc: string;
  }>;
  calendarEvents: Array<{
    id: string;
    title: string;
    start: string;
    end: string;
    allDay: boolean;
    type: "open" | "closure" | "reservation";
  }>;
};

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

function mockCurrentUserResponse(roles: string[] = ["GeneralUser"]) {
  return {
    isAuthenticated: roles.length > 0,
    user:
      roles.length > 0
        ? {
            id: 1,
            name: "一般 太郎",
            roles,
          }
        : undefined,
  };
}

function mockRoomDetailResponse(): MockRoomDetail {
  return {
    id: 1,
    name: "池袋ダンスルーム",
    imageName: "room01.jpg",
    description: "駅近でダンス練習に使いやすいスタジオです。",
    price: 3000,
    capacity: 6,
    postalCode: "170-0013",
    address: "東京都豊島区東池袋",
    hostName: "ホスト 太郎",
    averageScore: 4.5,
    reviewCount: 12,
    businessHours: [
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
    priceRules: [
      {
        id: 1,
        ruleType: "flat_fee",
        weekday: null,
        startHour: null,
        endHour: null,
        multiplier: null,
        flatFee: 2000,
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
        note: "金曜夜間料金",
      },
    ],
    reviews: [
      {
        id: 1,
        score: 5,
        content: "とても使いやすいスタジオでした。",
        userName: "一般 太郎",
        hostReply: "ご利用ありがとうございました。",
        hostReplyAt: "2026-05-02T10:00:00",
        createdAtUtc: "2026-05-01T09:00:00",
      },
      {
        id: 2,
        score: 4,
        content: "駅から近くて便利でした。",
        userName: "",
        hostReply: null,
        hostReplyAt: null,
        createdAtUtc: "2026-05-02T10:00:00",
      },
    ],
    hiddenHostReplies: [
      {
        id: 3,
        score: null,
        content: "",
        userName: "",
        hostReply: "設備点検のお知らせです。",
        hostReplyAt: "2026-05-03T10:00:00",
        createdAtUtc: "2026-05-03T09:00:00",
      },
    ],
    calendarEvents: [
      {
        id: "open-1",
        title: "営業時間",
        start: "2026-05-10T09:00:00",
        end: "2026-05-10T18:00:00",
        allDay: false,
        type: "open",
      },
      {
        id: "closure-1",
        title: "休館",
        start: "2026-05-11T00:00:00",
        end: "2026-05-12T00:00:00",
        allDay: true,
        type: "closure",
      },
      {
        id: "reservation-1",
        title: "予約済み",
        start: "2026-05-12T10:00:00",
        end: "2026-05-12T12:00:00",
        allDay: false,
        type: "reservation",
      },
    ],
  };
}

function setupFetch({
  roles = ["GeneralUser"],
  roomResponse = mockRoomDetailResponse(),
}: {
  roles?: string[];
  roomResponse?: MockRoomDetail;
} = {}) {
  mockFetch.mockImplementation((url: string) => {
    if (url.includes("/api/auth/me")) {
      return Promise.resolve({
        ok: true,
        status: 200,
        json: async () => mockCurrentUserResponse(roles),
      });
    }

    if (url.includes("/api/rooms/1")) {
      return Promise.resolve({
        ok: true,
        status: 200,
        json: async () => roomResponse,
      });
    }

    return Promise.reject(new Error(`Unhandled fetch: ${url}`));
  });
}

function setupRoomError(status: number) {
  mockFetch.mockImplementation((url: string) => {
    if (url.includes("/api/auth/me")) {
      return Promise.resolve({
        ok: true,
        status: 200,
        json: async () => mockCurrentUserResponse(["GeneralUser"]),
      });
    }

    if (url.includes("/api/rooms/1")) {
      return Promise.resolve({
        ok: false,
        status,
      });
    }

    return Promise.reject(new Error(`Unhandled fetch: ${url}`));
  });
}

async function waitForLoaded() {
  await waitFor(() => {
    expect(
      screen.getByRole("heading", { name: "池袋ダンスルーム" })
    ).toBeInTheDocument();
  });
}

describe("RoomDetailPage", () => {
  test("読み込み中メッセージを表示する", () => {
    setupFetch();

    render(<RoomDetailPage />);

    expect(
      screen.getByText("スタジオ詳細を読み込み中...")
    ).toBeInTheDocument();
  });

  test("API成功時、スタジオ詳細を表示する", async () => {
    setupFetch();

    render(<RoomDetailPage />);

    await waitForLoaded();

    expect(screen.getByText("ROOM DETAIL")).toBeInTheDocument();

    expect(
      screen.getByText(
        "スタジオ情報、営業時間、料金ルール、空き状況、レビューを確認できます。"
      )
    ).toBeInTheDocument();

    expect(screen.getByAltText("池袋ダンスルーム")).toHaveAttribute(
      "src",
      "/storage/room01.jpg"
    );

    expect(screen.getByText("スタジオ名")).toBeInTheDocument();
    expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);

    expect(screen.getByText("説明")).toBeInTheDocument();
    expect(
      screen.getByText("駅近でダンス練習に使いやすいスタジオです。")
    ).toBeInTheDocument();

    expect(screen.getByText("基本料金")).toBeInTheDocument();
    expect(screen.getByText("3,000円 / h")).toBeInTheDocument();

    expect(screen.getByText("定員")).toBeInTheDocument();
    expect(screen.getByText("6人")).toBeInTheDocument();

    expect(screen.getByText("郵便番号")).toBeInTheDocument();
    expect(screen.getByText("〒170-0013")).toBeInTheDocument();

    expect(screen.getByText("住所")).toBeInTheDocument();
    expect(screen.getByText("東京都豊島区東池袋")).toBeInTheDocument();

expect(screen.getAllByText("スタジオ提供者").length).toBeGreaterThan(0);
expect(screen.getByText("ホスト 太郎")).toBeInTheDocument();
  });

  test("画像がない場合、noImageを表示する", async () => {
    setupFetch({
      roomResponse: {
        ...mockRoomDetailResponse(),
        imageName: null,
      },
    });

    render(<RoomDetailPage />);

    await waitForLoaded();

    expect(screen.getByAltText("池袋ダンスルーム")).toHaveAttribute(
      "src",
      "/images/noImage.png"
    );
  });

  test("営業時間を表示する", async () => {
    setupFetch();

    render(<RoomDetailPage />);

    await waitForLoaded();

  expect(screen.getAllByText("営業時間").length).toBeGreaterThan(0);

    expect(screen.getAllByText("月曜").length).toBeGreaterThan(0);
    expect(screen.getAllByText("火曜").length).toBeGreaterThan(0);
    expect(screen.getAllByText("日曜").length).toBeGreaterThan(0);

    expect(screen.getAllByText("09:00").length).toBeGreaterThan(0);
    expect(screen.getAllByText("18:00").length).toBeGreaterThan(0);
    expect(screen.getAllByText("10:00").length).toBeGreaterThan(0);
    expect(screen.getAllByText("19:00").length).toBeGreaterThan(0);

    expect(screen.getAllByText("休み").length).toBeGreaterThan(0);
  });

  test("営業時間が未設定の場合、未設定メッセージを表示する", async () => {
    setupFetch({
      roomResponse: {
        ...mockRoomDetailResponse(),
        businessHours: [],
      },
    });

    render(<RoomDetailPage />);

    await waitForLoaded();

    expect(screen.getByText("営業時間の設定がありません。")).toBeInTheDocument();
  });

  test("料金ルールを表示する", async () => {
    setupFetch();

    render(<RoomDetailPage />);

    await waitForLoaded();

    expect(screen.getByText("料金ルール")).toBeInTheDocument();

    expect(screen.getByText("固定費")).toBeInTheDocument();
    expect(screen.getByText(/全て：2,000円/)).toBeInTheDocument();
    expect(screen.getByText(/清掃費/)).toBeInTheDocument();

    expect(screen.getByText("加算料金")).toBeInTheDocument();
    expect(screen.getByText(/金曜 18:00〜22:00/)).toBeInTheDocument();
    expect(screen.getByText(/1.5倍/)).toBeInTheDocument();
    expect(screen.getByText(/4,500円 \/ h/)).toBeInTheDocument();
    expect(screen.getByText(/金曜夜間料金/)).toBeInTheDocument();
  });

  test("料金ルールが未設定の場合、未設定を表示する", async () => {
    setupFetch({
      roomResponse: {
        ...mockRoomDetailResponse(),
        priceRules: [],
      },
    });

    render(<RoomDetailPage />);

    await waitForLoaded();

    expect(screen.getAllByText("未設定").length).toBeGreaterThanOrEqual(2);
  });

  test("レビューとホスト返信を表示する", async () => {
    setupFetch();

    render(<RoomDetailPage />);

    await waitForLoaded();

    expect(screen.getByText("この部屋のレビュー")).toBeInTheDocument();

    expect(
      screen.getByText("とても使いやすいスタジオでした。")
    ).toBeInTheDocument();

    expect(screen.getByText("駅から近くて便利でした。")).toBeInTheDocument();

    expect(screen.getByText("投稿者：一般 太郎")).toBeInTheDocument();
    expect(screen.getByText("投稿者：ユーザー")).toBeInTheDocument();

    expect(screen.getByText("ご利用ありがとうございました。")).toBeInTheDocument();
    expect(screen.getAllByText("スタジオ提供者").length).toBeGreaterThan(0);
  });

  test("レビューが0件の場合、空メッセージを表示する", async () => {
    setupFetch({
      roomResponse: {
        ...mockRoomDetailResponse(),
        reviews: [],
        hiddenHostReplies: [],
        averageScore: null,
        reviewCount: 0,
      },
    });

    render(<RoomDetailPage />);

    await waitForLoaded();

    expect(screen.getByText("まだレビューはありません。")).toBeInTheDocument();
    expect(screen.getAllByText("レビューなし").length).toBeGreaterThan(0);
  });

  test("非公開レビューへのホスト返信を表示する", async () => {
    setupFetch();

    render(<RoomDetailPage />);

    await waitForLoaded();

    expect(screen.getByText("ホストからのお知らせ")).toBeInTheDocument();
    expect(screen.getByText("非公開レビュー")).toBeInTheDocument();
    expect(screen.getByText("設備点検のお知らせです。")).toBeInTheDocument();
  });

  test("未ログイン時、ログインと会員登録リンクを表示する", async () => {
    setupFetch({
      roles: [],
    });

    render(<RoomDetailPage />);

    await waitForLoaded();

    expect(
      screen.getByText("予約するには一般会員としてログインしてください。")
    ).toBeInTheDocument();

    expect(
      screen.getByRole("link", { name: "ログインして予約する" })
    ).toHaveAttribute("href", "/auth/login");

    expect(screen.getByRole("link", { name: "会員登録する" })).toHaveAttribute(
      "href",
      "/signup"
    );
  });

  test("一般会員ログイン時、予約リンクを表示する", async () => {
    setupFetch({
      roles: ["GeneralUser"],
    });

    render(<RoomDetailPage />);

    await waitForLoaded();

    expect(
      screen.getByText("営業時間・空き状況を確認し、予約入力へ進んでください。")
    ).toBeInTheDocument();

    expect(
      screen.getByRole("link", { name: "予約する（確認へ）" })
    ).toHaveAttribute("href", "/rooms/1/reservations/input");
  });

  test("ホストログイン時、予約不可メッセージを表示する", async () => {
    setupFetch({
      roles: ["Host"],
    });

    render(<RoomDetailPage />);

    await waitForLoaded();

    expect(
      screen.getByText(
        "予約は一般会員のみ可能です。ホスト・管理者アカウントでは予約できません。"
      )
    ).toBeInTheDocument();

    expect(screen.getByRole("button", { name: "予約する" })).toBeDisabled();
  });

  test("管理者ログイン時、予約不可メッセージを表示する", async () => {
    setupFetch({
      roles: ["Admin"],
    });

    render(<RoomDetailPage />);

    await waitForLoaded();

    expect(
      screen.getByText(
        "予約は一般会員のみ可能です。ホスト・管理者アカウントでは予約できません。"
      )
    ).toBeInTheDocument();

    expect(screen.getByRole("button", { name: "予約する" })).toBeDisabled();
  });

  test("カレンダーを表示する", async () => {
    setupFetch();

    render(<RoomDetailPage />);

    await waitForLoaded();

    expect(screen.getByText("カレンダー")).toBeInTheDocument();
    expect(
      screen.getByText("営業時間・休館・予約済みの予定を確認できます。")
    ).toBeInTheDocument();

    expect(screen.getAllByText("営業時間").length).toBeGreaterThan(0);
    expect(screen.getByText("休業・休館")).toBeInTheDocument();
    expect(screen.getAllByText("予約済み").length).toBeGreaterThan(0);

    expect(screen.getByTestId("full-calendar")).toBeInTheDocument();
    expect(screen.getByText("initialView: timeGridWeek")).toBeInTheDocument();
    expect(screen.getByText("events: 3")).toBeInTheDocument();

    expect(screen.getAllByText("休館").length).toBeGreaterThan(0);
  });

  test("スマホ幅の場合、カレンダー初期表示が日表示になる", async () => {
    Object.defineProperty(window, "innerWidth", {
      writable: true,
      configurable: true,
      value: 375,
    });

    setupFetch();

    render(<RoomDetailPage />);

    await waitForLoaded();

    expect(screen.getByText("initialView: timeGridDay")).toBeInTheDocument();
  });

  test("404の場合、スタジオが見つからないメッセージを表示する", async () => {
    setupRoomError(404);

    render(<RoomDetailPage />);

    await waitFor(() => {
      expect(screen.getByText("スタジオが見つかりません。")).toBeInTheDocument();
    });

    expect(
      screen.getByRole("link", { name: "スタジオ一覧へ戻る" })
    ).toHaveAttribute("href", "/rooms");
  });

  test("API失敗時、取得失敗メッセージを表示する", async () => {
    setupRoomError(500);

    render(<RoomDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByText("スタジオ詳細の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    mockFetch.mockImplementation((url: string) => {
      if (url.includes("/api/auth/me")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: async () => mockCurrentUserResponse(["GeneralUser"]),
        });
      }

      if (url.includes("/api/rooms/1")) {
        return Promise.reject(new Error("Network Error"));
      }

      return Promise.reject(new Error(`Unhandled fetch: ${url}`));
    });

    render(<RoomDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("スタジオ一覧へ戻るリンクを表示する", async () => {
    setupFetch();

    render(<RoomDetailPage />);

    await waitForLoaded();

    expect(
      screen.getByRole("link", { name: "スタジオ一覧へ戻る" })
    ).toHaveAttribute("href", "/rooms");
  });

  test("API呼び出しURLが正しい", async () => {
    setupFetch();

    render(<RoomDetailPage />);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/auth/me",
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      })
    );

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/rooms/1",
      expect.objectContaining({
        method: "GET",
        cache: "no-store",
      })
    );
  });
});