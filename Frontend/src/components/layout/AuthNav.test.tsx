import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import AuthNav from "./AuthNav";

const mockFetch = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

describe("AuthNav", () => {
  test("未ログインの場合、ログインと会員登録リンクを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 401,
    });

    render(<AuthNav />);

    expect(screen.getByText("読み込み中...")).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByRole("link", { name: "ログイン" })).toBeInTheDocument();
    });

    expect(screen.getByRole("link", { name: "ログイン" })).toHaveAttribute(
      "href",
      "/auth/login"
    );
    expect(screen.getByRole("link", { name: "会員登録" })).toHaveAttribute(
      "href",
      "/signup"
    );
  });

  test("一般ユーザーの場合、会員向けメニューを表示する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        isAuthenticated: true,
        user: {
          id: 1,
          name: "一般ユーザー",
          email: "user@example.com",
          roles: ["GeneralUser"],
        },
      }),
    });

    render(<AuthNav />);

    const menuButton = await screen.findByRole("button", {
      name: /一般ユーザー/,
    });

    await user.click(menuButton);

    expect(screen.getByText("会員情報")).toBeInTheDocument();
    expect(screen.getByText("予約一覧・履歴")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "ログアウト" })).toBeInTheDocument();
  });

  test("管理者の場合、管理者メニューを表示する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        isAuthenticated: true,
        user: {
          id: 2,
          name: "管理者",
          email: "admin@example.com",
          roles: ["Admin"],
        },
      }),
    });

    render(<AuthNav />);

    const menuButton = await screen.findByRole("button", {
      name: /管理者メニュー/,
    });

    await user.click(menuButton);

    expect(screen.getByText("会員情報")).toBeInTheDocument();
    expect(screen.getByText("データ一覧")).toBeInTheDocument();
    expect(screen.getByText("会員一覧")).toBeInTheDocument();
    expect(screen.getByText("スタジオ一覧")).toBeInTheDocument();
    expect(screen.getByText("予約一覧")).toBeInTheDocument();
    expect(screen.getByText("ログ一覧")).toBeInTheDocument();
    expect(screen.getByText("管理設定")).toBeInTheDocument();
    expect(screen.getByText("AI検索ログ")).toBeInTheDocument();
  });

  test("ホストの場合、スタジオ提供者メニューを表示する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        isAuthenticated: true,
        user: {
          id: 3,
          name: "ホストユーザー",
          email: "host@example.com",
          roles: ["Host"],
        },
      }),
    });

    render(<AuthNav />);

    const menuButton = await screen.findByRole("button", {
      name: /スタジオ提供者メニュー/,
    });

    await user.click(menuButton);

    expect(screen.getByText("会員情報")).toBeInTheDocument();
    expect(screen.getByText("スタジオ一覧")).toBeInTheDocument();
    expect(screen.getByText("予約一覧")).toBeInTheDocument();
    expect(screen.getByText("レビュー一覧")).toBeInTheDocument();
    expect(screen.getByText("売上明細一覧")).toBeInTheDocument();
    expect(screen.getByText("統計一覧")).toBeInTheDocument();
  });
});