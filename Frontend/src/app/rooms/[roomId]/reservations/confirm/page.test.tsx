import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import ReservationConfirmPage from "./page";

const mockFetch = jest.fn();
const mockBack = jest.fn();

let mockSearchParams = new URLSearchParams(
  "startDate=2026-05-10&startTime=10:00&endDate=2026-05-10&endTime=12:00"
);

jest.mock("next/navigation", () => ({
  useParams: () => ({
    roomId: "1",
  }),
  useSearchParams: () => mockSearchParams,
  useRouter: () => ({
    back: mockBack,
  }),
}));

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();
  mockBack.mockReset();

  mockSearchParams = new URLSearchParams(
    "startDate=2026-05-10&startTime=10:00&endDate=2026-05-10&endTime=12:00"
  );

  global.fetch = mockFetch as jest.Mock;
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

function mockConfirmResponse() {
  return {
    roomId: 1,
    roomName: "池袋ダンスルーム",
    startAt: "2026-05-10T10:00:00",
    endAt: "2026-05-10T12:00:00",
    hourlyPrice: 3000,
    hours: 2,
    subtotal: 6000,
    taxRatePercent: 10,
    tax: 600,
    platformFeeRatePercent: 10,
    platformFee: 600,
    amount: 7200,
    stripePublishableKey: "pk_test_xxx",
    sessionId: "cs_test_xxx",
    checkoutUrl: "https://checkout.stripe.com/test-session",
    items: [
      {
        label: "基本料金",
        amount: 6000,
        sliceStart: "2026-05-10T10:00:00",
        sliceEnd: "2026-05-10T12:00:00",
        unitRatePerHour: 3000,
      },
      {
        label: "消費税",
        amount: 600,
        sliceStart: null,
        sliceEnd: null,
        unitRatePerHour: null,
      },
      {
        label: "プラットフォーム使用料",
        amount: 600,
        sliceStart: null,
        sliceEnd: null,
        unitRatePerHour: null,
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
      json: async () => mockConfirmResponse(),
    });

  render(<ReservationConfirmPage />);
}

async function waitForLoaded() {
  await waitFor(() => {
    expect(
      screen.getByRole("heading", { name: "予約内容確認" })
    ).toBeInTheDocument();
  });
}

describe("ReservationConfirmPage", () => {
  test("読み込み中メッセージを表示する", () => {
    setupSuccessInitialRender();

    expect(screen.getByText("予約内容を確認中...")).toBeInTheDocument();
  });

  test("API成功時、予約内容確認を表示する", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByText("RESERVATION CONFIRM")).toBeInTheDocument();

    expect(
      screen.getByText("内容をご確認のうえ、決済へお進みください。")
    ).toBeInTheDocument();

    expect(screen.getByText("スタジオ")).toBeInTheDocument();
    expect(screen.getByText("池袋ダンスルーム")).toBeInTheDocument();

    expect(screen.getByText("利用開始時刻")).toBeInTheDocument();
    expect(screen.getByText("利用完了時刻")).toBeInTheDocument();

    expect(screen.getByText("料金")).toBeInTheDocument();

    expect(
      screen.getByText("3,000 円 × 2 時間 ＝ 7,200 円")
    ).toBeInTheDocument();

    expect(screen.getByText("料金内訳")).toBeInTheDocument();
    expect(screen.getByText("基本料金")).toBeInTheDocument();
    expect(screen.getByText("消費税")).toBeInTheDocument();
    expect(screen.getByText("プラットフォーム使用料")).toBeInTheDocument();

    expect(screen.getAllByText("6,000 円").length).toBeGreaterThan(0);
    expect(screen.getAllByText("600 円").length).toBeGreaterThan(0);
    expect(screen.getAllByText("7,200 円").length).toBeGreaterThan(0);

    expect(screen.getByText("小計")).toBeInTheDocument();
    expect(screen.getByText("消費税（10%）")).toBeInTheDocument();
    expect(screen.getByText("プラットフォーム使用料（10%）")).toBeInTheDocument();
    expect(screen.getByText("合計")).toBeInTheDocument();
  });

  test("料金内訳が0件の場合、空メッセージを表示する", async () => {
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
          ...mockConfirmResponse(),
          items: [],
        }),
      });

    render(<ReservationConfirmPage />);

    await waitForLoaded();

    expect(screen.getByText("料金内訳がありません。")).toBeInTheDocument();
  });

  test("予約日時パラメータ不足の場合、エラーメッセージを表示する", async () => {
    mockSearchParams = new URLSearchParams("startDate=2026-05-10");

    render(<ReservationConfirmPage />);

    await waitFor(() => {
      expect(
        screen.getByText("予約日時が正しく指定されていません。")
      ).toBeInTheDocument();
    });

    expect(mockFetch).not.toHaveBeenCalled();

    expect(screen.getByRole("link", { name: "スタジオ詳細へ戻る" }))
      .toHaveAttribute("href", "/rooms/1");
  });

  test("ログイン情報取得失敗時、エラーメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<ReservationConfirmPage />);

    await waitFor(() => {
      expect(
        screen.getByText("ログイン情報の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("一般会員でない場合、権限エラーを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
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
    });

    render(<ReservationConfirmPage />);

    await waitFor(() => {
      expect(screen.getByText("予約は一般会員のみ可能です。"))
        .toBeInTheDocument();
    });

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("予約確認APIで400の場合、APIメッセージを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockCurrentUserResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({
          message: "指定時間は予約できません。",
        }),
      });

    render(<ReservationConfirmPage />);

    await waitFor(() => {
      expect(screen.getByText("指定時間は予約できません。"))
        .toBeInTheDocument();
    });
  });

  test("予約確認APIで403の場合、権限エラーを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockCurrentUserResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 403,
      });

    render(<ReservationConfirmPage />);

    await waitFor(() => {
      expect(screen.getByText("予約は一般会員のみ可能です。"))
        .toBeInTheDocument();
    });
  });

  test("予約確認APIで404の場合、スタジオが見つからないメッセージを表示する", async () => {
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

    render(<ReservationConfirmPage />);

    await waitFor(() => {
      expect(screen.getByText("スタジオが見つかりません。"))
        .toBeInTheDocument();
    });
  });

  test("予約確認API失敗時、取得失敗メッセージを表示する", async () => {
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

    render(<ReservationConfirmPage />);

    await waitFor(() => {
      expect(
        screen.getByText("予約確認情報の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<ReservationConfirmPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

test("決済するボタン押下で決済中表示になる", async () => {
  const user = userEvent.setup();

  setupSuccessInitialRender();

  await waitForLoaded();

  await user.click(screen.getByRole("button", { name: "決済する" }));

  expect(
    screen.getByRole("button", { name: "決済画面へ移動中..." })
  ).toBeDisabled();
});

  test("checkoutUrlが空の場合、エラーメッセージを表示する", async () => {
    const user = userEvent.setup();

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
          ...mockConfirmResponse(),
          checkoutUrl: "",
        }),
      });

    render(<ReservationConfirmPage />);

    await waitForLoaded();

    await user.click(screen.getByRole("button", { name: "決済する" }));

    expect(
      screen.getByText("決済画面のURL取得に失敗しました。")
    ).toBeInTheDocument();
  });

  test("戻るリンクの遷移先が正しい", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByRole("link", { name: "戻る" }))
      .toHaveAttribute("href", "/rooms/1");
  });

  test("エラー画面の前の画面へ戻るボタンでrouter.backを呼ぶ", async () => {
    const user = userEvent.setup();

    mockSearchParams = new URLSearchParams();

    render(<ReservationConfirmPage />);

    await waitFor(() => {
      expect(
        screen.getByText("予約日時が正しく指定されていません。")
      ).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "前の画面へ戻る" }));

    expect(mockBack).toHaveBeenCalledTimes(1);
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
      "https://localhost:7226/api/reservations/confirm"
    );

    expect(mockFetch.mock.calls[1][1]).toEqual(
      expect.objectContaining({
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          roomId: 1,
          startAt: "2026-05-10T10:00:00",
          endAt: "2026-05-10T12:00:00",
        }),
      })
    );
  });
});