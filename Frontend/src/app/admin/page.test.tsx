import { render, screen, waitFor } from "@testing-library/react";
import AdminPage from "./page";

const mockFetch = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

function mockAdminUserResponse() {
  return {
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
  };
}

describe("AdminPage", () => {
  test("読み込み中メッセージを表示する", () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockAdminUserResponse(),
    });

    render(<AdminPage />);

    expect(screen.getByText("管理者情報を読み込み中...")).toBeInTheDocument();
  });

  test("管理者情報を取得して表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockAdminUserResponse(),
    });

    render(<AdminPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "管理者情報" })
      ).toBeInTheDocument();
    });

    expect(
      screen.getByText("管理者として登録されている基本情報を確認できます。")
    ).toBeInTheDocument();

    expect(screen.getByText("氏名")).toBeInTheDocument();
    expect(screen.getByText("フリガナ")).toBeInTheDocument();
    expect(screen.getByText("郵便番号")).toBeInTheDocument();
    expect(screen.getByText("住所")).toBeInTheDocument();
    expect(screen.getByText("電話番号")).toBeInTheDocument();
    expect(screen.getByText("メールアドレス")).toBeInTheDocument();

    expect(screen.getByText("管理 太郎")).toBeInTheDocument();
    expect(screen.getByText("カンリ タロウ")).toBeInTheDocument();
    expect(screen.getByText("101-0022")).toBeInTheDocument();
    expect(
      screen.getByText("東京都千代田区神田練塀町300番地")
    ).toBeInTheDocument();
    expect(screen.getByText("090-1234-5678")).toBeInTheDocument();
    expect(screen.getByText("admin@example.com")).toBeInTheDocument();
  });

  test("編集リンクの遷移先が管理者情報編集画面である", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockAdminUserResponse(),
    });

    render(<AdminPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "管理者情報" })
      ).toBeInTheDocument();
    });

    expect(screen.getByRole("link", { name: "編集" })).toHaveAttribute(
      "href",
      "/admin/edit"
    );
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

    render(<AdminPage />);

    await waitFor(() => {
      expect(
        screen.getByText("このページを表示する権限がありません。")
      ).toBeInTheDocument();
    });

    expect(screen.getByRole("link", { name: "ホームへ戻る" })).toHaveAttribute(
      "href",
      "/"
    );
  });

  test("API失敗時、取得失敗メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<AdminPage />);

    await waitFor(() => {
      expect(
        screen.getByText("管理者情報の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<AdminPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("空項目は未登録として表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        isAuthenticated: true,
        user: {
          id: 1,
          name: "管理 太郎",
          kana: "",
          email: "admin@example.com",
          postalCode: "",
          address: "",
          phoneNumber: "",
          roles: ["Admin"],
        },
      }),
    });

    render(<AdminPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "管理者情報" })
      ).toBeInTheDocument();
    });

    expect(screen.getAllByText("未登録").length).toBeGreaterThan(0);
  });

  test("API呼び出しURLが正しい", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockAdminUserResponse(),
    });

    render(<AdminPage />);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/auth/me",
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      })
    );
  });
});