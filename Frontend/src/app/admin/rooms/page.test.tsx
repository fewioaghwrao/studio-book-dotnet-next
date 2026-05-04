import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import AdminRoomsPage from "./page";

const mockFetch = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

describe("AdminRoomsPage", () => {
  test("スタジオ一覧の基本表示を行う", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        items: [],
        keyword: "",
        page: 1,
        pageSize: 10,
        totalCount: 0,
        totalPages: 1,
      }),
    });

    render(<AdminRoomsPage />);

    expect(
      screen.getByRole("heading", { name: "スタジオ一覧" })
    ).toBeInTheDocument();

    expect(
      screen.getByText("登録されているスタジオ情報を検索・確認できます。")
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "公開デモでは誤操作防止のため、スタジオ削除機能は提供していません。"
      )
    ).toBeInTheDocument();

    expect(screen.getByText("スタジオ一覧を読み込み中...")).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText("スタジオ情報がありません。")).toBeInTheDocument();
    });
  });

  test("API成功時、スタジオ一覧を表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        items: [
          {
            id: 1,
            name: "池袋ダンスルーム",
            postalCode: "170-0013",
            address: "東京都豊島区東池袋",
            hostUserId: 10,
            hostName: "ホスト太郎",
          },
          {
            id: 2,
            name: "渋谷撮影スタジオ",
            postalCode: "150-0002",
            address: "東京都渋谷区渋谷",
            hostUserId: 11,
            hostName: "",
          },
        ],
        keyword: "",
        page: 1,
        pageSize: 10,
        totalCount: 2,
        totalPages: 1,
      }),
    });

    render(<AdminRoomsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    });

    expect(screen.getAllByText("渋谷撮影スタジオ").length).toBeGreaterThan(0);
    expect(screen.getAllByText("170-0013").length).toBeGreaterThan(0);
    expect(screen.getAllByText("150-0002").length).toBeGreaterThan(0);
    expect(screen.getAllByText("東京都豊島区東池袋").length).toBeGreaterThan(0);
    expect(screen.getAllByText("東京都渋谷区渋谷").length).toBeGreaterThan(0);
    expect(screen.getAllByText("ホスト太郎").length).toBeGreaterThan(0);
    expect(screen.getAllByText("未設定").length).toBeGreaterThan(0);

    expect(screen.getByText("全2件中 1 / 1 ページ")).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "登録" })).toHaveAttribute(
      "href",
      "/admin/rooms/register"
    );

    const detailLinks = screen.getAllByRole("link", { name: "基本情報" });

    expect(
      detailLinks.some((link) => link.getAttribute("href") === "/admin/rooms/1")
    ).toBe(true);

    expect(
      detailLinks.some((link) => link.getAttribute("href") === "/admin/rooms/2")
    ).toBe(true);
  });

  test("403の場合、管理者のみアクセス可能メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 403,
    });

    render(<AdminRoomsPage />);

    await waitFor(() => {
      expect(screen.getByText("管理者のみアクセスできます。")).toBeInTheDocument();
    });
  });

  test("API失敗時、エラーメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<AdminRoomsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("スタジオ一覧の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<AdminRoomsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("キーワード検索を行うと検索条件付きで再取得する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          items: [],
          keyword: "",
          page: 1,
          pageSize: 10,
          totalCount: 0,
          totalPages: 1,
        }),
      })
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
              hostUserId: 10,
              hostName: "ホスト太郎",
            },
          ],
          keyword: "池袋",
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
        }),
      });

    render(<AdminRoomsPage />);

    await waitFor(() => {
      expect(screen.getByText("スタジオ情報がありません。")).toBeInTheDocument();
    });

    await user.type(
      screen.getByPlaceholderText("スタジオ名・住所・提供者名"),
      "池袋"
    );

    await user.click(screen.getByRole("button", { name: "検索" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch).toHaveBeenLastCalledWith(
      expect.stringContaining(
        "/api/admin/rooms?page=1&pageSize=10&keyword=%E6%B1%A0%E8%A2%8B"
      ),
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      })
    );

    expect(screen.getByText("検索条件：")).toBeInTheDocument();
    expect(screen.getByText("池袋")).toBeInTheDocument();
  });

  test("クリアボタンを押すと入力中のキーワードがクリアされる", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        items: [
          {
            id: 1,
            name: "池袋ダンスルーム",
            postalCode: "170-0013",
            address: "東京都豊島区東池袋",
            hostUserId: 10,
            hostName: "ホスト太郎",
          },
        ],
        keyword: "",
        page: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1,
      }),
    });

    render(<AdminRoomsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    });

    const keywordInput = screen.getByPlaceholderText(
      "スタジオ名・住所・提供者名"
    ) as HTMLInputElement;

    await user.type(keywordInput, "池袋");

    expect(keywordInput).toHaveValue("池袋");

    await user.click(screen.getByRole("button", { name: "クリア" }));

    expect(keywordInput).toHaveValue("");
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("検索後にクリアすると検索条件なしで再取得する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          items: [],
          keyword: "",
          page: 1,
          pageSize: 10,
          totalCount: 0,
          totalPages: 1,
        }),
      })
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
              hostUserId: 10,
              hostName: "ホスト太郎",
            },
          ],
          keyword: "池袋",
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
        }),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          items: [],
          keyword: "",
          page: 1,
          pageSize: 10,
          totalCount: 0,
          totalPages: 1,
        }),
      });

    render(<AdminRoomsPage />);

    await waitFor(() => {
      expect(screen.getByText("スタジオ情報がありません。")).toBeInTheDocument();
    });

    const keywordInput = screen.getByPlaceholderText("スタジオ名・住所・提供者名");

    await user.type(keywordInput, "池袋");
    await user.click(screen.getByRole("button", { name: "検索" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    await waitFor(() => {
      expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    });

    await user.click(screen.getByRole("button", { name: "クリア" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(3);
    });

    expect(mockFetch).toHaveBeenLastCalledWith(
      expect.stringContaining("/api/admin/rooms?page=1&pageSize=10"),
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      })
    );
  });

  test("ページングボタンを押すと指定ページを取得する", async () => {
    const user = userEvent.setup();

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
              hostUserId: 10,
              hostName: "ホスト太郎",
            },
          ],
          keyword: "",
          page: 1,
          pageSize: 10,
          totalCount: 25,
          totalPages: 3,
        }),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          items: [
            {
              id: 11,
              name: "2ページ目スタジオ",
              postalCode: "160-0000",
              address: "東京都新宿区",
              hostUserId: 12,
              hostName: "ホスト次郎",
            },
          ],
          keyword: "",
          page: 2,
          pageSize: 10,
          totalCount: 25,
          totalPages: 3,
        }),
      });

    render(<AdminRoomsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    });

    await user.click(screen.getByRole("button", { name: "次" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch).toHaveBeenLastCalledWith(
      expect.stringContaining("/api/admin/rooms?page=2&pageSize=10"),
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      })
    );

    await waitFor(() => {
      expect(screen.getAllByText("2ページ目スタジオ").length).toBeGreaterThan(0);
    });
  });
});