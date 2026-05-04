import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import ReservationInputPage from "./page";

const mockFetch = jest.fn();
const mockPush = jest.fn();

jest.mock("next/navigation", () => ({
  useParams: () => ({
    roomId: "1",
  }),
  useRouter: () => ({
    push: mockPush,
  }),
}));

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();
  mockPush.mockReset();

  global.fetch = mockFetch as jest.Mock;

  jest.useFakeTimers();

  // 2026-05-04 09:00 を「実行環境のローカル時刻」として固定する
  // new Date("2026-05-04T09:00:00+09:00") だと CI(UTC) で判定がズレる
  jest.setSystemTime(new Date(2026, 4, 4, 9, 0, 0));
});

afterEach(() => {
  jest.useRealTimers();
});

function mockCurrentUserResponse() {
  return {
    isAuthenticated: true,
    user: {
      id: 1,
      name: "一般 太郎",
      roles: ["GeneralUser"],
    },
  };
}

function mockRoomResponse() {
  return {
    id: 1,
    name: "池袋ダンスルーム",
    imageName: "room01.jpg",
    description: "駅近で使いやすいダンススタジオです。",
    price: 3000,
    capacity: 6,
    postalCode: "170-0013",
    address: "東京都豊島区東池袋",
    hostName: "ホスト 太郎",
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
  };
}

function setupSuccessInitialRender() {
  mockFetch
    .mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockCurrentUserResponse(),
    })
    .mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockRoomResponse(),
    });

  render(<ReservationInputPage />);
}

async function waitForLoaded() {
  await waitFor(() => {
    expect(
      screen.getByRole("heading", { name: "予約入力" })
    ).toBeInTheDocument();
  });
}

