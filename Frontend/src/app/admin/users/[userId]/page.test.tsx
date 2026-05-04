import { render, screen, waitFor } from "@testing-library/react";
import AdminUserDetailPage from "./page";

const mockFetch = jest.fn();

jest.mock("next/navigation", () => ({
  useParams: () => ({
    userId: "1",
  }),
}));

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

function mockUserResponse() {
  return {
    id: 1,
    name: "山田 太郎",
    kana: "ヤマダ タロウ",
    postalCode: "101-0022",
    address: "東京都千代田区神田練塀町300番地",
    phoneNumber: "090-1234-5678",
    email: "user@example.com",
    usageType: "personal",
    roleName: "GeneralUser",
    roleLabel: "一般会員",
    enabled: true,
  };
}

describe("AdminUserDetailPage", () => {
  test("読み込み中メッセージを表示する", () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockUserResponse(),
    });

    render(<AdminUserDetailPage />);

    expect(screen.getByText("会員詳細を読み込み中...")).toBeInTheDocument();
  });

  test("API成功時、会員詳細を表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockUserResponse(),
    });

    render(<AdminUserDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "山田 太郎" })
      ).toBeInTheDocument();
    });

    expect(screen.getByText("ADMIN USER DETAIL")).toBeInTheDocument();

    expect(screen.getByText("有効")).toBeInTheDocument();
    expect(screen.getAllByText("一般会員").length).toBeGreaterThan(0);

    expect(screen.getByText("ID")).toBeInTheDocument();
    expect(screen.getByText("氏名")).toBeInTheDocument();
    expect(screen.getByText("フリガナ")).toBeInTheDocument();
    expect(screen.getByText("郵便番号")).toBeInTheDocument();
    expect(screen.getByText("住所")).toBeInTheDocument();
    expect(screen.getByText("電話番号")).toBeInTheDocument();
    expect(screen.getByText("メールアドレス")).toBeInTheDocument();
    expect(screen.getByText("利用区分")).toBeInTheDocument();
    expect(screen.getByText("属性")).toBeInTheDocument();

    expect(screen.getAllByText("1").length).toBeGreaterThan(0);
    expect(screen.getAllByText("山田 太郎").length).toBeGreaterThan(0);
    expect(screen.getByText("ヤマダ タロウ")).toBeInTheDocument();
    expect(screen.getByText("101-0022")).toBeInTheDocument();
    expect(
      screen.getByText("東京都千代田区神田練塀町300番地")
    ).toBeInTheDocument();
    expect(screen.getByText("090-1234-5678")).toBeInTheDocument();
    expect(screen.getByText("user@example.com")).toBeInTheDocument();
    expect(screen.getByText("個人利用")).toBeInTheDocument();
  });

  test("法人利用と無効ステータスを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockUserResponse(),
        usageType: "business",
        enabled: false,
        roleLabel: "スタジオ提供者",
      }),
    });

    render(<AdminUserDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "山田 太郎" })
      ).toBeInTheDocument();
    });

    expect(screen.getByText("法人・業務利用")).toBeInTheDocument();
    expect(screen.getByText("無効")).toBeInTheDocument();
    expect(screen.getAllByText("スタジオ提供者").length).toBeGreaterThan(0);
  });

  test("空項目は未登録として表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockUserResponse(),
        kana: "",
        postalCode: "",
        address: "",
        phoneNumber: "",
        usageType: "",
        roleLabel: "",
      }),
    });

    render(<AdminUserDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "山田 太郎" })
      ).toBeInTheDocument();
    });

    expect(screen.getAllByText("未登録").length).toBeGreaterThan(0);
    expect(screen.getAllByText("未設定").length).toBeGreaterThan(0);
  });

  test("会員一覧へ戻るリンクの遷移先が正しい", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockUserResponse(),
    });

    render(<AdminUserDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "山田 太郎" })
      ).toBeInTheDocument();
    });

    expect(
      screen.getByRole("link", { name: "会員一覧へ戻る" })
    ).toHaveAttribute("href", "/admin/users");
  });

  test("403の場合、管理者のみアクセス可能メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 403,
    });

    render(<AdminUserDetailPage />);

    await waitFor(() => {
      expect(screen.getByText("管理者のみアクセスできます。")).toBeInTheDocument();
    });

    expect(
      screen.getByRole("link", { name: "会員一覧へ戻る" })
    ).toHaveAttribute("href", "/admin/users");
  });

  test("404の場合、会員情報が見つからないメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 404,
    });

    render(<AdminUserDetailPage />);

    await waitFor(() => {
      expect(screen.getByText("会員情報が見つかりません。")).toBeInTheDocument();
    });

    expect(
      screen.getByRole("link", { name: "会員一覧へ戻る" })
    ).toHaveAttribute("href", "/admin/users");
  });

  test("API失敗時、取得失敗メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<AdminUserDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByText("会員詳細の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<AdminUserDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("API呼び出しURLが userId を含んでいる", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockUserResponse(),
    });

    render(<AdminUserDetailPage />);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/admin/users/1",
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      })
    );
  });
});