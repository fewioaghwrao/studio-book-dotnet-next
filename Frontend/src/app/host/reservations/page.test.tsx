import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import HostReservationsPage from "./page";

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

function mockReservationsResponse() {
  return {
    items: [
      {
        reservationId: 1,
        roomId: 101,
        roomName: "池袋ダンスルーム",
        guestName: "予約 太郎",
        startAt: "2026-05-04T10:00:00Z",
        endAt: "2026-05-04T12:00:00Z",
        amount: 6000,
        status: "booked",
      },
      {
        reservationId: 2,
        roomId: 102,
        roomName: "渋谷撮影スタジオ",
        guestName: "利用 花子",
        startAt: "2026-05-05T13:00:00Z",
        endAt: "2026-05-05T15:00:00Z",
        amount: 9000,
        status: "paid",
      },
      {
        reservationId: 3,
        roomId: 103,
        roomName: "新宿配信スタジオ",
        guestName: "取消 次郎",
        startAt: "2026-05-06T09:00:00Z",
        endAt: "2026-05-06T11:00:00Z",
        amount: 5000,
        status: "canceled",
      },
    ],
    page: 1,
    pageSize: 10,
    totalCount: 3,
    totalPages: 1,
    roomOptions: [
      {
        id: 101,
        name: "池袋ダンスルーム",
      },
      {
        id: 102,
        name: "渋谷撮影スタジオ",
      },
    ],
  };
}

