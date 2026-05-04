import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import AdminEditPage from "./page";

const mockFetch = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

describe("AdminEditPage", () => {
  test("読み込み中メッセージを表示する", () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        isAuthenticated: true,
        user: {
          id: 1,
          name: "管理 太郎",
          kana: "カンリ タロウ",
          email: "admin@example.com",
          postalCode: "101-0022",
          address: "東京都千代田区神田練塀町300番地",
          phoneNumber: "090-1234-5678",
          roles: ["Admin"],
        },
      }),
    });

    render(<AdminEditPage />);

    expect(screen.getByText("管理者情報を読み込み中...")).toBeInTheDocument();
  });

  test("管理者情報を取得してフォームに表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        isAuthenticated: true,
        user: {
          id: 1,
          name: "管理 太郎",
          kana: "カンリ タロウ",
          email: "admin@example.com",
          postalCode: "101-0022",
          address: "東京都千代田区神田練塀町300番地",
          phoneNumber: "090-1234-5678",
          roles: ["Admin"],
        },
      }),
    });

    render(<AdminEditPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "管理者情報編集" })
      ).toBeInTheDocument();
    });

    expect(screen.getByDisplayValue("管理 太郎")).toBeInTheDocument();
    expect(screen.getByDisplayValue("カンリ タロウ")).toBeInTheDocument();
    expect(screen.getByDisplayValue("101-0022")).toBeInTheDocument();
    expect(
      screen.getByDisplayValue("東京都千代田区神田練塀町300番地")
    ).toBeInTheDocument();
    expect(screen.getByDisplayValue("090-1234-5678")).toBeInTheDocument();
    expect(screen.getByDisplayValue("admin@example.com")).toBeInTheDocument();

    expect(
      screen.getByText("デモ環境のため、管理者情報の更新は無効化しています。")
    ).toBeInTheDocument();
  });

  test("デモ環境では入力欄と更新ボタンが無効化される", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        isAuthenticated: true,
        user: {
          id: 1,
          name: "管理 太郎",
          kana: "カンリ タロウ",
          email: "admin@example.com",
          postalCode: "101-0022",
          address: "東京都千代田区神田練塀町300番地",
          phoneNumber: "090-1234-5678",
          roles: ["Admin"],
        },
      }),
    });

    render(<AdminEditPage />);

    await waitFor(() => {
      expect(screen.getByDisplayValue("管理 太郎")).toBeInTheDocument();
    });

    expect(screen.getByDisplayValue("管理 太郎")).toBeDisabled();
    expect(screen.getByDisplayValue("カンリ タロウ")).toBeDisabled();
    expect(screen.getByDisplayValue("101-0022")).toBeDisabled();
    expect(
      screen.getByDisplayValue("東京都千代田区神田練塀町300番地")
    ).toBeDisabled();
    expect(screen.getByDisplayValue("090-1234-5678")).toBeDisabled();
    expect(screen.getByDisplayValue("admin@example.com")).toBeDisabled();

    expect(
      screen.getByRole("button", { name: "デモ環境では更新不可" })
    ).toBeDisabled();
  });

  test("非管理者の場合、権限エラーを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        isAuthenticated: true,
        user: {
          id: 2,
          name: "一般 ユーザー",
          kana: "イッパン ユーザー",
          email: "user@example.com",
          postalCode: "150-0001",
          address: "東京都渋谷区",
          phoneNumber: "090-0000-0000",
          roles: ["GeneralUser"],
        },
      }),
    });

    render(<AdminEditPage />);

    await waitFor(() => {
      expect(
        screen.getByText("このページを表示する権限がありません。")
      ).toBeInTheDocument();
    });

    expect(screen.getByRole("link", { name: "管理者情報へ戻る" }))
      .toHaveAttribute("href", "/admin");

    expect(screen.getByRole("link", { name: "ホームへ戻る" }))
      .toHaveAttribute("href", "/");
  });

  test("API失敗時、取得失敗メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<AdminEditPage />);

    await waitFor(() => {
      expect(
        screen.getByText("管理者情報の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<AdminEditPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("デモ環境では送信してもPUTリクエストは実行されない", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        isAuthenticated: true,
        user: {
          id: 1,
          name: "管理 太郎",
          kana: "カンリ タロウ",
          email: "admin@example.com",
          postalCode: "101-0022",
          address: "東京都千代田区神田練塀町300番地",
          phoneNumber: "090-1234-5678",
          roles: ["Admin"],
        },
      }),
    });

    render(<AdminEditPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: "デモ環境では更新不可" })
      ).toBeInTheDocument();
    });

    await user.click(
      screen.getByRole("button", { name: "デモ環境では更新不可" })
    );

    expect(mockFetch).toHaveBeenCalledTimes(1);
    expect(mockFetch).not.toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({
        method: "PUT",
      })
    );
  });
});