describe("ReservationInputPage", () => {
  test("読み込み中メッセージを表示する", () => {
    setupSuccessInitialRender();

    expect(
      screen.getByText("予約入力画面を読み込み中...")
    ).toBeInTheDocument();
  });

  test("API成功時、予約入力画面とスタジオ情報を表示する", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByText("RESERVATION INPUT")).toBeInTheDocument();

    expect(
      screen.getByText("利用日時を入力して、予約内容確認へ進みます。")
    ).toBeInTheDocument();

    expect(screen.getByText("池袋ダンスルーム")).toBeInTheDocument();
    expect(screen.getByText("東京都豊島区東池袋")).toBeInTheDocument();
    expect(screen.getByText("3,000円 / h")).toBeInTheDocument();

    expect(screen.getByAltText("池袋ダンスルーム")).toHaveAttribute(
      "src",
      "/storage/room01.jpg"
    );

    expect(screen.getByText("本スタジオは営業時間内のご利用のみ可能です。日をまたぐ連続利用をご希望の場合は、1日ごとに予約してください。"))
      .toBeInTheDocument();

    expect(screen.getByText("営業時間")).toBeInTheDocument();
    expect(screen.getByText("月曜")).toBeInTheDocument();
    expect(screen.getByText("09:00〜18:00")).toBeInTheDocument();
    expect(screen.getByText("火曜")).toBeInTheDocument();
    expect(screen.getByText("10:00〜19:00")).toBeInTheDocument();
    expect(screen.getByText("日曜")).toBeInTheDocument();
    expect(screen.getByText("休み")).toBeInTheDocument();
  });

  test("画像がない場合、noImageを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockCurrentUserResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          ...mockRoomResponse(),
          imageName: null,
        }),
      });

    render(<ReservationInputPage />);

    await waitForLoaded();

    expect(screen.getByAltText("池袋ダンスルーム")).toHaveAttribute(
      "src",
      "/images/noImage.png"
    );
  });

  test("営業時間が未設定の場合、未設定メッセージを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockCurrentUserResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          ...mockRoomResponse(),
          businessHours: [],
        }),
      });

    render(<ReservationInputPage />);

    await waitForLoaded();

    expect(screen.getByText("営業時間の設定がありません。")).toBeInTheDocument();
  });

  test("ログイン情報取得失敗時、エラーメッセージを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: false,
        status: 500,
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockRoomResponse(),
      });

    render(<ReservationInputPage />);

    await waitFor(() => {
      expect(
        screen.getByText("ログイン情報の取得に失敗しました。")
      ).toBeInTheDocument();
    });

    expect(screen.getByRole("link", { name: "スタジオ詳細へ戻る" }))
      .toHaveAttribute("href", "/rooms/1");
  });

  test("一般会員でない場合、権限エラーを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          isAuthenticated: true,
          user: {
            id: 2,
            name: "ホスト 太郎",
            roles: ["Host"],
          },
        }),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockRoomResponse(),
      });

    render(<ReservationInputPage />);

    await waitFor(() => {
      expect(screen.getByText("予約は一般会員のみ可能です。"))
        .toBeInTheDocument();
    });
  });

  test("スタジオが見つからない場合、エラーメッセージを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockCurrentUserResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 404,
      });

    render(<ReservationInputPage />);

    await waitFor(() => {
      expect(screen.getByText("スタジオが見つかりません。"))
        .toBeInTheDocument();
    });
  });

  test("スタジオ情報取得失敗時、エラーメッセージを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockCurrentUserResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 500,
      });

    render(<ReservationInputPage />);

    await waitFor(() => {
      expect(
        screen.getByText("スタジオ情報の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<ReservationInputPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("開始日未入力の場合、バリデーションエラーを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    setupSuccessInitialRender();

    await waitForLoaded();

    await user.click(screen.getByRole("button", { name: "予約内容を確認する" }));

    expect(screen.getByText("開始日を入力してください。")).toBeInTheDocument();
    expect(mockPush).not.toHaveBeenCalled();
  });

  test("開始時刻未入力の場合、バリデーションエラーを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    setupSuccessInitialRender();

    await waitForLoaded();

    await user.type(screen.getByLabelText("開始日"), "2026-05-10");

    await user.click(screen.getByRole("button", { name: "予約内容を確認する" }));

    expect(screen.getByText("開始時刻を入力してください。")).toBeInTheDocument();
    expect(mockPush).not.toHaveBeenCalled();
  });

  test("終了日未入力の場合、バリデーションエラーを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    setupSuccessInitialRender();

    await waitForLoaded();

    await user.type(screen.getByLabelText("開始日"), "2026-05-10");
    await user.type(screen.getByLabelText("開始時刻"), "10:00");

    await user.click(screen.getByRole("button", { name: "予約内容を確認する" }));

    expect(screen.getByText("終了日を入力してください。")).toBeInTheDocument();
    expect(mockPush).not.toHaveBeenCalled();
  });

  test("終了時刻未入力の場合、バリデーションエラーを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    setupSuccessInitialRender();

    await waitForLoaded();

    await user.type(screen.getByLabelText("開始日"), "2026-05-10");
    await user.type(screen.getByLabelText("開始時刻"), "10:00");
    await user.type(screen.getByLabelText("終了日"), "2026-05-10");

    await user.click(screen.getByRole("button", { name: "予約内容を確認する" }));

    expect(screen.getByText("終了時刻を入力してください。")).toBeInTheDocument();
    expect(mockPush).not.toHaveBeenCalled();
  });

  test("開始日時が現在時刻より前の場合、バリデーションエラーを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    setupSuccessInitialRender();

    await waitForLoaded();

    await user.type(screen.getByLabelText("開始日"), "2026-05-04");
    await user.type(screen.getByLabelText("開始時刻"), "08:00");
    await user.type(screen.getByLabelText("終了日"), "2026-05-04");
    await user.type(screen.getByLabelText("終了時刻"), "10:00");

    await user.click(screen.getByRole("button", { name: "予約内容を確認する" }));

    expect(
      screen.getByText("開始日時は現在時刻より後にしてください。")
    ).toBeInTheDocument();

    expect(mockPush).not.toHaveBeenCalled();
  });

  test("終了日時が開始日時以前の場合、バリデーションエラーを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    setupSuccessInitialRender();

    await waitForLoaded();

    await user.type(screen.getByLabelText("開始日"), "2026-05-10");
    await user.type(screen.getByLabelText("開始時刻"), "12:00");
    await user.type(screen.getByLabelText("終了日"), "2026-05-10");
    await user.type(screen.getByLabelText("終了時刻"), "10:00");

    await user.click(screen.getByRole("button", { name: "予約内容を確認する" }));

    expect(
      screen.getByText("終了日時は開始日時より後にしてください。")
    ).toBeInTheDocument();

    expect(mockPush).not.toHaveBeenCalled();
  });

  test("正常入力時、予約確認画面へ遷移する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    setupSuccessInitialRender();

    await waitForLoaded();

    await user.type(screen.getByLabelText("開始日"), "2026-05-10");
    await user.type(screen.getByLabelText("開始時刻"), "10:00");
    await user.type(screen.getByLabelText("終了日"), "2026-05-10");
    await user.type(screen.getByLabelText("終了時刻"), "12:00");

    await user.click(screen.getByRole("button", { name: "予約内容を確認する" }));

    expect(mockPush).toHaveBeenCalledWith(
      "/rooms/1/reservations/confirm?startDate=2026-05-10&startTime=10%3A00&endDate=2026-05-10&endTime=12%3A00"
    );
  });

  test("戻るリンクの遷移先が正しい", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByRole("link", { name: "戻る" }))
      .toHaveAttribute("href", "/rooms/1");
  });

  test("API呼び出し内容が正しい", async () => {
    setupSuccessInitialRender();

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch.mock.calls[0][0]).toBe(
      "https://localhost:7226/api/auth/me"
    );

    expect(mockFetch.mock.calls[0][1]).toEqual(
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      })
    );

    expect(mockFetch.mock.calls[1][0]).toBe(
      "https://localhost:7226/api/rooms/1"
    );

    expect(mockFetch.mock.calls[1][1]).toEqual(
      expect.objectContaining({
        method: "GET",
        cache: "no-store",
      })
    );
  });
});