import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import HostStatusPage from "./page";

const mockFetch = jest.fn();

jest.mock("recharts", () => ({
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="responsive-container">{children}</div>
  ),
  BarChart: ({
    children,
    data,
  }: {
    children: React.ReactNode;
    data?: Array<{ label: string; booked: number; paid: number }>;
  }) => (
    <div data-testid="bar-chart">
      <div>chart rows: {data?.length ?? 0}</div>
      {data?.map((row) => (
        <div key={row.label}>
          {row.label} / booked: {row.booked} / paid: {row.paid}
        </div>
      ))}
      {children}
    </div>
  ),
  CartesianGrid: () => <div data-testid="cartesian-grid" />,
  Legend: () => <div data-testid="legend" />,
  XAxis: () => <div data-testid="x-axis" />,
  YAxis: () => <div data-testid="y-axis" />,
  Tooltip: () => <div data-testid="tooltip" />,
  Bar: ({ name }: { name?: string }) => <div data-testid="bar">{name}</div>,
  Line: ({ name }: { name?: string }) => <div data-testid="line">{name}</div>,
}));

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;

  jest.useFakeTimers();
  jest.setSystemTime(new Date("2026-05-04T00:00:00+09:00"));
});

afterEach(() => {
  jest.useRealTimers();
});

function mockStatusResponse() {
  return {
    labels: ["2026年3月", "2026年4月", "2026年5月"],
    booked: [100000, 120000, 150000],
    paid: [80000, 110000, 130000],
    utilizationPercents: [45.25, 62.34, null],
    reviewAvgAny: 4.26,
    reviewAvgPublic: 4.74,
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

function setupSuccessInitialRender() {
  mockFetch.mockResolvedValueOnce({
    ok: true,
    status: 200,
    json: async () => mockStatusResponse(),
  });

  render(<HostStatusPage />);
}

async function waitForLoaded() {
  await waitFor(() => {
    expect(screen.getByText("売上一覧（表示基準月を含む3か月）"))
      .toBeInTheDocument();
  });
}

describe("HostStatusPage", () => {
  test("初期表示と読み込み中メッセージを表示する", () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockStatusResponse(),
    });

    render(<HostStatusPage />);

    expect(
      screen.getByRole("heading", { name: "統計一覧" })
    ).toBeInTheDocument();

    expect(screen.getByText("HOST STATUS")).toBeInTheDocument();

    expect(
      screen.getByText(
        "表示基準月を含む3か月分の売上、稼働率、レビュー平均を確認できます。"
      )
    ).toBeInTheDocument();

    expect(screen.getByText("統計情報を読み込み中...")).toBeInTheDocument();
  });

test("API成功時、統計情報を表示する", async () => {
  setupSuccessInitialRender();

  await waitForLoaded();

  expect(screen.getAllByText("2026年5月").length).toBeGreaterThan(0);

  expect(
    screen.getByText("売上一覧（表示基準月を含む3か月）")
  ).toBeInTheDocument();

  expect(
    screen.getByText("予約済みを見込み売上、利用済みを確定売上として表示します。")
  ).toBeInTheDocument();

  expect(screen.getByTestId("responsive-container")).toBeInTheDocument();
  expect(screen.getByTestId("bar-chart")).toBeInTheDocument();
  expect(screen.getByText("chart rows: 3")).toBeInTheDocument();

  expect(
    screen.getByText("2026年3月 / booked: 100000 / paid: 80000")
  ).toBeInTheDocument();

  expect(
    screen.getByText("2026年4月 / booked: 120000 / paid: 110000")
  ).toBeInTheDocument();

  expect(
    screen.getByText("2026年5月 / booked: 150000 / paid: 130000")
  ).toBeInTheDocument();

  expect(screen.getByText("見込み売上")).toBeInTheDocument();
  expect(screen.getByText("確定売上")).toBeInTheDocument();
});

  test("稼働率とレビュー平均を表示する", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByText("稼働率（直近3か月）")).toBeInTheDocument();
    expect(screen.getByText("営業時間に対する予約時間の割合です。"))
      .toBeInTheDocument();

    expect(screen.getByText("45.3%")).toBeInTheDocument();
    expect(screen.getByText("62.3%")).toBeInTheDocument();
    expect(screen.getByText("—")).toBeInTheDocument();

    expect(screen.getByText("レビュー")).toBeInTheDocument();
    expect(screen.getByText("投稿済みレビューの平均スコアです。"))
      .toBeInTheDocument();

    expect(screen.getByText("平均レビュー（公開・非公開すべて）"))
      .toBeInTheDocument();
    expect(screen.getByText("平均レビュー（公開のみ）"))
      .toBeInTheDocument();

    expect(screen.getByText("4.3")).toBeInTheDocument();
    expect(screen.getByText("4.7")).toBeInTheDocument();
  });

  test("スタジオ選択肢を表示する", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(
      screen.getByRole("option", { name: "全体（すべてのスタジオ）" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("option", { name: "池袋ダンスルーム" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("option", { name: "渋谷撮影スタジオ" })
    ).toBeInTheDocument();
  });

  test("スタジオを変更すると roomId 付きで再取得する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

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

    render(<HostStatusPage />);

    await waitForLoaded();

    await user.selectOptions(
      screen.getByDisplayValue("全体（すべてのスタジオ）"),
      "101"
    );

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch.mock.calls[1][0]).toBe(
      "https://localhost:7226/api/host/status?roomId=101&year=2026&month=5"
    );
  });

  test("前月ボタンを押すと前月で再取得する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

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

    render(<HostStatusPage />);

    await waitForLoaded();

    await user.click(screen.getByRole("button", { name: "前月" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch.mock.calls[1][0]).toBe(
      "https://localhost:7226/api/host/status?year=2026&month=4"
    );

    expect(screen.getByText("2026年4月")).toBeInTheDocument();
  });

  test("次月ボタンを押すと次月で再取得する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

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

    render(<HostStatusPage />);

    await waitForLoaded();

    await user.click(screen.getByRole("button", { name: "次月" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch.mock.calls[1][0]).toBe(
      "https://localhost:7226/api/host/status?year=2026&month=6"
    );

    expect(screen.getByText("2026年6月")).toBeInTheDocument();
  });

  test("403の場合、ホストユーザーのみアクセス可能メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 403,
    });

    render(<HostStatusPage />);

    await waitFor(() => {
      expect(
        screen.getByText("ホストユーザーのみアクセスできます。")
      ).toBeInTheDocument();
    });
  });

  test("API失敗時、取得失敗メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<HostStatusPage />);

    await waitFor(() => {
      expect(
        screen.getByText("統計情報の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<HostStatusPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("data が null の場合、統計情報なしメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => null,
    });

    render(<HostStatusPage />);

    await waitFor(() => {
      expect(screen.getByText("統計情報がありません。")).toBeInTheDocument();
    });
  });

  test("API呼び出しURLが正しい", async () => {
    setupSuccessInitialRender();

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/host/status?year=2026&month=5",
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      })
    );
  });
});