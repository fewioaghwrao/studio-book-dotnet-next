import { render, screen, waitFor } from "@testing-library/react";
import HostPage from "./page";

const mockFetch = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

function mockHostUserResponse() {
  return {
    isAuthenticated: true,
    user: {
      id: 1,
      name: "ホスト 太郎",
      kana: "ホスト タロウ",
      email: "host@example.com",
      postalCode: "101-0022",
      address: "東京都千代田区神田練塀町300番地",
      phoneNumber: "090-1234-5678",
      usageType: "business",
      roles: ["Host"],
    },
  };
}

describe("HostPage", () => {
  test("読み込み中メッセージを表示する", () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockHostUserResponse(),
    });

    render(<HostPage />);

    expect(screen.getByText("会員情報を読み込み中...")).toBeInTheDocument();
  });

  test("ホスト会員情報を取得して表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockHostUserResponse(),
    });

    render(<HostPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "会員情報（ホスト）" })
      ).toBeInTheDocument();
    });

    expect(screen.getByText("HOST MY PAGE")).toBeInTheDocument();

    expect(
      screen.getByText("スタジオ提供者として登録されている会員情報を確認できます。")
    ).toBeInTheDocument();

    expect(screen.getByText("氏名")).toBeInTheDocument();
    expect(screen.getByText("フリガナ")).toBeInTheDocument();
    expect(screen.getByText("郵便番号")).toBeInTheDocument();
    expect(screen.getByText("住所")).toBeInTheDocument();
    expect(screen.getByText("電話番号")).toBeInTheDocument();
    expect(screen.getByText("メールアドレス")).toBeInTheDocument();

    expect(screen.getByText("ホスト 太郎")).toBeInTheDocument();
    expect(screen.getByText("ホスト タロウ")).toBeInTheDocument();
    expect(screen.getByText("101-0022")).toBeInTheDocument();
    expect(
      screen.getByText("東京都千代田区神田練塀町300番地")
    ).toBeInTheDocument();
    expect(screen.getByText("090-1234-5678")).toBeInTheDocument();
    expect(screen.getByText("host@example.com")).toBeInTheDocument();
  });

  test("編集リンクの遷移先がホスト情報編集画面である", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockHostUserResponse(),
    });

    render(<HostPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "会員情報（ホスト）" })
      ).toBeInTheDocument();
    });

    expect(screen.getByRole("link", { name: "編集" })).toHaveAttribute(
      "href",
      "/host/edit"
    );
  });

  test("API失敗時、取得失敗メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<HostPage />);

    await waitFor(() => {
      expect(
        screen.getByText("会員情報の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<HostPage />);

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
          name: "ホスト 太郎",
          kana: "",
          email: "host@example.com",
          postalCode: "",
          address: "",
          phoneNumber: "",
          usageType: "business",
          roles: ["Host"],
        },
      }),
    });

    render(<HostPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "会員情報（ホスト）" })
      ).toBeInTheDocument();
    });

    expect(screen.getAllByText("未登録").length).toBeGreaterThan(0);
  });

  test("API呼び出しURLが正しい", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockHostUserResponse(),
    });

    render(<HostPage />);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/auth/me",
      expect.objectContaining({
        method: "GET",        cache: "no-store",
      })
    );
  });
});