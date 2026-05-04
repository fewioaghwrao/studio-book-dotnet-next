import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import ReservationsPage from "./page";

const mockFetch = jest.fn();

let mockSearchParams = new URLSearchParams();

jest.mock("next/navigation", () => ({
  useSearchParams: () => mockSearchParams,
}));

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  mockSearchParams = new URLSearchParams();

  global.fetch = mockFetch as jest.Mock;
});

function mockCurrentUserResponse() {
  return {
    isAuthenticated: true,
    user: {
      id: 1,
      name: "一般 太郎",
      email: "user@example.com",
      roles: ["GeneralUser"],
    },
  };
}

function mockReservationsResponse() {
  return {
    items: [
      {
        reservationId: 1,
        roomId: 101,
        roomName: "池袋ダンスルーム",
        roomImageName: "room01.jpg",
        roomAddress: "東京都豊島区東池袋",
        startAt: "2026-05-10T10:00:00",
        endAt: "2026-05-10T12:00:00",
        amount: 6000,
        status: "paid",
        createdAtUtc: "2026-05-01T09:00:00",
      },
      {
        reservationId: 2,
        roomId: 102,
        roomName: "渋谷撮影スタジオ",
        roomImageName: null,
        roomAddress: "東京都渋谷区渋谷",
        startAt: "2026-05-11T13:00:00",
        endAt: "2026-05-11T15:00:00",
        amount: 9000,
        status: "booked",
        createdAtUtc: "2026-05-02T09:00:00",
      },
      {
        reservationId: 3,
        roomId: 103,
        roomName: "新宿配信スタジオ",
        roomImageName: "room03.jpg",
        roomAddress: "東京都新宿区新宿",
        startAt: "2026-05-12T09:00:00",
        endAt: "2026-05-12T11:00:00",
        amount: 5000,
        status: "canceled",
        createdAtUtc: "2026-05-03T09:00:00",
      },
    ],
    page: 1,
    pageSize: 10,
    totalCount: 3,
    totalPages: 1,
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
      json: async () => mockReservationsResponse(),
    });

  render(<ReservationsPage />);
}

async function waitForLoaded() {
  await waitFor(() => {
    expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
  });
}

describe("ReservationsPage", () => {
  test("読み込み中メッセージを表示する", () => {
    setupSuccessInitialRender();

    expect(screen.getByText("予約一覧を読み込み中...")).toBeInTheDocument();
  });

  test("API成功時、予約一覧を表示する", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByText("RESERVATION HISTORY")).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "予約一覧・履歴" })
    ).toBeInTheDocument();

    expect(
      screen.getByText("予約済み・決済済み・キャンセル済みの予約を確認できます。")
    ).toBeInTheDocument();

    expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    expect(screen.getAllByText("渋谷撮影スタジオ").length).toBeGreaterThan(0);
    expect(screen.getAllByText("新宿配信スタジオ").length).toBeGreaterThan(0);

    expect(screen.getAllByText("東京都豊島区東池袋").length).toBeGreaterThan(0);
    expect(screen.getAllByText("東京都渋谷区渋谷").length).toBeGreaterThan(0);
    expect(screen.getAllByText("東京都新宿区新宿").length).toBeGreaterThan(0);

    expect(screen.getAllByText("6,000 円").length).toBeGreaterThan(0);
    expect(screen.getAllByText("9,000 円").length).toBeGreaterThan(0);
    expect(screen.getAllByText("5,000 円").length).toBeGreaterThan(0);

    expect(screen.getAllByText("決済済み").length).toBeGreaterThan(0);
    expect(screen.getAllByText("予約済み").length).toBeGreaterThan(0);
    expect(screen.getAllByText("キャンセル").length).toBeGreaterThan(0);
  });

  test("予約完了クエリがある場合、完了メッセージを表示する", async () => {
    mockSearchParams = new URLSearchParams("reserved=true");

    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByText("予約が完了しました。")).toBeInTheDocument();

    expect(
      screen.getByText("ご予約ありがとうございます。予約内容は下記の一覧から確認できます。")
    ).toBeInTheDocument();
  });

  test("予約履歴が0件の場合、空メッセージを表示する", async () => {
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
          items: [],
          page: 1,
          pageSize: 10,
          totalCount: 0,
          totalPages: 1,
        }),
      });

    render(<ReservationsPage />);

    await waitFor(() => {
      expect(screen.getByText("予約履歴はまだありません。")).toBeInTheDocument();
    });

    expect(
      screen.getByText("スタジオ詳細ページから予約できます。")
    ).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "スタジオを探す" })).toHaveAttribute(
      "href",
      "/rooms"
    );
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
          email: "host@example.com",
          roles: ["Host"],
        },
      }),
    });

    render(<ReservationsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("予約一覧は一般会員のみ閲覧できます。")
      ).toBeInTheDocument();
    });

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("ログイン情報取得失敗時、エラーメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<ReservationsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("ログイン情報の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("予約一覧取得で403の場合、権限エラーを表示する", async () => {
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

    render(<ReservationsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("予約一覧は一般会員のみ閲覧できます。")
      ).toBeInTheDocument();
    });
  });

  test("予約一覧取得失敗時、エラーメッセージを表示する", async () => {
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

    render(<ReservationsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("予約一覧の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<ReservationsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("レビューを書くリンクは決済済み予約のみ表示される", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    const reviewLinks = screen.getAllByRole("link", { name: "レビューを書く" });

    expect(reviewLinks.length).toBeGreaterThan(0);

    expect(
      reviewLinks.some(
        (link) =>
          link.getAttribute("href") ===
          "/rooms/101/reviews/new?reservationId=1"
      )
    ).toBe(true);

    expect(
      reviewLinks.some(
        (link) =>
          link.getAttribute("href") ===
          "/rooms/102/reviews/new?reservationId=2"
      )
    ).toBe(false);
  });

  test("スタジオ詳細リンクの遷移先が正しい", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    const roomLinks = screen.getAllByRole("link", { name: "池袋ダンスルーム" });

    expect(roomLinks.some((link) => link.getAttribute("href") === "/rooms/101"))
      .toBe(true);
  });

  test("画像がある場合はstorage、ない場合はnoImageを表示する", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByAltText("池袋ダンスルーム")).toHaveAttribute(
      "src",
      "/storage/room01.jpg"
    );

    expect(screen.getByAltText("渋谷撮影スタジオ")).toHaveAttribute(
      "src",
      "/images/noImage.png"
    );
  });

  test("ページングで次ページを取得する", async () => {
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
          ...mockReservationsResponse(),
          page: 1,
          totalCount: 25,
          totalPages: 3,
        }),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockCurrentUserResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          ...mockReservationsResponse(),
          page: 2,
          totalCount: 25,
          totalPages: 3,
        }),
      });

    render(<ReservationsPage />);

    await waitForLoaded();

    await user.click(screen.getByRole("button", { name: "次" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(4);
    });

    expect(mockFetch.mock.calls[2][0]).toBe(
      "https://localhost:7226/api/auth/me"
    );

    expect(mockFetch.mock.calls[3][0]).toBe(
      "https://localhost:7226/api/reservations?page=2&pageSize=10"
    );
  });

  test("API呼び出しURLが正しい", async () => {
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
      "https://localhost:7226/api/reservations?page=1&pageSize=10"
    );

    expect(mockFetch.mock.calls[1][1]).toEqual(
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      })
    );
  });

  test("スタジオ一覧へリンクを表示する", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByRole("link", { name: "スタジオ一覧へ" })).toHaveAttribute(
      "href",
      "/rooms"
    );
  });
});