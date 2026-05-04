import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import StudioBookTopPage from "./page";

const mockFetch = jest.fn();
const mockPush = jest.fn();

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mockPush,
  }),
}));

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();
  mockPush.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

describe("StudioBookTopPage", () => {
  test("トップページの基本文言を表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        popularRooms: [],
        newRooms: [],
      }),
    });

    render(<StudioBookTopPage />);

    expect(
      screen.getByText(/架空のスタジオ予約サイトを、/)
    ).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "スタジオ一覧を見る" }))
      .toHaveAttribute("href", "/rooms");

    expect(screen.getByRole("link", { name: "会員登録" }))
      .toHaveAttribute("href", "/signup");

    await waitFor(() => {
      expect(
        screen.getByText("人気のスタジオはまだありません。")
      ).toBeInTheDocument();
    });
  });

  test("API成功時、人気スタジオと新着スタジオを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        popularRooms: [
          {
            id: 1,
            name: "池袋ダンスルーム",
            address: "東京都豊島区",
            price: 3000,
            imageName: "room01.jpg",
            averageScore: 4.5,
            reviewCount: 12,
          },
        ],
        newRooms: [
          {
            id: 2,
            name: "渋谷撮影スタジオ",
            address: "東京都渋谷区",
            price: 4500,
            imageName: null,
            averageScore: null,
            reviewCount: 0,
          },
        ],
      }),
    });

    render(<StudioBookTopPage />);

    expect(screen.getByText("スタジオ情報を読み込み中..."))
      .toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText("池袋ダンスルーム")).toBeInTheDocument();
    });

    expect(screen.getByText("渋谷撮影スタジオ")).toBeInTheDocument();
    expect(screen.getByText("東京都豊島区")).toBeInTheDocument();
    expect(screen.getByText("東京都渋谷区")).toBeInTheDocument();
    expect(screen.getByText("3,000円 / h")).toBeInTheDocument();
    expect(screen.getByText("4,500円 / h")).toBeInTheDocument();
    expect(screen.getByText("4.5 / 5")).toBeInTheDocument();
    expect(screen.getByText("（12件）")).toBeInTheDocument();
    expect(screen.getByText("レビューなし")).toBeInTheDocument();
  });

  test("API失敗時、エラーメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<StudioBookTopPage />);

    await waitFor(() => {
      expect(
        screen.getByText("トップページ情報の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<StudioBookTopPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("キーワード未入力で検索するとスタジオ一覧へ遷移する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        popularRooms: [],
        newRooms: [],
      }),
    });

    render(<StudioBookTopPage />);

    await user.click(screen.getByRole("button", { name: "検索" }));

    expect(mockPush).toHaveBeenCalledWith("/rooms");
  });

  test("キーワード入力ありで検索するとクエリ付きで遷移する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        popularRooms: [],
        newRooms: [],
      }),
    });

    render(<StudioBookTopPage />);

    await user.type(
      screen.getByPlaceholderText("スタジオ名・住所で検索"),
      "池袋 ダンス"
    );

    await user.click(screen.getByRole("button", { name: "検索" }));

    expect(mockPush).toHaveBeenCalledWith(
      "/rooms?keyword=%E6%B1%A0%E8%A2%8B%20%E3%83%80%E3%83%B3%E3%82%B9"
    );
  });
});