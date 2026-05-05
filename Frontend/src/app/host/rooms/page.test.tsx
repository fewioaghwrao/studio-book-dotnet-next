import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import HostRoomsPage from "./page";

const mockFetch = jest.fn();
const mockPush = jest.fn();

let mockSearchParams = new URLSearchParams();

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mockPush,
  }),
  useSearchParams: () => mockSearchParams,
}));

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();
  mockPush.mockReset();

  mockSearchParams = new URLSearchParams();

  global.fetch = mockFetch as jest.Mock;
});

function mockRoomsResponse() {
  return {
    items: [
      {
        id: 1,
        name: "池袋ダンスルーム",
        postalCode: "170-0013",
        address: "東京都豊島区東池袋",
      },
      {
        id: 2,
        name: "渋谷撮影スタジオ",
        postalCode: "150-0002",
        address: "東京都渋谷区渋谷",
      },
    ],
    page: 1,
    pageSize: 10,
    totalCount: 2,
    totalPages: 1,
  };
}

function mockCurrentUserResponse() {
  return {
    isAuthenticated: true,
    user: {
      id: 10,
      name: "ホスト 太郎",
      kana: "ホスト タロウ",
      email: "host@example.com",
      postalCode: "101-0022",
      address: "東京都千代田区",
      phoneNumber: "090-1234-5678",
      usageType: "business",
      roles: ["Host"],
    },
  };
}

describe("HostRoomsPage", () => {
  test("スタジオ一覧の基本表示を行う", async () => {
    mockFetch
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
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockCurrentUserResponse(),
      });

    render(<HostRoomsPage />);

    expect(screen.getByText("スタジオ一覧を読み込み中...")).toBeInTheDocument();

    await waitFor(() => {
      expect(
        screen.getByText("該当するスタジオがありません。")
      ).toBeInTheDocument();
    });

    expect(screen.getByText("HOST ROOMS")).toBeInTheDocument();
    expect(
      screen.getByText("登録済みスタジオの確認、詳細表示、各種設定画面へ移動できます。")
    ).toBeInTheDocument();
    expect(screen.getByText("全 0 件")).toBeInTheDocument();
  });

  test("API成功時、ホスト名とスタジオ一覧を表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockRoomsResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockCurrentUserResponse(),
      });

    render(<HostRoomsPage />);

    await waitFor(() => {
      expect(screen.getByText("池袋ダンスルーム")).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "ホスト 太郎様のスタジオ一覧" })
      ).toBeInTheDocument();
    });

    expect(screen.getByText("渋谷撮影スタジオ")).toBeInTheDocument();
    expect(screen.getByText("170-0013")).toBeInTheDocument();
    expect(screen.getByText("150-0002")).toBeInTheDocument();
    expect(screen.getByText("東京都豊島区東池袋")).toBeInTheDocument();
    expect(screen.getByText("東京都渋谷区渋谷")).toBeInTheDocument();
    expect(screen.getAllByText("ホスト管理").length).toBeGreaterThan(0);
    expect(screen.getByText("全 2 件")).toBeInTheDocument();
  });

  test("各リンクの遷移先が正しい", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockRoomsResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockCurrentUserResponse(),
      });

    render(<HostRoomsPage />);

    await waitFor(() => {
      expect(screen.getByText("池袋ダンスルーム")).toBeInTheDocument();
    });

    expect(screen.getAllByRole("link", { name: "休館日設定" })[0])
      .toHaveAttribute("href", "/host/rooms/1/closures");

    expect(screen.getAllByRole("link", { name: "営業時間設定" })[0])
      .toHaveAttribute("href", "/host/rooms/1/business-hours");

    expect(screen.getAllByRole("link", { name: "料金ルール設定" })[0])
      .toHaveAttribute("href", "/host/rooms/1/price-rules");

    expect(screen.getAllByRole("link", { name: "詳細" })[0])
      .toHaveAttribute("href", "/host/rooms/1");
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
        json: async () => mockCurrentUserResponse(),
      });

    render(<HostRoomsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("ホストユーザーのみアクセスできます。")
      ).toBeInTheDocument();
    });
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
        json: async () => mockCurrentUserResponse(),
      });

    render(<HostRoomsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("スタジオ一覧の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    mockFetch
      .mockRejectedValueOnce(new Error("Network Error"))
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockCurrentUserResponse(),
      });

    render(<HostRoomsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("検索するとキーワード付きURLへ遷移する", async () => {
    const user = userEvent.setup();

    mockFetch
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
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockCurrentUserResponse(),
      });

    render(<HostRoomsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("該当するスタジオがありません。")
      ).toBeInTheDocument();
    });

    await user.type(screen.getByPlaceholderText("スタジオ名"), "池袋");
    await user.click(screen.getByRole("button", { name: "検索" }));

    expect(mockPush).toHaveBeenCalledWith(
      "/host/rooms?keyword=%E6%B1%A0%E8%A2%8B&page=1"
    );
  });

  test("ページングボタンを押すと指定ページへ遷移する", async () => {
    const user = userEvent.setup();

    mockSearchParams = new URLSearchParams("page=1");

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          items: [
            {
              id: 1,
              name: "池袋ダンスルーム",
              postalCode: "170-0013",
              address: "東京都豊島区東池袋",
            },
          ],
          page: 1,
          pageSize: 10,
          totalCount: 25,
          totalPages: 3,
        }),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockCurrentUserResponse(),
      });

    render(<HostRoomsPage />);

    await waitFor(() => {
      expect(screen.getByText("池袋ダンスルーム")).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "次" }));

    expect(mockPush).toHaveBeenCalledWith("/host/rooms?page=2");
  });

  test("検索条件を保持したままページングする", async () => {
    const user = userEvent.setup();

    mockSearchParams = new URLSearchParams("keyword=池袋&page=1");

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          items: [
            {
              id: 1,
              name: "池袋ダンスルーム",
              postalCode: "170-0013",
              address: "東京都豊島区東池袋",
            },
          ],
          page: 1,
          pageSize: 10,
          totalCount: 25,
          totalPages: 3,
        }),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockCurrentUserResponse(),
      });

    render(<HostRoomsPage />);

    await waitFor(() => {
      expect(screen.getByText("池袋ダンスルーム")).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "次" }));

    expect(mockPush).toHaveBeenCalledWith(
      "/host/rooms?keyword=%E6%B1%A0%E8%A2%8B&page=2"
    );
  });

  test("API呼び出しURLが正しい", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockRoomsResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockCurrentUserResponse(),
      });

    render(<HostRoomsPage />);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalled();
    });

    expect(mockFetch.mock.calls[0][0]).toBe(
      "https://localhost:7226/api/host/rooms?page=1&pageSize=10"
    );

    expect(mockFetch.mock.calls[0][1]).toEqual(
      expect.objectContaining({
        method: "GET",        cache: "no-store",
      })
    );
  });
});