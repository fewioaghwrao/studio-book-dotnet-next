import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import HostReviewsPage from "./page";

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

function mockReviewsResponse() {
  return {
    items: [
      {
        id: 1,
        roomId: 101,
        roomName: "池袋ダンスルーム",
        userId: 201,
        userName: "レビュー 太郎",
        score: 5,
        content: "とても使いやすいスタジオでした。",
        publicVisible: true,
        hiddenReason: null,
        hostReply: "ご利用ありがとうございました。",
        hostReplyAt: "2026-05-04T10:30:00Z",
        createdAtUtc: "2026-05-01T09:00:00Z",
      },
      {
        id: 2,
        roomId: 102,
        roomName: "渋谷撮影スタジオ",
        userId: 202,
        userName: "レビュー 花子",
        score: 3,
        content: "設備は良かったです。",
        publicVisible: false,
        hiddenReason: "内容確認中",
        hostReply: null,
        hostReplyAt: null,
        createdAtUtc: "2026-05-02T09:00:00Z",
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
  };
}

describe("HostReviewsPage", () => {
  test("レビュー一覧の基本表示を行う", async () => {
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

    render(<HostReviewsPage />);

    expect(
      screen.getByRole("heading", { name: "レビュー管理" })
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "自分が管理するスタジオのレビュー確認、返信、公開状態の切り替えができます。"
      )
    ).toBeInTheDocument();

    expect(screen.getByText("レビューを読み込み中...")).toBeInTheDocument();

    await waitFor(() => {
      expect(
        screen.getByText("該当するレビューはありません。")
      ).toBeInTheDocument();
    });

    expect(screen.getByText("全 0 件")).toBeInTheDocument();
  });

  test("API成功時、レビュー一覧を表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockReviewsResponse(),
    });

    render(<HostReviewsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    });

    expect(screen.getAllByText("渋谷撮影スタジオ").length).toBeGreaterThan(0);
    expect(screen.getByText(/レビュー 太郎/)).toBeInTheDocument();
    expect(screen.getByText(/レビュー 花子/)).toBeInTheDocument();

    expect(
      screen.getByText("とても使いやすいスタジオでした。")
    ).toBeInTheDocument();
    expect(screen.getByText("設備は良かったです。")).toBeInTheDocument();

    expect(screen.getByText("5/5")).toBeInTheDocument();
    expect(screen.getByText("3/5")).toBeInTheDocument();

    expect(screen.getByText("公開")).toBeInTheDocument();
    expect(screen.getByText("非公開")).toBeInTheDocument();
    expect(screen.getByText("非公開理由：内容確認中")).toBeInTheDocument();

    expect(
      screen.getByDisplayValue("ご利用ありがとうございました。")
    ).toBeInTheDocument();

    expect(screen.getByText("全 2 件")).toBeInTheDocument();

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

    render(<HostReviewsPage />);

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

    render(<HostReviewsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("レビュー一覧の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<HostReviewsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("絞り込み条件を入力して検索すると、クエリ付きURLへ遷移する", async () => {
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

    render(<HostReviewsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("該当するレビューはありません。")
      ).toBeInTheDocument();
    });

    await user.selectOptions(
      screen.getByDisplayValue("すべてのスタジオ"),
      "101"
    );

    await user.selectOptions(screen.getByDisplayValue("すべて"), "5");

    await user.click(screen.getByRole("checkbox", { name: "非公開のみ" }));

    await user.click(screen.getByRole("button", { name: "絞り込む" }));

    expect(mockPush).toHaveBeenCalledWith(
      "/host/reviews?roomId=101&stars=5&onlyHidden=true&page=1&pageSize=10"
    );
  });

  test("クリアボタンを押すとレビュー一覧へ戻る", async () => {
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

    render(<HostReviewsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("該当するレビューはありません。")
      ).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "クリア" }));

    expect(mockPush).toHaveBeenCalledWith("/host/reviews");
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
            roomId: 101,
            roomName: "2ページ目スタジオ",
            userId: 201,
            userName: "レビュー者",
            score: 4,
            content: "2ページ目のレビュー",
            publicVisible: true,
            hiddenReason: null,
            hostReply: null,
            hostReplyAt: null,
            createdAtUtc: "2026-05-04T09:00:00Z",
          },
        ],
        page: 2,
        pageSize: 10,
        totalCount: 25,
        totalPages: 3,
        roomOptions: [],
      }),
    });

    render(<HostReviewsPage />);

    await waitFor(() => {
      expect(screen.getByText("2ページ目スタジオ")).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "次" }));

    expect(mockPush).toHaveBeenCalledWith("/host/reviews?page=3&pageSize=10");
  });

  test("返信を入力して保存すると返信保存APIを呼び、再取得する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          ...mockReviewsResponse(),
          items: [
            {
              ...mockReviewsResponse().items[0],
              hostReply: "",
              hostReplyAt: null,
            },
          ],
          totalCount: 1,
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
          ...mockReviewsResponse(),
          items: [
            {
              ...mockReviewsResponse().items[0],
              hostReply: "またのご利用をお待ちしています。",
              hostReplyAt: "2026-05-04T10:30:00Z",
            },
          ],
          totalCount: 1,
        }),
      });

    render(<HostReviewsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    });

    const textarea = screen.getByPlaceholderText("ホストからの返信を入力");

    await user.type(textarea, "またのご利用をお待ちしています。");

    await user.click(screen.getByRole("button", { name: "返信を保存" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(3);
    });

    expect(mockFetch.mock.calls[1][0]).toBe(
      "https://localhost:7226/api/host/reviews/1/reply"
    );

    expect(mockFetch.mock.calls[1][1]).toEqual(
      expect.objectContaining({
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          hostReply: "またのご利用をお待ちしています。",
        }),
      })
    );

    expect(screen.getByText("返信を保存しました。")).toBeInTheDocument();
  });

  test("返信保存API失敗時、APIから返されたエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          ...mockReviewsResponse(),
          items: [mockReviewsResponse().items[0]],
          totalCount: 1,
        }),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({
          message: "返信内容が不正です。",
        }),
      });

    render(<HostReviewsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    });

    await user.click(screen.getByRole("button", { name: "返信を保存" }));

    await waitFor(() => {
      expect(screen.getByText("返信内容が不正です。")).toBeInTheDocument();
    });
  });

  test("公開レビューを非公開に変更する", async () => {
    const user = userEvent.setup();

    jest.spyOn(window, "confirm").mockReturnValueOnce(true);

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          ...mockReviewsResponse(),
          items: [mockReviewsResponse().items[0]],
          totalCount: 1,
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
          ...mockReviewsResponse(),
          items: [
            {
              ...mockReviewsResponse().items[0],
              publicVisible: false,
              hiddenReason: "不適切表現を含むため",
            },
          ],
          totalCount: 1,
        }),
      });

    render(<HostReviewsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    });

    await user.type(screen.getByPlaceholderText("任意"), "不適切表現を含むため");

    await user.click(screen.getByRole("button", { name: "非公開にする" }));

    expect(window.confirm).toHaveBeenCalledWith("このレビューを非公開にしますか？");

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(3);
    });

    expect(mockFetch.mock.calls[1][0]).toBe(
      "https://localhost:7226/api/host/reviews/1/visibility"
    );

    expect(mockFetch.mock.calls[1][1]).toEqual(
      expect.objectContaining({
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          isPublic: false,
          reason: "不適切表現を含むため",
        }),
      })
    );

    expect(screen.getByText("非公開に変更しました。")).toBeInTheDocument();
  });

  test("非公開確認でキャンセルした場合、APIを呼ばない", async () => {
    const user = userEvent.setup();

    jest.spyOn(window, "confirm").mockReturnValueOnce(false);

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockReviewsResponse(),
        items: [mockReviewsResponse().items[0]],
        totalCount: 1,
      }),
    });

    render(<HostReviewsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("池袋ダンスルーム").length).toBeGreaterThan(0);
    });

    await user.click(screen.getByRole("button", { name: "非公開にする" }));

    expect(window.confirm).toHaveBeenCalledWith("このレビューを非公開にしますか？");
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("非公開レビューを公開に戻す", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          ...mockReviewsResponse(),
          items: [mockReviewsResponse().items[1]],
          totalCount: 1,
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
          ...mockReviewsResponse(),
          items: [
            {
              ...mockReviewsResponse().items[1],
              publicVisible: true,
              hiddenReason: null,
            },
          ],
          totalCount: 1,
        }),
      });

    render(<HostReviewsPage />);

    await waitFor(() => {
      expect(screen.getAllByText("渋谷撮影スタジオ").length).toBeGreaterThan(0);
    });

    await user.click(screen.getByRole("button", { name: "公開に戻す" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(3);
    });

    expect(mockFetch.mock.calls[1][0]).toBe(
      "https://localhost:7226/api/host/reviews/2/visibility"
    );

    expect(mockFetch.mock.calls[1][1]).toEqual(
      expect.objectContaining({
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          isPublic: true,
          reason: null,
        }),
      })
    );

    expect(screen.getByText("公開に変更しました。")).toBeInTheDocument();
  });
});