import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import AdminReservationsPage from "./page";

const mockFetch = jest.fn();
const mockPush = jest.fn();

let mockSearchParams = new URLSearchParams();

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mockPush,
  }),
  useSearchParams: () => mockSearchParams,
}));

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();
  mockPush.mockReset();

  mockSearchParams = new URLSearchParams();

  global.fetch = mockFetch as jest.Mock;
});

describe("AdminReservationsPage", () => {
  test("予約一覧の基本表示を行う", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        items: [],
        page: 1,
        pageSize: 10,
        totalCount: 0,
        totalPages: 1,
        roomOptions: [],
      }),
    });

    render(<AdminReservationsPage />);

    expect(
      screen.getByRole("heading", { name: "予約一覧（管理者）" })
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "全スタジオの予約状況を検索・確認できます。公開デモでは誤操作防止のため、予約状態の変更操作は提供していません。"
      )
    ).toBeInTheDocument();

    expect(screen.getByText("予約一覧を読み込み中...")).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText("該当する予約はありません。")).toBeInTheDocument();
    });

    expect(screen.getByText("全 0 件")).toBeInTheDocument();
  });

  test("API成功時、予約一覧とスタジオ選択肢を表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        items: [
          {
            reservationId: 1,
            roomId: 101,
            roomName: "池袋ダンスルーム",
            hostUserId: 201,
            hostName: "ホスト太郎",
            guestUserId: 301,
            guestName: "予約花子",
            startAt: "2026-05-04T10:00:00Z",
            endAt: "2026-05-04T12:00:00Z",
            amount: 6000,
            status: "booked",
          },
          {
            reservationId: 2,
            roomId: 102,
            roomName: "渋谷撮影スタジオ",
            hostUserId: 202,
            hostName: "",
            guestUserId: 302,
            guestName: "利用次郎",
            startAt: "2026-05-05T13:00:00Z",
            endAt: "2026-05-05T15:00:00Z",
            amount: 9000,
            status: "paid",
          },
        ],
        page: 1,
        pageSize: 10,
        totalCount: 2,
        totalPages: 1,
        roomOptions: [
          { id: 101, name: "池袋ダンスルーム" },
          { id: 102, name: "渋谷撮影スタジオ" },
        ],
      }),
    });

    render(<AdminReservationsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    });

    expect(screen.getAllByText("渋谷撮影スタジオ").length).toBeGreaterThan(0);
    expect(screen.getAllByText("ホスト太郎").length).toBeGreaterThan(0);
    expect(screen.getAllByText("予約花子").length).toBeGreaterThan(0);
    expect(screen.getAllByText("利用次郎").length).toBeGreaterThan(0);
    expect(screen.getAllByText("未設定").length).toBeGreaterThan(0);
    expect(screen.getAllByText("予約済み").length).toBeGreaterThan(0);
    expect(screen.getAllByText("承認済み").length).toBeGreaterThan(0);
    expect(screen.getAllByText("￥6,000").length).toBeGreaterThan(0);
    expect(screen.getAllByText("￥9,000").length).toBeGreaterThan(0);
    expect(screen.getByText("全 2 件")).toBeInTheDocument();

    expect(screen.getAllByRole("option", { name: "池袋ダンスルーム" }).length)
      .toBeGreaterThan(0);
    expect(screen.getAllByRole("option", { name: "渋谷撮影スタジオ" }).length)
      .toBeGreaterThan(0);
  });

  test("403の場合、管理者のみアクセス可能メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 403,
    });

    render(<AdminReservationsPage />);

    await waitFor(() => {
      expect(screen.getByText("管理者のみアクセスできます。")).toBeInTheDocument();
    });
  });

  test("API失敗時、エラーメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<AdminReservationsPage />);

    await waitFor(() => {
      expect(screen.getByText("予約一覧の取得に失敗しました。")).toBeInTheDocument();
    });
  });

  test("通信エラー時、エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<AdminReservationsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("検索条件を入力して検索すると、クエリ付きURLへ遷移する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        items: [],
        page: 1,
        pageSize: 10,
        totalCount: 0,
        totalPages: 1,
        roomOptions: [{ id: 101, name: "池袋ダンスルーム" }],
      }),
    });

    render(<AdminReservationsPage />);

    await waitFor(() => {
      expect(screen.getByText("該当する予約はありません。")).toBeInTheDocument();
    });

    await user.type(
      screen.getByPlaceholderText("スタジオ名 / 予約者名 / 提供者名"),
      "池袋"
    );
    await user.type(screen.getByPlaceholderText("完全一致"), "1");

    await user.selectOptions(screen.getByDisplayValue("すべて"), "paid");
    await user.selectOptions(
      screen.getByDisplayValue("すべてのスタジオ"),
      "101"
    );

    await user.click(screen.getByRole("button", { name: "検索" }));

    expect(mockPush).toHaveBeenCalledWith(
      "/admin/reservations?keyword=%E6%B1%A0%E8%A2%8B&reservationId=1&status=paid&roomId=101&page=1&pageSize=10"
    );
  });

  test("クリアボタンを押すと予約一覧へ戻る", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        items: [],
        page: 1,
        pageSize: 10,
        totalCount: 0,
        totalPages: 1,
        roomOptions: [],
      }),
    });

    render(<AdminReservationsPage />);

    await waitFor(() => {
      expect(screen.getByText("該当する予約はありません。")).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "クリア" }));

    expect(mockPush).toHaveBeenCalledWith("/admin/reservations");
  });

  test("ページングボタンを押すと指定ページへ遷移する", async () => {
    const user = userEvent.setup();

    mockSearchParams = new URLSearchParams("page=2");

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        items: [
          {
            reservationId: 11,
            roomId: 101,
            roomName: "検索用スタジオ",
            hostUserId: 201,
            hostName: "ホスト",
            guestUserId: 301,
            guestName: "予約者",
            startAt: "2026-05-04T10:00:00Z",
            endAt: "2026-05-04T12:00:00Z",
            amount: 6000,
            status: "canceled",
          },
        ],
        page: 2,
        pageSize: 10,
        totalCount: 25,
        totalPages: 3,
        roomOptions: [],
      }),
    });

    render(<AdminReservationsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("検索用スタジオ").length).toBeGreaterThan(0);
    });

    await user.click(screen.getByRole("button", { name: "次" }));

    expect(mockPush).toHaveBeenCalledWith(
      "/admin/reservations?page=3&pageSize=10"
    );
  });
});