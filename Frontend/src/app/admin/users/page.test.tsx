import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import AdminUsersPage from "./page";

const mockFetch = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

function mockUsersResponse() {
  return {
    items: [
      {
        id: 1,
        name: "山田 太郎",
        kana: "ヤマダ タロウ",
        email: "yamada@example.com",
        roleName: "GeneralUser",
        roleLabel: "一般会員",
        enabled: true,
      },
      {
        id: 2,
        name: "佐藤 花子",
        kana: "",
        email: "sato@example.com",
        roleName: "Host",
        roleLabel: "",
        enabled: false,
      },
    ],
    keyword: "",
    page: 1,
    pageSize: 10,
    totalCount: 2,
    totalPages: 1,
  };
}

describe("AdminUsersPage", () => {
  test("会員一覧の基本表示を行う", async () => {
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

    render(<AdminUsersPage />);

    expect(
      screen.getByRole("heading", { name: "会員一覧" })
    ).toBeInTheDocument();

    expect(
      screen.getByText("登録されている会員情報を検索・確認できます。")
    ).toBeInTheDocument();

    expect(screen.getByText("会員一覧を読み込み中...")).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText("会員情報がありません。")).toBeInTheDocument();
    });
  });

  test("API成功時、会員一覧を表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockUsersResponse(),
    });

    render(<AdminUsersPage />);

    await waitFor(() => {
      expect(screen.getAllByText("山田 太郎").length).toBeGreaterThan(0);
    });

    expect(screen.getAllByText("佐藤 花子").length).toBeGreaterThan(0);
    expect(screen.getAllByText("ヤマダ タロウ").length).toBeGreaterThan(0);
    expect(screen.getAllByText("未登録").length).toBeGreaterThan(0);
    expect(screen.getAllByText("yamada@example.com").length).toBeGreaterThan(0);
    expect(screen.getAllByText("sato@example.com").length).toBeGreaterThan(0);
    expect(screen.getAllByText("一般会員").length).toBeGreaterThan(0);
    expect(screen.getAllByText("未設定").length).toBeGreaterThan(0);
    expect(screen.getAllByText("有効").length).toBeGreaterThan(0);
    expect(screen.getAllByText("無効").length).toBeGreaterThan(0);

    expect(screen.getByText("全2件中 1 / 1 ページ")).toBeInTheDocument();

    const detailLinks = screen.getAllByRole("link", { name: "詳細" });

    expect(
      detailLinks.some((link) => link.getAttribute("href") === "/admin/users/1")
    ).toBe(true);

    expect(
      detailLinks.some((link) => link.getAttribute("href") === "/admin/users/2")
    ).toBe(true);
  });

  test("403の場合、管理者のみアクセス可能メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 403,
    });

    render(<AdminUsersPage />);

    await waitFor(() => {
      expect(screen.getByText("管理者のみアクセスできます。")).toBeInTheDocument();
    });
  });

  test("API失敗時、エラーメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<AdminUsersPage />);

    await waitFor(() => {
      expect(
        screen.getByText("会員一覧の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<AdminUsersPage />);

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
              name: "山田 太郎",
              kana: "ヤマダ タロウ",
              email: "yamada@example.com",
              roleName: "GeneralUser",
              roleLabel: "一般会員",
              enabled: true,
            },
          ],
          keyword: "山田",
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
        }),
      });

    render(<AdminUsersPage />);

    await waitFor(() => {
      expect(screen.getByText("会員情報がありません。")).toBeInTheDocument();
    });

    await user.type(
      screen.getByPlaceholderText("氏名・フリガナ・メールアドレス"),
      "山田"
    );

    await user.click(screen.getByRole("button", { name: "検索" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch).toHaveBeenLastCalledWith(
      expect.stringContaining(
        "/api/admin/users?page=1&pageSize=10&keyword=%E5%B1%B1%E7%94%B0"
      ),
      expect.objectContaining({
        method: "GET",        cache: "no-store",
      })
    );

    expect(screen.getByText("検索条件：")).toBeInTheDocument();
    expect(screen.getByText("山田")).toBeInTheDocument();
  });

  test("クリアボタンを押すと入力中のキーワードがクリアされる", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockUsersResponse(),
    });

    render(<AdminUsersPage />);

    await waitFor(() => {
      expect(screen.getAllByText("山田 太郎").length).toBeGreaterThan(0);
    });

    const keywordInput = screen.getByPlaceholderText(
      "氏名・フリガナ・メールアドレス"
    ) as HTMLInputElement;

    await user.type(keywordInput, "山田");

    expect(keywordInput).toHaveValue("山田");

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
              name: "山田 太郎",
              kana: "ヤマダ タロウ",
              email: "yamada@example.com",
              roleName: "GeneralUser",
              roleLabel: "一般会員",
              enabled: true,
            },
          ],
          keyword: "山田",
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

    render(<AdminUsersPage />);

    await waitFor(() => {
      expect(screen.getByText("会員情報がありません。")).toBeInTheDocument();
    });

    const keywordInput = screen.getByPlaceholderText(
      "氏名・フリガナ・メールアドレス"
    );

    await user.type(keywordInput, "山田");
    await user.click(screen.getByRole("button", { name: "検索" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    await waitFor(() => {
      expect(screen.getAllByText("山田 太郎").length).toBeGreaterThan(0);
    });

    await user.click(screen.getByRole("button", { name: "クリア" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(3);
    });

    expect(mockFetch).toHaveBeenLastCalledWith(
      expect.stringContaining("/api/admin/users?page=1&pageSize=10"),
      expect.objectContaining({
        method: "GET",        cache: "no-store",
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
              name: "山田 太郎",
              kana: "ヤマダ タロウ",
              email: "yamada@example.com",
              roleName: "GeneralUser",
              roleLabel: "一般会員",
              enabled: true,
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
              name: "2ページ目ユーザー",
              kana: "ニページメ ユーザー",
              email: "page2@example.com",
              roleName: "Host",
              roleLabel: "スタジオ提供者",
              enabled: true,
            },
          ],
          keyword: "",
          page: 2,
          pageSize: 10,
          totalCount: 25,
          totalPages: 3,
        }),
      });

    render(<AdminUsersPage />);

    await waitFor(() => {
      expect(screen.getAllByText("山田 太郎").length).toBeGreaterThan(0);
    });

    await user.click(screen.getByRole("button", { name: "次" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch).toHaveBeenLastCalledWith(
      expect.stringContaining("/api/admin/users?page=2&pageSize=10"),
      expect.objectContaining({
        method: "GET",        cache: "no-store",
      })
    );

    await waitFor(() => {
      expect(screen.getAllByText("2ページ目ユーザー").length).toBeGreaterThan(0);
    });
  });
});