import { render, screen, waitFor } from "@testing-library/react";
import UserPage from "./page";

const mockFetch = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

function mockUserResponse() {
  return {
    isAuthenticated: true,
    user: {
      id: 1,
      name: "一般 太郎",
      kana: "イッパン タロウ",
      email: "user@example.com",
      postalCode: "123-4567",
      address: "東京都新宿区サンプル1-2-3",
      phoneNumber: "09012345678",
      usageType: "personal",
      roles: ["GeneralUser"],
    },
  };
}

function setupSuccessResponse() {
  mockFetch.mockResolvedValueOnce({
    ok: true,
    status: 200,
    json: async () => mockUserResponse(),
  });
}

async function waitForLoaded() {
  await waitFor(() => {
    expect(
      screen.getByRole("heading", { name: "会員情報" })
    ).toBeInTheDocument();
  });
}

describe("UserPage", () => {
  test("読み込み中メッセージを表示する", () => {
    setupSuccessResponse();

    render(<UserPage />);

    expect(screen.getByText("会員情報を読み込み中...")).toBeInTheDocument();
  });

  test("API成功時、会員情報を表示する", async () => {
    setupSuccessResponse();

    render(<UserPage />);

    await waitForLoaded();

    expect(screen.getByText("MY PAGE")).toBeInTheDocument();

    expect(
      screen.getByText("登録されている会員情報を確認できます。")
    ).toBeInTheDocument();

    expect(screen.getByText("氏名")).toBeInTheDocument();
    expect(screen.getByText("一般 太郎")).toBeInTheDocument();

    expect(screen.getByText("フリガナ")).toBeInTheDocument();
    expect(screen.getByText("イッパン タロウ")).toBeInTheDocument();

    expect(screen.getByText("郵便番号")).toBeInTheDocument();
    expect(screen.getByText("123-4567")).toBeInTheDocument();

    expect(screen.getByText("住所")).toBeInTheDocument();
    expect(screen.getByText("東京都新宿区サンプル1-2-3")).toBeInTheDocument();

    expect(screen.getByText("電話番号")).toBeInTheDocument();
    expect(screen.getByText("09012345678")).toBeInTheDocument();

    expect(screen.getByText("メールアドレス")).toBeInTheDocument();
    expect(screen.getByText("user@example.com")).toBeInTheDocument();
  });

  test("未登録項目がある場合、未登録を表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockUserResponse(),
        user: {
          ...mockUserResponse().user,
          kana: "",
          postalCode: "",
          address: "",
          phoneNumber: "",
        },
      }),
    });

    render(<UserPage />);

    await waitForLoaded();

    expect(screen.getAllByText("未登録").length).toBeGreaterThanOrEqual(4);
  });

  test("パンくずと編集リンクを表示する", async () => {
    setupSuccessResponse();

    render(<UserPage />);

    await waitForLoaded();

    expect(screen.getByRole("link", { name: "ホーム" })).toHaveAttribute(
      "href",
      "/"
    );

    expect(screen.getByRole("link", { name: "編集" })).toHaveAttribute(
      "href",
      "/user/edit"
    );

    expect(screen.getAllByText("会員情報").length).toBeGreaterThan(0);
  });

  test("API失敗時、エラーメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<UserPage />);

    await waitFor(() => {
      expect(
        screen.getByText("会員情報の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<UserPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("API呼び出し内容が正しい", async () => {
    setupSuccessResponse();

    render(<UserPage />);

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