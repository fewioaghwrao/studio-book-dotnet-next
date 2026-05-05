import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import HostBusinessHoursPage from "./page";

const mockFetch = jest.fn();
const mockPush = jest.fn();

jest.mock("next/navigation", () => ({
  useParams: () => ({
    id: "1",
  }),
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

function mockBusinessHoursResponse() {
  return {
    roomId: 1,
    roomName: "池袋ダンスルーム",
    rows: [
      {
        dayOfWeek: 1,
        startTime: "09:00:00",
        endTime: "18:00:00",
        isHoliday: false,
      },
      {
        dayOfWeek: 2,
        startTime: "10:00:00",
        endTime: "19:00:00",
        isHoliday: false,
      },
      {
        dayOfWeek: 3,
        startTime: null,
        endTime: null,
        isHoliday: true,
      },
    ],
  };
}

describe("HostBusinessHoursPage", () => {
  test("読み込み中メッセージを表示する", () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockBusinessHoursResponse(),
    });

    render(<HostBusinessHoursPage />);

    expect(screen.getByText("営業時間を読み込み中...")).toBeInTheDocument();
  });

  test("API成功時、営業時間設定画面を表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockBusinessHoursResponse(),
    });

    render(<HostBusinessHoursPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", {
          name: "池袋ダンスルーム の営業時間設定",
        })
      ).toBeInTheDocument();
    });

    expect(screen.getByText("BUSINESS HOURS")).toBeInTheDocument();

    expect(
      screen.getByText(
        "曜日ごとの営業開始・終了時刻を設定できます。 休みを選択した曜日は予約受付対象外になります。"
      )
    ).toBeInTheDocument();

    expect(screen.getAllByText("月曜").length).toBeGreaterThan(0);
    expect(screen.getAllByText("火曜").length).toBeGreaterThan(0);
    expect(screen.getAllByText("水曜").length).toBeGreaterThan(0);

    expect(screen.getAllByDisplayValue("09:00").length).toBeGreaterThan(0);
    expect(screen.getAllByDisplayValue("18:00").length).toBeGreaterThan(0);
    expect(screen.getAllByDisplayValue("10:00").length).toBeGreaterThan(0);
    expect(screen.getAllByDisplayValue("19:00").length).toBeGreaterThan(0);
  });

  test("403の場合、ホストユーザーのみアクセス可能メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 403,
    });

    render(<HostBusinessHoursPage />);

    await waitFor(() => {
      expect(
        screen.getByText("ホストユーザーのみアクセスできます。")
      ).toBeInTheDocument();
    });
  });

  test("404の場合、スタジオが見つからないメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 404,
    });

    render(<HostBusinessHoursPage />);

    await waitFor(() => {
      expect(screen.getByText("スタジオが見つかりません。")).toBeInTheDocument();
    });
  });

  test("API失敗時、取得失敗メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<HostBusinessHoursPage />);

    await waitFor(() => {
      expect(
        screen.getByText("営業時間の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<HostBusinessHoursPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("休みにすると開始時刻と終了時刻が空になり、select が無効になる", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockBusinessHoursResponse(),
    });

    render(<HostBusinessHoursPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", {
          name: "池袋ダンスルーム の営業時間設定",
        })
      ).toBeInTheDocument();
    });

    const holidayCheckboxes = screen.getAllByRole("checkbox", { name: "休み" });

    await user.click(holidayCheckboxes[0]);

    const selects = screen.getAllByRole("combobox");

    expect(selects[0]).toBeDisabled();
    expect(selects[1]).toBeDisabled();
  });

  test("開始時刻未選択の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        roomId: 1,
        roomName: "池袋ダンスルーム",
        rows: [
          {
            dayOfWeek: 1,
            startTime: null,
            endTime: "18:00:00",
            isHoliday: false,
          },
        ],
      }),
    });

    render(<HostBusinessHoursPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", {
          name: "池袋ダンスルーム の営業時間設定",
        })
      ).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "保存する" }));

    expect(
      screen.getByText("月曜の開始時刻を選択してください。")
    ).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("終了時刻未選択の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        roomId: 1,
        roomName: "池袋ダンスルーム",
        rows: [
          {
            dayOfWeek: 1,
            startTime: "09:00:00",
            endTime: null,
            isHoliday: false,
          },
        ],
      }),
    });

    render(<HostBusinessHoursPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", {
          name: "池袋ダンスルーム の営業時間設定",
        })
      ).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "保存する" }));

    expect(
      screen.getByText("月曜の終了時刻を選択してください。")
    ).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("終了時刻が開始時刻以前の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        roomId: 1,
        roomName: "池袋ダンスルーム",
        rows: [
          {
            dayOfWeek: 1,
            startTime: "18:00:00",
            endTime: "09:00:00",
            isHoliday: false,
          },
        ],
      }),
    });

    render(<HostBusinessHoursPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", {
          name: "池袋ダンスルーム の営業時間設定",
        })
      ).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "保存する" }));

    expect(
      screen.getByText("月曜の終了時刻は開始時刻より後にしてください。")
    ).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("正常入力で保存するとPUTリクエストを送信する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockBusinessHoursResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
      });

    render(<HostBusinessHoursPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", {
          name: "池袋ダンスルーム の営業時間設定",
        })
      ).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "保存する" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch).toHaveBeenLastCalledWith(
      "https://localhost:7226/api/host/rooms/1/business-hours",
      expect.objectContaining({
        method: "PUT",        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          rows: [
            {
              dayOfWeek: 1,
              startTime: "09:00",
              endTime: "18:00",
              isHoliday: false,
            },
            {
              dayOfWeek: 2,
              startTime: "10:00",
              endTime: "19:00",
              isHoliday: false,
            },
            {
              dayOfWeek: 3,
              startTime: null,
              endTime: null,
              isHoliday: true,
            },
          ],
        }),
      })
    );

    expect(screen.getByText("営業時間を保存しました。")).toBeInTheDocument();
  });

  test("PUTで403の場合、ホストユーザーのみアクセス可能メッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockBusinessHoursResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 403,
      });

    render(<HostBusinessHoursPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", {
          name: "池袋ダンスルーム の営業時間設定",
        })
      ).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "保存する" }));

    await waitFor(() => {
      expect(
        screen.getByText("ホストユーザーのみアクセスできます。")
      ).toBeInTheDocument();
    });
  });

  test("PUT失敗時、APIから返されたエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockBusinessHoursResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({
          message: "営業時間の設定値が不正です。",
        }),
      });

    render(<HostBusinessHoursPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", {
          name: "池袋ダンスルーム の営業時間設定",
        })
      ).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "保存する" }));

    await waitFor(() => {
      expect(
        screen.getByText("営業時間の設定値が不正です。")
      ).toBeInTheDocument();
    });
  });

  test("PUT通信エラー時、通信エラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockBusinessHoursResponse(),
      })
      .mockRejectedValueOnce(new Error("Network Error"));

    render(<HostBusinessHoursPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", {
          name: "池袋ダンスルーム の営業時間設定",
        })
      ).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "保存する" }));

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("詳細へ戻るボタンを押すと詳細画面へ遷移する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockBusinessHoursResponse(),
    });

    render(<HostBusinessHoursPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", {
          name: "池袋ダンスルーム の営業時間設定",
        })
      ).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "詳細へ戻る" }));

    expect(mockPush).toHaveBeenCalledWith("/host/rooms/1");
  });

  test("API呼び出しURLが正しい", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockBusinessHoursResponse(),
    });

    render(<HostBusinessHoursPage />);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/host/rooms/1/business-hours",
      expect.objectContaining({
        method: "GET",        cache: "no-store",
      })
    );
  });
});