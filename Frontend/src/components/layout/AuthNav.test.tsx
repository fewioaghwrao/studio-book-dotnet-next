import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import AuthNav from "./AuthNav";

const mockFetch = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  localStorage.clear();

  global.fetch = mockFetch as jest.Mock;
});

describe("AuthNav", () => {
  test("未ログインの場合、ログインと会員登録リンクを表示する", async () => {
    render(<AuthNav />);

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

    expect(mockFetch).not.toHaveBeenCalled();
  });

  test("tokenありでAPIが401の場合、ログインと会員登録リンクを表示する", async () => {
    localStorage.setItem("token", "test-token");

    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 401,
    });

    render(<AuthNav />);

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

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/auth/me",
      expect.objectContaining({
        method: "GET",
        cache: "no-store",
        headers: expect.objectContaining({
          Authorization: "Bearer test-token",
        }),
      })
    );
  });

  test("一般ユーザーの場合、会員向けメニューを表示する", async () => {
    const user = userEvent.setup();

    localStorage.setItem("token", "test-token");

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
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

    expect(screen.getAllByText("一般ユーザー").length).toBeGreaterThan(0);
    expect(screen.getByText("ログイン中")).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "会員情報" })).toHaveAttribute(
      "href",
      "/user"
    );

    expect(
      screen.getByRole("link", { name: "予約一覧・履歴" })
    ).toHaveAttribute("href", "/reservations");

    expect(screen.getByRole("button", { name: "ログアウト" })).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/auth/me",
      expect.objectContaining({
        method: "GET",
        cache: "no-store",
        headers: expect.objectContaining({
          Authorization: "Bearer test-token",
        }),
      })
    );
  });

  test("管理者の場合、管理者メニューを表示する", async () => {
    const user = userEvent.setup();

    localStorage.setItem("token", "test-token");

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
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

    expect(screen.getAllByText("管理者メニュー").length).toBeGreaterThan(0);

    expect(screen.getByRole("link", { name: "会員情報" })).toHaveAttribute(
      "href",
      "/admin"
    );

    expect(screen.getByRole("link", { name: "データ一覧" })).toHaveAttribute(
      "href",
      "/admin/status"
    );

    expect(screen.getByRole("link", { name: "会員一覧" })).toHaveAttribute(
      "href",
      "/admin/users"
    );

    expect(screen.getByRole("link", { name: "スタジオ一覧" })).toHaveAttribute(
      "href",
      "/admin/rooms"
    );

    expect(screen.getByRole("link", { name: "予約一覧" })).toHaveAttribute(
      "href",
      "/admin/reservations"
    );

    expect(screen.getByRole("link", { name: "ログ一覧" })).toHaveAttribute(
      "href",
      "/admin/logs"
    );

    expect(screen.getByRole("link", { name: "管理設定" })).toHaveAttribute(
      "href",
      "/admin/settings"
    );

    expect(screen.getByRole("link", { name: "AI検索ログ" })).toHaveAttribute(
      "href",
      "/admin/ai-search-logs"
    );

    expect(screen.getByRole("button", { name: "ログアウト" })).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/auth/me",
      expect.objectContaining({
        method: "GET",
        cache: "no-store",
        headers: expect.objectContaining({
          Authorization: "Bearer test-token",
        }),
      })
    );
  });

  test("ホストの場合、スタジオ提供者メニューを表示する", async () => {
    const user = userEvent.setup();

    localStorage.setItem("token", "test-token");

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
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

    expect(
      screen.getAllByText("スタジオ提供者メニュー").length
    ).toBeGreaterThan(0);

    expect(screen.getByRole("link", { name: "会員情報" })).toHaveAttribute(
      "href",
      "/host"
    );

    expect(screen.getByRole("link", { name: "スタジオ一覧" })).toHaveAttribute(
      "href",
      "/host/rooms"
    );

    expect(screen.getByRole("link", { name: "予約一覧" })).toHaveAttribute(
      "href",
      "/host/reservations"
    );

    expect(screen.getByRole("link", { name: "レビュー一覧" })).toHaveAttribute(
      "href",
      "/host/reviews"
    );

    expect(screen.getByRole("link", { name: "売上明細一覧" })).toHaveAttribute(
      "href",
      "/host/sales"
    );

    expect(screen.getByRole("link", { name: "統計一覧" })).toHaveAttribute(
      "href",
      "/host/status"
    );

    expect(screen.getByRole("button", { name: "ログアウト" })).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/auth/me",
      expect.objectContaining({
        method: "GET",
        cache: "no-store",
        headers: expect.objectContaining({
          Authorization: "Bearer test-token",
        }),
      })
    );
  });
});