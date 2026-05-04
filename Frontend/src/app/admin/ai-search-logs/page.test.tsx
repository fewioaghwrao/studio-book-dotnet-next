import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import AdminAiSearchLogsPage from "./page";

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

describe("AdminAiSearchLogsPage", () => {
  test("AI検索ログ一覧の基本表示を行う", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        items: [],
        page: 1,
        pageSize: 10,
        totalCount: 0,
        totalPages: 1,
      }),
    });

    render(<AdminAiSearchLogsPage />);

    expect(
      screen.getByRole("heading", { name: "AI検索ログ" })
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "AI自然文スタジオ検索の利用状況、成功・失敗、検索文、結果件数を確認できます。"
      )
    ).toBeInTheDocument();

    expect(screen.getByText("AI検索ログを読み込み中...")).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText("AI検索ログはありません。")).toBeInTheDocument();
    });

    expect(screen.getByText("全 0 件")).toBeInTheDocument();
  });

test("API成功時、AI検索ログを表示する", async () => {
  mockFetch.mockResolvedValueOnce({
    ok: true,
    status: 200,
    json: async () => ({
      items: [
        {
          id: 1,
          createdAtUtc: "2026-05-04T10:30:00Z",
          query: "池袋で夜に使えるスタジオ",
          ipAddress: "127.0.0.1",
          userId: 10,
          model: "gpt-5.4-mini",
          succeeded: true,
          resultCount: 3,
          errorMessage: null,
        },
        {
          id: 2,
          createdAtUtc: "2026-05-04T11:00:00Z",
          query: "渋谷 撮影",
          ipAddress: null,
          userId: null,
          model: null,
          succeeded: false,
          resultCount: 0,
          errorMessage: "API error",
        },
      ],
      page: 1,
      pageSize: 10,
      totalCount: 2,
      totalPages: 1,
    }),
  });

  render(<AdminAiSearchLogsPage />);

  await waitFor(() => {
    expect(screen.getAllByText("池袋で夜に使えるスタジオ").length).toBeGreaterThan(0);
  });

  expect(screen.getAllByText("渋谷 撮影").length).toBeGreaterThan(0);
  expect(screen.getAllByText("成功").length).toBeGreaterThan(0);
  expect(screen.getAllByText("失敗").length).toBeGreaterThan(0);
  expect(screen.getAllByText("3件").length).toBeGreaterThan(0);
  expect(screen.getAllByText("0件").length).toBeGreaterThan(0);
  expect(screen.getAllByText("gpt-5.4-mini").length).toBeGreaterThan(0);
  expect(screen.getAllByText("API error").length).toBeGreaterThan(0);
  expect(screen.getByText("全 2 件")).toBeInTheDocument();
});
  test("403の場合、管理者のみアクセス可能メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 403,
    });

    render(<AdminAiSearchLogsPage />);

    await waitFor(() => {
      expect(screen.getByText("管理者のみアクセスできます。")).toBeInTheDocument();
    });
  });

  test("API失敗時、エラーメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<AdminAiSearchLogsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("AI検索ログの取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<AdminAiSearchLogsPage />);

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
      }),
    });

    render(<AdminAiSearchLogsPage />);

    await waitFor(() => {
      expect(screen.getByText("AI検索ログはありません。")).toBeInTheDocument();
    });

    await user.type(
      screen.getByPlaceholderText("検索文 / エラー内容"),
      "池袋"
    );

    await user.type(screen.getByPlaceholderText("userId"), "10");
    await user.type(screen.getByPlaceholderText("127.0.0.1"), "192.168.0.1");
    await user.type(screen.getByPlaceholderText("gpt-5.4-mini"), "gpt-test");

    await user.selectOptions(screen.getByDisplayValue("すべて"), "true");

    await user.click(screen.getByRole("button", { name: "検索" }));

    expect(mockPush).toHaveBeenCalledWith(
      "/admin/ai-search-logs?q=%E6%B1%A0%E8%A2%8B&userId=10&ipAddress=192.168.0.1&succeeded=true&model=gpt-test&page=1&pageSize=10"
    );
  });

  test("クリアボタンを押すとAI検索ログ一覧へ戻る", async () => {
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
      }),
    });

    render(<AdminAiSearchLogsPage />);

    await waitFor(() => {
      expect(screen.getByText("AI検索ログはありません。")).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "クリア" }));

    expect(mockPush).toHaveBeenCalledWith("/admin/ai-search-logs");
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
          id: 11,
          createdAtUtc: "2026-05-04T10:30:00Z",
          query: "検索ログ",
          ipAddress: "127.0.0.1",
          userId: 1,
          model: "gpt-test",
          succeeded: true,
          resultCount: 1,
          errorMessage: null,
        },
      ],
      page: 2,
      pageSize: 10,
      totalCount: 25,
      totalPages: 3,
    }),
  });

  render(<AdminAiSearchLogsPage />);

  await waitFor(() => {
    expect(screen.getAllByText("検索ログ").length).toBeGreaterThan(0);
  });

  await user.click(screen.getByRole("button", { name: "次" }));

  expect(mockPush).toHaveBeenCalledWith(
    "/admin/ai-search-logs?page=3&pageSize=10"
  );
});
});