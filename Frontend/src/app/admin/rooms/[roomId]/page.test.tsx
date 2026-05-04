import { render, screen, waitFor } from "@testing-library/react";
import AdminRoomDetailPage from "./page";

const mockFetch = jest.fn();

jest.mock("next/navigation", () => ({
  useParams: () => ({
    roomId: "1",
  }),
}));

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

function mockRoomResponse() {
  return {
    id: 1,
    userId: 10,
    hostName: "ホスト太郎",
    name: "池袋ダンスルーム",
    imageName: "room01.jpg",
    description: "駅近のダンス向けスタジオです。",
    price: 3000,
    capacity: 6,
    postalCode: "170-0013",
    address: "東京都豊島区東池袋",
  };
}

describe("AdminRoomDetailPage", () => {
  test("読み込み中メッセージを表示する", () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockRoomResponse(),
    });

    render(<AdminRoomDetailPage />);

    expect(
      screen.getByText("スタジオ基本情報を読み込み中...")
    ).toBeInTheDocument();
  });

  test("API成功時、スタジオ基本情報を表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockRoomResponse(),
    });

    render(<AdminRoomDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "池袋ダンスルーム" })
      ).toBeInTheDocument();
    });

    expect(
      screen.getByText("スタジオの基本情報を確認できます。")
    ).toBeInTheDocument();

    expect(screen.getByAltText("池袋ダンスルーム")).toHaveAttribute(
      "src",
      "/storage/room01.jpg"
    );

    expect(screen.getByText("ID")).toBeInTheDocument();
    expect(screen.getByText("スタジオ名")).toBeInTheDocument();
    expect(screen.getByText("説明")).toBeInTheDocument();
    expect(screen.getByText("基本料金")).toBeInTheDocument();
    expect(screen.getByText("最大定員")).toBeInTheDocument();
    expect(screen.getByText("郵便番号")).toBeInTheDocument();
    expect(screen.getByText("住所")).toBeInTheDocument();
    expect(screen.getByText("スタジオ提供者")).toBeInTheDocument();

    expect(screen.getAllByText("1").length).toBeGreaterThan(0);
    expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    expect(
      screen.getByText("駅近のダンス向けスタジオです。")
    ).toBeInTheDocument();
    expect(screen.getByText("￥3,000")).toBeInTheDocument();
    expect(screen.getByText("6人")).toBeInTheDocument();
    expect(screen.getByText("170-0013")).toBeInTheDocument();
    expect(screen.getByText("東京都豊島区東池袋")).toBeInTheDocument();
    expect(screen.getByText("ホスト太郎")).toBeInTheDocument();
  });

  test("編集リンクと戻るリンクの遷移先が正しい", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockRoomResponse(),
    });

    render(<AdminRoomDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "池袋ダンスルーム" })
      ).toBeInTheDocument();
    });

    expect(screen.getByRole("link", { name: "編集" })).toHaveAttribute(
      "href",
      "/admin/rooms/1/edit"
    );

    expect(
      screen.getByRole("link", { name: "スタジオ一覧へ戻る" })
    ).toHaveAttribute("href", "/admin/rooms");
  });

  test("画像名が空の場合、noImage を表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockRoomResponse(),
        imageName: "",
      }),
    });

    render(<AdminRoomDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "池袋ダンスルーム" })
      ).toBeInTheDocument();
    });

    expect(screen.getByAltText("池袋ダンスルーム")).toHaveAttribute(
      "src",
      "/images/noImage.png"
    );
  });

  test("空項目は未登録として表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockRoomResponse(),
        description: "",
        hostName: "",
      }),
    });

    render(<AdminRoomDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "池袋ダンスルーム" })
      ).toBeInTheDocument();
    });

    expect(screen.getAllByText("未登録").length).toBeGreaterThan(0);
  });

  test("403の場合、管理者のみアクセス可能メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 403,
    });

    render(<AdminRoomDetailPage />);

    await waitFor(() => {
      expect(screen.getByText("管理者のみアクセスできます。")).toBeInTheDocument();
    });

    expect(
      screen.getByRole("link", { name: "スタジオ一覧へ戻る" })
    ).toHaveAttribute("href", "/admin/rooms");
  });

  test("404の場合、スタジオが見つからないメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 404,
    });

    render(<AdminRoomDetailPage />);

    await waitFor(() => {
      expect(screen.getByText("スタジオが見つかりません。")).toBeInTheDocument();
    });

    expect(
      screen.getByRole("link", { name: "スタジオ一覧へ戻る" })
    ).toHaveAttribute("href", "/admin/rooms");
  });

  test("API失敗時、取得失敗メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<AdminRoomDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByText("スタジオ基本情報の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<AdminRoomDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("API呼び出しURLが roomId を含んでいる", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockRoomResponse(),
    });

    render(<AdminRoomDetailPage />);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/admin/rooms/1",
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      })
    );
  });
});