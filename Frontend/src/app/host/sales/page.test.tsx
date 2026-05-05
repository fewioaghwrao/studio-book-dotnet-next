import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import HostSalesPage from "./page";

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

function mockSalesResponse() {
  return {
    items: [
      {
        reservationId: 1,
        roomId: 101,
        roomName: "池袋ダンスルーム",
        guestName: "予約 太郎",
        startAt: "2026-05-10T10:00:00",
        endAt: "2026-05-10T12:00:00",
        amount: 6000,
        status: "paid",
        hasItems: true,
      },
      {
        reservationId: 2,
        roomId: 102,
        roomName: "渋谷撮影スタジオ",
        guestName: "利用 花子",
        startAt: "2026-05-11T13:00:00",
        endAt: "2026-05-11T15:00:00",
        amount: 9000,
        status: "booked",
        hasItems: false,
      },
      {
        reservationId: 3,
        roomId: 103,
        roomName: "新宿配信スタジオ",
        guestName: "取消 次郎",
        startAt: "2026-05-12T09:00:00",
        endAt: "2026-05-12T11:00:00",
        amount: 5000,
        status: "canceled",
        hasItems: true,
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

function setupSuccessInitialRender() {
  mockFetch.mockResolvedValueOnce({
    ok: true,
    status: 200,
    json: async () => mockSalesResponse(),
  });

  render(<HostSalesPage />);
}

async function waitForSalesLoaded() {
  await waitFor(() => {
    expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
  });
}

describe("HostSalesPage", () => {
  test("初期表示と読み込み中メッセージを表示する", () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockSalesResponse(),
    });

    render(<HostSalesPage />);

    expect(
      screen.getByRole("heading", { name: "売上明細一覧" })
    ).toBeInTheDocument();

    expect(screen.getByText("HOST SALES")).toBeInTheDocument();

    expect(
      screen.getByText("管理スタジオの予約売上と料金明細を確認できます。")
    ).toBeInTheDocument();

    expect(screen.getByText("売上一覧を読み込み中...")).toBeInTheDocument();
  });

  test("API成功時、売上一覧を表示する", async () => {
    setupSuccessInitialRender();

    await waitForSalesLoaded();

    expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    expect(screen.getAllByText("渋谷撮影スタジオ").length).toBeGreaterThan(0);
    expect(screen.getAllByText("新宿配信スタジオ").length).toBeGreaterThan(0);

    expect(screen.getAllByText("予約 太郎").length).toBeGreaterThan(0);
    expect(screen.getAllByText("利用 花子").length).toBeGreaterThan(0);
    expect(screen.getAllByText("取消 次郎").length).toBeGreaterThan(0);

    expect(screen.getAllByText("6,000円").length).toBeGreaterThan(0);
    expect(screen.getAllByText("9,000円").length).toBeGreaterThan(0);
    expect(screen.getAllByText("5,000円").length).toBeGreaterThan(0);

    expect(screen.getAllByText("利用済み").length).toBeGreaterThan(0);
    expect(screen.getAllByText("予約済み").length).toBeGreaterThan(0);
    expect(screen.getAllByText("キャンセル済み").length).toBeGreaterThan(0);

    expect(screen.getByText("全 3 件")).toBeInTheDocument();

    expect(
      screen.getByRole("option", { name: "池袋ダンスルーム" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("option", { name: "渋谷撮影スタジオ" })
    ).toBeInTheDocument();
  });

  test("0件の場合、空メッセージを表示する", async () => {
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

    render(<HostSalesPage />);

    await waitFor(() => {
      expect(screen.getByText("該当する予約はありません。")).toBeInTheDocument();
    });

    expect(screen.getByText("全 0 件")).toBeInTheDocument();
  });

  test("403の場合、ホストユーザーのみアクセス可能メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 403,
    });

    render(<HostSalesPage />);

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

    render(<HostSalesPage />);

    await waitFor(() => {
      expect(
        screen.getByText("売上一覧の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<HostSalesPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("検索条件を変更して選択適用すると router.push する", async () => {
    const user = userEvent.setup();

    setupSuccessInitialRender();

    await waitForSalesLoaded();

    await user.selectOptions(screen.getByDisplayValue("全体"), "101");

    const checkbox = screen.getByRole("checkbox", {
      name: "明細がある予約のみ",
    });

    expect(checkbox).toBeChecked();

    await user.click(checkbox);

    await user.click(screen.getByRole("button", { name: "選択適用" }));

    expect(mockPush).toHaveBeenCalledWith(
      "/host/sales?roomId=101&onlyWithItems=false&page=1&pageSize=10"
    );
  });

test("CSVダウンロード時、正しいURLでAPIを呼び出す", async () => {
  const user = userEvent.setup();

  setupSuccessInitialRender();

  await waitForSalesLoaded();

  mockFetch.mockResolvedValueOnce({
    ok: true,
    status: 200,
    blob: async () => new Blob(["csv"], { type: "text/csv" }),
  });

  Object.defineProperty(URL, "createObjectURL", {
    writable: true,
    value: jest.fn(() => "blob:mock-url"),
  });

  Object.defineProperty(URL, "revokeObjectURL", {
    writable: true,
    value: jest.fn(),
  });

  await user.click(screen.getByRole("button", { name: "CSVダウンロード" }));

  expect(mockFetch).toHaveBeenLastCalledWith(
    "https://localhost:7226/api/host/sales.csv?onlyWithItems=true",
    expect.objectContaining({
      method: "GET",
    })
  );
});

test("検索条件変更後、CSVダウンロードURLも変わる", async () => {
  const user = userEvent.setup();

  setupSuccessInitialRender();

  await waitForSalesLoaded();

  await user.selectOptions(screen.getByDisplayValue("全体"), "101");

  const checkbox = screen.getByRole("checkbox", {
    name: "明細がある予約のみ",
  });

  await user.click(checkbox);

  mockFetch.mockResolvedValueOnce({
    ok: true,
    status: 200,
    blob: async () => new Blob(["csv"], { type: "text/csv" }),
  });

  const createObjectURLMock = jest.fn(() => "blob:mock-url");
  const revokeObjectURLMock = jest.fn();

  Object.defineProperty(URL, "createObjectURL", {
    writable: true,
    value: createObjectURLMock,
  });

  Object.defineProperty(URL, "revokeObjectURL", {
    writable: true,
    value: revokeObjectURLMock,
  });

  const clickMock = jest.fn();

  jest.spyOn(document, "createElement").mockImplementation((tagName: string) => {
    const element = document.createElementNS(
      "http://www.w3.org/1999/xhtml",
      tagName
    ) as HTMLElement;

    if (tagName === "a") {
      Object.defineProperty(element, "click", {
        writable: true,
        value: clickMock,
      });
    }

    return element;
  });

  await user.click(screen.getByRole("button", { name: "CSVダウンロード" }));

  expect(mockFetch).toHaveBeenLastCalledWith(
    "https://localhost:7226/api/host/sales.csv?roomId=101&onlyWithItems=false",
    expect.objectContaining({
      method: "GET",
    })
  );

  expect(createObjectURLMock).toHaveBeenCalled();
  expect(clickMock).toHaveBeenCalled();
  expect(revokeObjectURLMock).toHaveBeenCalledWith("blob:mock-url");
});
  test("ページングボタンを押すと指定ページへ遷移する", async () => {
    const user = userEvent.setup();

    mockSearchParams = new URLSearchParams("page=1");

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockSalesResponse(),
        page: 1,
        pageSize: 10,
        totalCount: 25,
        totalPages: 3,
      }),
    });

    render(<HostSalesPage />);

    await waitForSalesLoaded();

    await user.click(screen.getByRole("button", { name: "次" }));

    expect(mockPush).toHaveBeenCalledWith(
      "/host/sales?onlyWithItems=true&page=2&pageSize=10"
    );
  });

  test("検索条件を保持したままページングする", async () => {
    const user = userEvent.setup();

    mockSearchParams = new URLSearchParams(
      "roomId=101&onlyWithItems=false&page=2"
    );

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockSalesResponse(),
        page: 2,
        pageSize: 10,
        totalCount: 25,
        totalPages: 3,
      }),
    });

    render(<HostSalesPage />);

    await waitForSalesLoaded();

    await user.click(screen.getByRole("button", { name: "次" }));

    expect(mockPush).toHaveBeenCalledWith(
      "/host/sales?roomId=101&onlyWithItems=false&page=3&pageSize=10"
    );
  });

  test("明細リンクの遷移先が正しい", async () => {
    setupSuccessInitialRender();

    await waitForSalesLoaded();

    const detailLinks = screen.getAllByRole("link", { name: "明細" });

    expect(
      detailLinks.some((link) => link.getAttribute("href") === "/host/sales/1")
    ).toBe(true);

    expect(
      detailLinks.some((link) => link.getAttribute("href") === "/host/sales/2")
    ).toBe(true);

    expect(
      detailLinks.some((link) => link.getAttribute("href") === "/host/sales/3")
    ).toBe(true);
  });

  test("API呼び出しURLが正しい", async () => {
    setupSuccessInitialRender();

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/host/sales?onlyWithItems=true&page=1&pageSize=10",
      expect.objectContaining({
        method: "GET",        cache: "no-store",
      })
    );
  });

  test("クエリ付き初期表示の場合、API呼び出しURLが正しい", async () => {
    mockSearchParams = new URLSearchParams(
      "roomId=101&onlyWithItems=false&page=2"
    );

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockSalesResponse(),
    });

    render(<HostSalesPage />);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/host/sales?roomId=101&onlyWithItems=false&page=2&pageSize=10",
      expect.objectContaining({
        method: "GET",        cache: "no-store",
      })
    );
  });
});