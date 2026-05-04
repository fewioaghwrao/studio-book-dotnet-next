import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import AdminLogsPage from "./page";

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

describe("AdminLogsPage", () => {
test("ログ一覧の基本表示を行う", async () => {
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

  render(<AdminLogsPage />);

  expect(
    screen.getAllByRole("heading", { name: "ログ一覧" }).length
  ).toBeGreaterThan(0);

  expect(
    screen.getByText("管理操作や主要イベントの履歴を検索・確認できます。")
  ).toBeInTheDocument();

  expect(screen.getByText("ログ一覧を読み込み中...")).toBeInTheDocument();

  await waitFor(() => {
    expect(screen.getByText("ログはありません。")).toBeInTheDocument();
  });

  expect(screen.getByText("全 0 件")).toBeInTheDocument();
});

  test("API成功時、ログ一覧を表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        items: [
          {
            id: 1,
            ts: "2026-05-04T10:30:00Z",
            actorId: 10,
            action: "CREATE_ROOM",
            entity: "Room",
            entityId: 101,
            note: "スタジオを作成しました",
          },
          {
            id: 2,
            ts: "2026-05-04T11:00:00Z",
            actorId: null,
            action: "DELETE_USER",
            entity: "User",
            entityId: null,
            note: "ユーザーを削除しました",
          },
        ],
        page: 1,
        pageSize: 10,
        totalCount: 2,
        totalPages: 1,
      }),
    });

    render(<AdminLogsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("CREATE_ROOM").length).toBeGreaterThan(0);
    });

    expect(screen.getAllByText("DELETE_USER").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Room").length).toBeGreaterThan(0);
    expect(screen.getAllByText("User").length).toBeGreaterThan(0);
    expect(screen.getAllByText("スタジオを作成しました").length).toBeGreaterThan(0);
    expect(screen.getAllByText("ユーザーを削除しました").length).toBeGreaterThan(0);
    expect(screen.getByText("全 2 件")).toBeInTheDocument();
  });

  test("403の場合、管理者のみアクセス可能メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 403,
    });

    render(<AdminLogsPage />);

    await waitFor(() => {
      expect(screen.getByText("管理者のみアクセスできます。")).toBeInTheDocument();
    });
  });

  test("API失敗時、エラーメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<AdminLogsPage />);

    await waitFor(() => {
      expect(screen.getByText("ログ一覧の取得に失敗しました。")).toBeInTheDocument();
    });
  });

  test("通信エラー時、エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<AdminLogsPage />);

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

    render(<AdminLogsPage />);

    await waitFor(() => {
      expect(screen.getByText("ログはありません。")).toBeInTheDocument();
    });

    await user.type(screen.getByPlaceholderText("action / entity / note"), "room");
    await user.type(screen.getByPlaceholderText("actorId"), "10");
    await user.type(screen.getByPlaceholderText("entityId"), "101");
    await user.type(screen.getByPlaceholderText("UPDATE"), "UPDATE_ROOM");
    await user.type(screen.getByPlaceholderText("Room"), "Room");

    await user.click(screen.getByRole("button", { name: "検索" }));

    expect(mockPush).toHaveBeenCalledWith(
      "/admin/logs?q=room&actorId=10&action=UPDATE_ROOM&entity=Room&entityId=101&page=1&pageSize=10"
    );
  });

  test("クリアボタンを押すとログ一覧へ戻る", async () => {
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

    render(<AdminLogsPage />);

    await waitFor(() => {
      expect(screen.getByText("ログはありません。")).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "クリア" }));

    expect(mockPush).toHaveBeenCalledWith("/admin/logs");
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
            ts: "2026-05-04T10:30:00Z",
            actorId: 1,
            action: "UPDATE_ROOM",
            entity: "Room",
            entityId: 101,
            note: "検索ログ",
          },
        ],
        page: 2,
        pageSize: 10,
        totalCount: 25,
        totalPages: 3,
      }),
    });

    render(<AdminLogsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("検索ログ").length).toBeGreaterThan(0);
    });

    await user.click(screen.getByRole("button", { name: "次" }));

    expect(mockPush).toHaveBeenCalledWith("/admin/logs?page=3&pageSize=10");
  });
});