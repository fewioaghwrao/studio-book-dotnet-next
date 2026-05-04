import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import AdminStatusPage from "./page";

const mockFetch = jest.fn();

jest.mock("recharts", () => ({
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="responsive-container">{children}</div>
  ),
  BarChart: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="bar-chart">{children}</div>
  ),
  CartesianGrid: () => <div data-testid="cartesian-grid" />,
  Legend: () => <div data-testid="legend" />,
  Tooltip: () => <div data-testid="tooltip" />,
  XAxis: () => <div data-testid="x-axis" />,
  YAxis: () => <div data-testid="y-axis" />,
  Bar: ({ name }: { name?: string }) => <div>{name}</div>,
  Line: ({ name }: { name?: string }) => <div>{name}</div>,
}));

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

function mockStatusResponse() {
  return {
    labels: ["2026年3月", "2026年4月", "2026年5月"],
    booked: [10000, 20000, 30000],
    paid: [8000, 16000, 24000],
    utilizationPercents: [30.5, 42.25, null],
    reviewAvgAny: 4.26,
    reviewAvgPublic: 4.04,
    totalRoomCount: 23,
    totalHostCount: 2,
    totalReservationCount: 120,
    totalPaidAmount: 480000,
    roomOptions: [
      {
        id: 1,
        name: "池袋ダンスルーム",
      },
      {
        id: 2,
        name: "渋谷撮影スタジオ",
      },
    ],
  };
}

describe("AdminStatusPage", () => {
  test("読み込み中メッセージを表示する", () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockStatusResponse(),
    });

    render(<AdminStatusPage />);

    expect(screen.getByText("データを読み込み中...")).toBeInTheDocument();
  });

  test("API成功時、データ一覧を表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockStatusResponse(),
    });

    render(<AdminStatusPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "データ一覧" })
      ).toBeInTheDocument();
    });

    expect(
      screen.getByText("全スタジオを対象に、売上・稼働率・レビュー平均を確認できます。")
    ).toBeInTheDocument();

    expect(screen.getByText("登録スタジオ数")).toBeInTheDocument();
    expect(screen.getByText("23件")).toBeInTheDocument();

    expect(screen.getByText("ホスト数")).toBeInTheDocument();
    expect(screen.getByText("2人")).toBeInTheDocument();

    expect(screen.getByText("予約件数")).toBeInTheDocument();
    expect(screen.getByText("120件")).toBeInTheDocument();

    expect(screen.getByText("確定手数料売上合計")).toBeInTheDocument();
    expect(screen.getByText("￥480,000")).toBeInTheDocument();

    expect(
      screen.getByText("手数料売上一覧（表示基準月を含む3か月）")
    ).toBeInTheDocument();

    expect(screen.getByText("見込み手数料売上")).toBeInTheDocument();
    expect(screen.getByText("確定手数料売上")).toBeInTheDocument();

    expect(screen.getByText("稼働率（直近3か月）")).toBeInTheDocument();
    expect(screen.getByText("30.5%")).toBeInTheDocument();
    expect(screen.getByText("42.3%")).toBeInTheDocument();
    expect(screen.getByText("—")).toBeInTheDocument();

    expect(screen.getByText("レビュー")).toBeInTheDocument();
    expect(screen.getByText("平均レビュー（公開・非公開すべて）")).toBeInTheDocument();
    expect(screen.getByText("平均レビュー（公開のみ）")).toBeInTheDocument();
    expect(screen.getByText("4.3")).toBeInTheDocument();
    expect(screen.getByText("4.0")).toBeInTheDocument();

    expect(screen.getByRole("option", { name: "池袋ダンスルーム" })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "渋谷撮影スタジオ" })).toBeInTheDocument();

    expect(screen.getByTestId("bar-chart")).toBeInTheDocument();
  });

  test("403の場合、管理者のみアクセス可能メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 403,
    });

    render(<AdminStatusPage />);

    await waitFor(() => {
      expect(screen.getByText("管理者のみアクセスできます。")).toBeInTheDocument();
    });
  });

  test("API失敗時、エラーメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<AdminStatusPage />);

    await waitFor(() => {
      expect(
        screen.getByText("データ一覧の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<AdminStatusPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("対象スタジオを変更するとroomId付きで再取得する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockStatusResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          ...mockStatusResponse(),
          totalReservationCount: 10,
        }),
      });

    render(<AdminStatusPage />);

    await waitFor(() => {
      expect(screen.getByRole("option", { name: "池袋ダンスルーム" })).toBeInTheDocument();
    });

    await user.selectOptions(
      screen.getByDisplayValue("全体（すべてのスタジオ）"),
      "1"
    );

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch).toHaveBeenLastCalledWith(
      expect.stringContaining("/api/admin/status?roomId=1&year="),
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      })
    );

    expect(mockFetch).toHaveBeenLastCalledWith(
      expect.stringContaining("&month="),
      expect.any(Object)
    );
  });

  test("前月ボタンを押すと前月データを再取得する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockStatusResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockStatusResponse(),
      });

    render(<AdminStatusPage />);

    await waitFor(() => {
      expect(screen.getByText("23件")).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "前月" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch.mock.calls[1][0]).toContain("/api/admin/status?");
    expect(mockFetch.mock.calls[1][0]).toContain("year=");
    expect(mockFetch.mock.calls[1][0]).toContain("month=");
  });

  test("次月ボタンを押すと次月データを再取得する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockStatusResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockStatusResponse(),
      });

    render(<AdminStatusPage />);

    await waitFor(() => {
      expect(screen.getByText("23件")).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "次月" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch.mock.calls[1][0]).toContain("/api/admin/status?");
    expect(mockFetch.mock.calls[1][0]).toContain("year=");
    expect(mockFetch.mock.calls[1][0]).toContain("month=");
  });

  test("データがnullの場合、データなしメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => null,
    });

    render(<AdminStatusPage />);

    await waitFor(() => {
      expect(screen.getByText("データがありません。")).toBeInTheDocument();
    });
  });
});