describe("HostReservationsPage", () => {
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

    render(<HostReservationsPage />);

    expect(
      screen.getByRole("heading", { name: "予約一覧（スタジオ提供者）" })
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "自分が管理するスタジオの予約状況を確認し、予約の承認・キャンセルを行えます。"
      )
    ).toBeInTheDocument();

    expect(screen.getByText("予約一覧を読み込み中...")).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText("該当する予約はありません。")).toBeInTheDocument();
    });

    expect(screen.getByText("全 0 件")).toBeInTheDocument();
  });

  test("API成功時、予約一覧を表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockReservationsResponse(),
    });

    render(<HostReservationsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    });

    expect(screen.getAllByText("渋谷撮影スタジオ").length).toBeGreaterThan(0);
    expect(screen.getAllByText("新宿配信スタジオ").length).toBeGreaterThan(0);

    expect(screen.getAllByText("予約 太郎").length).toBeGreaterThan(0);
    expect(screen.getAllByText("利用 花子").length).toBeGreaterThan(0);
    expect(screen.getAllByText("取消 次郎").length).toBeGreaterThan(0);

    expect(screen.getAllByText("6,000円").length).toBeGreaterThan(0);
    expect(screen.getAllByText("9,000円").length).toBeGreaterThan(0);
    expect(screen.getAllByText("5,000円").length).toBeGreaterThan(0);

    expect(screen.getAllByText("予約済み").length).toBeGreaterThan(0);
    expect(screen.getAllByText("承認済み").length).toBeGreaterThan(0);
    expect(screen.getAllByText("キャンセル").length).toBeGreaterThan(0);

    expect(screen.getByText("全 3 件")).toBeInTheDocument();

    expect(
      screen.getByRole("option", { name: "池袋ダンスルーム" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("option", { name: "渋谷撮影スタジオ" })
    ).toBeInTheDocument();
  });

  test("403の場合、ホストユーザーのみアクセス可能メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 403,
    });

    render(<HostReservationsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("ホストユーザーのみアクセスできます。")
      ).toBeInTheDocument();
    });
  });

  test("API失敗時、エラーメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<HostReservationsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("予約一覧の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<HostReservationsPage />);

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
        roomOptions: [
          {
            id: 101,
            name: "池袋ダンスルーム",
          },
        ],
      }),
    });

    render(<HostReservationsPage />);

    await waitFor(() => {
      expect(screen.getByText("該当する予約はありません。")).toBeInTheDocument();
    });

    await user.type(
      screen.getByPlaceholderText("スタジオ名 / 予約者名"),
      "池袋"
    );

    await user.type(screen.getByPlaceholderText("完全一致"), "1");

    await user.selectOptions(screen.getByDisplayValue("すべて"), "paid");

    await user.selectOptions(
      screen.getByDisplayValue("すべてのスタジオ"),
      "101"
    );

    const dateInputs = document.querySelectorAll('input[type="date"]');

    await user.type(dateInputs[0], "2026-05-01");
    await user.type(dateInputs[1], "2026-05-31");

    await user.click(screen.getByRole("button", { name: "検索" }));

    expect(mockPush).toHaveBeenCalledWith(
      "/host/reservations?keyword=%E6%B1%A0%E8%A2%8B&reservationId=1&status=paid&roomId=101&startFrom=2026-05-01&startTo=2026-05-31&page=1&pageSize=10"
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

    render(<HostReservationsPage />);

    await waitFor(() => {
      expect(screen.getByText("該当する予約はありません。")).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "クリア" }));

    expect(mockPush).toHaveBeenCalledWith("/host/reservations");
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
            roomName: "2ページ目スタジオ",
            guestName: "予約者",
            startAt: "2026-05-04T10:00:00Z",
            endAt: "2026-05-04T12:00:00Z",
            amount: 6000,
            status: "booked",
          },
        ],
        page: 2,
        pageSize: 10,
        totalCount: 25,
        totalPages: 3,
        roomOptions: [],
      }),
    });

    render(<HostReservationsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("2ページ目スタジオ").length).toBeGreaterThan(0);
    });

    await user.click(screen.getByRole("button", { name: "次" }));

    expect(mockPush).toHaveBeenCalledWith(
      "/host/reservations?page=3&pageSize=10"
    );
  });

  test("承認ボタンを押すと承認APIを呼び、再取得する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          items: [
            {
              reservationId: 1,
              roomId: 101,
              roomName: "池袋ダンスルーム",
              guestName: "予約 太郎",
              startAt: "2026-05-04T10:00:00Z",
              endAt: "2026-05-04T12:00:00Z",
              amount: 6000,
              status: "booked",
            },
          ],
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
          roomOptions: [],
        }),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          items: [
            {
              reservationId: 1,
              roomId: 101,
              roomName: "池袋ダンスルーム",
              guestName: "予約 太郎",
              startAt: "2026-05-04T10:00:00Z",
              endAt: "2026-05-04T12:00:00Z",
              amount: 6000,
              status: "paid",
            },
          ],
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
          roomOptions: [],
        }),
      });

    render(<HostReservationsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    });

    await user.click(screen.getAllByRole("button", { name: "承認" })[0]);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(3);
    });

    expect(mockFetch.mock.calls[1][0]).toBe(
      "https://localhost:7226/api/host/reservations/1/approve"
    );

    expect(mockFetch.mock.calls[1][1]).toEqual(
      expect.objectContaining({
        method: "POST",
        credentials: "include",
      })
    );

    expect(screen.getByText("予約を承認しました。")).toBeInTheDocument();
  });

  test("承認API失敗時、APIから返されたエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          items: [
            {
              reservationId: 1,
              roomId: 101,
              roomName: "池袋ダンスルーム",
              guestName: "予約 太郎",
              startAt: "2026-05-04T10:00:00Z",
              endAt: "2026-05-04T12:00:00Z",
              amount: 6000,
              status: "booked",
            },
          ],
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
          roomOptions: [],
        }),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({
          message: "この予約は承認できません。",
        }),
      });

    render(<HostReservationsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    });

    await user.click(screen.getAllByRole("button", { name: "承認" })[0]);

    await waitFor(() => {
      expect(
        screen.getByText("この予約は承認できません。")
      ).toBeInTheDocument();
    });
  });

  test("キャンセル確認でOKの場合、キャンセルAPIを呼び、再取得する", async () => {
    const user = userEvent.setup();

    jest.spyOn(window, "confirm").mockReturnValueOnce(true);

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          items: [
            {
              reservationId: 1,
              roomId: 101,
              roomName: "池袋ダンスルーム",
              guestName: "予約 太郎",
              startAt: "2026-05-04T10:00:00Z",
              endAt: "2026-05-04T12:00:00Z",
              amount: 6000,
              status: "booked",
            },
          ],
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
          roomOptions: [],
        }),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
      })
      .mockResolvedValueOnce({
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

    render(<HostReservationsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    });

    await user.click(screen.getAllByRole("button", { name: "キャンセル" })[0]);

    expect(window.confirm).toHaveBeenCalledWith("本当にキャンセルしますか？");

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(3);
    });

    expect(mockFetch.mock.calls[1][0]).toBe(
      "https://localhost:7226/api/host/reservations/1/cancel"
    );

    expect(mockFetch.mock.calls[1][1]).toEqual(
      expect.objectContaining({
        method: "POST",
        credentials: "include",
      })
    );

    expect(screen.getByText("予約をキャンセルしました。")).toBeInTheDocument();
  });

  test("キャンセル確認でキャンセルした場合、APIを呼ばない", async () => {
    const user = userEvent.setup();

    jest.spyOn(window, "confirm").mockReturnValueOnce(false);

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        items: [
          {
            reservationId: 1,
            roomId: 101,
            roomName: "池袋ダンスルーム",
            guestName: "予約 太郎",
            startAt: "2026-05-04T10:00:00Z",
            endAt: "2026-05-04T12:00:00Z",
            amount: 6000,
            status: "booked",
          },
        ],
        page: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1,
        roomOptions: [],
      }),
    });

    render(<HostReservationsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    });

    await user.click(screen.getAllByRole("button", { name: "キャンセル" })[0]);

    expect(window.confirm).toHaveBeenCalledWith("本当にキャンセルしますか？");
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });
});