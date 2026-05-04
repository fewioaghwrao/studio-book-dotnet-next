import { render, screen, waitFor } from "@testing-library/react";
import HostSalesDetailPage from "./page";

const mockFetch = jest.fn();

jest.mock("next/navigation", () => ({
  useParams: () => ({
    id: "1",
  }),
}));

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

function mockSalesDetailResponse() {
  return {
    reservationId: 1,
    roomId: 101,
    roomName: "池袋ダンスルーム",
    guestName: "予約 太郎",
    startAt: "2026-05-10T10:00:00",
    endAt: "2026-05-10T12:00:00",
    amount: 7200,
    status: "paid",
    items: [
      {
        kind: "base",
        description: "基本利用料",
        sliceStart: "2026-05-10T10:00:00",
        sliceEnd: "2026-05-10T12:00:00",
        unitRatePerHour: 3000,
        sliceAmount: 6000,
      },
      {
        kind: "tax",
        description: "消費税",
        sliceStart: null,
        sliceEnd: null,
        unitRatePerHour: null,
        sliceAmount: 600,
      },
      {
        kind: "platform_fee",
        description: "プラットフォーム手数料",
        sliceStart: null,
        sliceEnd: null,
        unitRatePerHour: null,
        sliceAmount: 600,
      },
    ],
  };
}

function setupSuccessInitialRender() {
  mockFetch.mockResolvedValueOnce({
    ok: true,
    status: 200,
    json: async () => mockSalesDetailResponse(),
  });

  render(<HostSalesDetailPage />);
}

async function waitForLoaded() {
  await waitFor(() => {
    expect(
      screen.getByRole("heading", { name: "売上明細" })
    ).toBeInTheDocument();
  });
}

describe("HostSalesDetailPage", () => {
  test("読み込み中メッセージを表示する", () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockSalesDetailResponse(),
    });

    render(<HostSalesDetailPage />);

    expect(screen.getByText("売上明細を読み込み中...")).toBeInTheDocument();
  });

  test("API成功時、売上明細の基本情報を表示する", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByText("SALES DETAIL")).toBeInTheDocument();

    expect(
      screen.getByText("予約単位の料金・税・手数料の内訳を確認できます。")
    ).toBeInTheDocument();

    expect(screen.getByText("予約ID")).toBeInTheDocument();
    expect(screen.getByText("スタジオ")).toBeInTheDocument();
    expect(screen.getByText("予約者")).toBeInTheDocument();
    expect(screen.getByText("期間")).toBeInTheDocument();
    expect(screen.getAllByText("合計").length).toBeGreaterThan(0);
    expect(screen.getByText("状態")).toBeInTheDocument();

    expect(screen.getByText("池袋ダンスルーム")).toBeInTheDocument();
    expect(screen.getByText("予約 太郎")).toBeInTheDocument();
    expect(screen.getAllByText("7,200円").length).toBeGreaterThan(0);
    expect(screen.getByText("利用済み")).toBeInTheDocument();
  });

  test("料金明細を表示する", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByText("料金明細")).toBeInTheDocument();

    expect(
      screen.getByText(
        "基本料金、加算料金、消費税、プラットフォーム手数料の内訳です。"
      )
    ).toBeInTheDocument();

    expect(screen.getAllByText("基本料金").length).toBeGreaterThan(0);
    expect(screen.getAllByText("消費税").length).toBeGreaterThan(0);
    expect(screen.getAllByText("手数料").length).toBeGreaterThan(0);

    expect(screen.getAllByText("基本利用料").length).toBeGreaterThan(0);
    expect(screen.getAllByText("プラットフォーム手数料").length).toBeGreaterThan(0);

    expect(screen.getAllByText("3,000円/時").length).toBeGreaterThan(0);
    expect(screen.getAllByText("6,000円").length).toBeGreaterThan(0);
    expect(screen.getAllByText("600円").length).toBeGreaterThan(0);
  });

  test("CSVとPDFリンクのURLが正しい", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByRole("link", { name: "明細CSV" })).toHaveAttribute(
      "href",
      "https://localhost:7226/api/host/sales/1/items.csv"
    );

    expect(
      screen.getByRole("link", { name: "明細請求書PDF" })
    ).toHaveAttribute(
      "href",
      "https://localhost:7226/api/host/sales/1/invoice.pdf"
    );
  });

  test("明細が0件の場合、空メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockSalesDetailResponse(),
        items: [],
      }),
    });

    render(<HostSalesDetailPage />);

    await waitForLoaded();

    expect(screen.getByText("明細はありません。")).toBeInTheDocument();
  });

  test("403の場合、ホストユーザーのみアクセス可能メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 403,
    });

    render(<HostSalesDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByText("ホストユーザーのみアクセスできます。")
      ).toBeInTheDocument();
    });

    expect(
      screen.getByRole("link", { name: "売上明細一覧へ戻る" })
    ).toHaveAttribute("href", "/host/sales");
  });

  test("404の場合、売上明細が見つからないメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 404,
    });

    render(<HostSalesDetailPage />);

    await waitFor(() => {
      expect(screen.getByText("売上明細が見つかりません。")).toBeInTheDocument();
    });

    expect(
      screen.getByRole("link", { name: "売上明細一覧へ戻る" })
    ).toHaveAttribute("href", "/host/sales");
  });

  test("API失敗時、取得失敗メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<HostSalesDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByText("売上明細の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<HostSalesDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("戻るリンクの遷移先が正しい", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByRole("link", { name: "戻る" })).toHaveAttribute(
      "href",
      "/host/sales"
    );
  });

  test("API呼び出しURLが正しい", async () => {
    setupSuccessInitialRender();

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/host/sales/1",
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      })
    );
  });

  test("予約ステータス booked を予約済みとして表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockSalesDetailResponse(),
        status: "booked",
      }),
    });

    render(<HostSalesDetailPage />);

    await waitForLoaded();

    expect(screen.getByText("予約済み")).toBeInTheDocument();
  });

  test("予約ステータス canceled をキャンセル済みとして表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockSalesDetailResponse(),
        status: "canceled",
      }),
    });

    render(<HostSalesDetailPage />);

    await waitForLoaded();

    expect(screen.getByText("キャンセル済み")).toBeInTheDocument();
  });
});