import { render, screen, waitFor, act } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import ReviewNewPage from "./page";

const mockFetch = jest.fn();
const mockPush = jest.fn();

let mockSearchParams = new URLSearchParams("reservationId=10");

jest.mock("next/navigation", () => ({
  useParams: () => ({
    roomId: "1",
  }),
  useSearchParams: () => mockSearchParams,
  useRouter: () => ({
    push: mockPush,
  }),
}));

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();
  mockPush.mockReset();

  mockSearchParams = new URLSearchParams("reservationId=10");
  global.fetch = mockFetch as jest.Mock;

  jest.useFakeTimers();
});

afterEach(() => {
  jest.useRealTimers();
});

function mockReviewPageResponse() {
  return {
    roomId: 1,
    roomName: "池袋ダンスルーム",
    roomImageName: "room01.jpg",
    roomAddress: "東京都豊島区東池袋",
    averageScore: 4.5,
    reviewCount: 12,
    alreadyReviewed: false,
    canReview: true,
    page: 1,
    pageSize: 10,
    totalCount: 2,
    totalPages: 2,
    reviews: [
      {
        id: 1,
        score: 5,
        content: "とても使いやすいスタジオでした。",
        userName: "一般 太郎",
        createdAtUtc: "2026-05-01T09:00:00",
      },
      {
        id: 2,
        score: 4,
        content: "駅から近くて便利でした。",
        userName: "",
        createdAtUtc: "2026-05-02T10:00:00",
      },
    ],
  };
}

function setupSuccessInitialRender() {
  mockFetch.mockResolvedValueOnce({
    ok: true,
    status: 200,
    json: async () => mockReviewPageResponse(),
  });

  render(<ReviewNewPage />);
}

async function waitForLoaded() {
  await waitFor(() => {
    expect(
      screen.getByRole("heading", { name: "レビューを書く" })
    ).toBeInTheDocument();
  });
}

describe("ReviewNewPage", () => {
  test("読み込み中メッセージを表示する", () => {
    setupSuccessInitialRender();

    expect(screen.getByText("レビュー画面を読み込み中...")).toBeInTheDocument();
  });

  test("API成功時、レビュー投稿画面とスタジオ情報を表示する", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByText("WRITE REVIEW")).toBeInTheDocument();

    expect(
      screen.getByText("利用したスタジオの感想を投稿できます。")
    ).toBeInTheDocument();

    expect(screen.getByText("池袋ダンスルーム")).toBeInTheDocument();
    expect(screen.getByText("東京都豊島区東池袋")).toBeInTheDocument();

    expect(screen.getByAltText("池袋ダンスルーム")).toHaveAttribute(
      "src",
      "/storage/room01.jpg"
    );

    expect(screen.getByText("4.5 / 5")).toBeInTheDocument();
    expect(screen.getByText("（12件）")).toBeInTheDocument();

    expect(screen.getByText("選択中: 5 / 5")).toBeInTheDocument();
    expect(screen.getByLabelText("コメント")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "投稿する" })).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "レビュー文を整える" })
    ).toBeDisabled();
  });

  test("画像がない場合、noImageを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockReviewPageResponse(),
        roomImageName: null,
      }),
    });

    render(<ReviewNewPage />);

    await waitForLoaded();

    expect(screen.getByAltText("池袋ダンスルーム")).toHaveAttribute(
      "src",
      "/images/noImage.png"
    );
  });

  test("既存レビューを表示する", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByText("この部屋のレビュー")).toBeInTheDocument();

    expect(
      screen.getByText("とても使いやすいスタジオでした。")
    ).toBeInTheDocument();

    expect(screen.getByText("駅から近くて便利でした。")).toBeInTheDocument();
    expect(screen.getByText("投稿者: 一般 太郎")).toBeInTheDocument();
    expect(screen.getByText("投稿者: ユーザー")).toBeInTheDocument();

    expect(screen.getByText("5 / 5")).toBeInTheDocument();
    expect(screen.getByText("4 / 5")).toBeInTheDocument();
  });

  test("レビューが0件の場合、空メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockReviewPageResponse(),
        reviews: [],
        totalCount: 0,
        totalPages: 1,
      }),
    });

    render(<ReviewNewPage />);

    await waitForLoaded();

    expect(screen.getByText("まだレビューはありません。")).toBeInTheDocument();
  });

  test("canReview=false かつ alreadyReviewed=true の場合、投稿済みメッセージを表示しフォームを表示しない", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockReviewPageResponse(),
        canReview: false,
        alreadyReviewed: true,
      }),
    });

    render(<ReviewNewPage />);

    await waitForLoaded();

    expect(
      screen.getByText("このスタジオへのレビューは投稿済みです。")
    ).toBeInTheDocument();

    expect(
      screen.queryByRole("button", { name: "投稿する" })
    ).not.toBeInTheDocument();
  });

  test("canReview=false かつ alreadyReviewed=false の場合、予約条件メッセージを表示しフォームを表示しない", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockReviewPageResponse(),
        canReview: false,
        alreadyReviewed: false,
      }),
    });

    render(<ReviewNewPage />);

    await waitForLoaded();

    expect(
      screen.getByText("レビュー投稿は、決済済みの予約があるユーザーのみ可能です。")
    ).toBeInTheDocument();

    expect(
      screen.queryByRole("button", { name: "投稿する" })
    ).not.toBeInTheDocument();
  });

  test("404の場合、スタジオが見つからないメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 404,
    });

    render(<ReviewNewPage />);

    await waitFor(() => {
      expect(screen.getByText("スタジオが見つかりません。")).toBeInTheDocument();
    });

    expect(screen.getByRole("link", { name: "予約一覧へ戻る" })).toHaveAttribute(
      "href",
      "/reservations"
    );
  });

  test("API失敗時、取得失敗メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<ReviewNewPage />);

    await waitFor(() => {
      expect(
        screen.getByText("レビュー画面の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<ReviewNewPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("コメント未入力で投稿するとバリデーションエラーを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    setupSuccessInitialRender();

    await waitForLoaded();

    await user.click(screen.getByRole("button", { name: "投稿する" }));

    expect(screen.getByText("コメントを入力してください。")).toBeInTheDocument();
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("評価を変更できる", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    setupSuccessInitialRender();

    await waitForLoaded();

    await user.click(screen.getByRole("button", { name: "3点" }));

    expect(screen.getByText("選択中: 3 / 5")).toBeInTheDocument();
  });

  test("コメントは1000文字まで入力できる", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    setupSuccessInitialRender();

    await waitForLoaded();

    const textarea = screen.getByLabelText("コメント");

    await user.type(textarea, "a".repeat(1001));

    expect(textarea).toHaveValue("a".repeat(1000));
    expect(screen.getByText("1000/1000")).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("正常入力でレビュー投稿し、再取得後に予約一覧へ遷移する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockReviewPageResponse(),
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
          ...mockReviewPageResponse(),
          alreadyReviewed: true,
          canReview: false,
        }),
      });

    render(<ReviewNewPage />);

    await waitForLoaded();

    await user.click(screen.getByRole("button", { name: "4点" }));
    await user.type(screen.getByLabelText("コメント"), "とても良かったです。");

    await user.click(screen.getByRole("button", { name: "投稿する" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(3);
    });

    expect(mockFetch.mock.calls[1][0]).toBe(
      "https://localhost:7226/api/rooms/1/reviews"
    );

    expect(mockFetch.mock.calls[1][1]).toEqual(
      expect.objectContaining({
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          reservationId: 10,
          score: 4,
          content: "とても良かったです。",
        }),
      })
    );

    await waitFor(() => {
      expect(screen.getByText("レビューを投稿しました。")).toBeInTheDocument();
    });

    await act(async () => {
      jest.advanceTimersByTime(700);
    });

    expect(mockPush).toHaveBeenCalledWith("/reservations");
  });

  test("投稿APIで400の場合、APIメッセージを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockReviewPageResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({
          message: "この予約にはレビューを投稿できません。",
        }),
      });

    render(<ReviewNewPage />);

    await waitForLoaded();

    await user.type(screen.getByLabelText("コメント"), "良かったです。");
    await user.click(screen.getByRole("button", { name: "投稿する" }));

    await waitFor(() => {
      expect(
        screen.getByText("この予約にはレビューを投稿できません。")
      ).toBeInTheDocument();
    });
  });

  test("投稿APIで404の場合、スタジオが見つからないメッセージを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockReviewPageResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 404,
        json: async () => ({}),
      });

    render(<ReviewNewPage />);

    await waitForLoaded();

    await user.type(screen.getByLabelText("コメント"), "良かったです。");
    await user.click(screen.getByRole("button", { name: "投稿する" }));

    await waitFor(() => {
      expect(screen.getByText("スタジオが見つかりません。")).toBeInTheDocument();
    });
  });

  test("投稿API失敗時、投稿失敗メッセージを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockReviewPageResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: async () => ({}),
      });

    render(<ReviewNewPage />);

    await waitForLoaded();

    await user.type(screen.getByLabelText("コメント"), "良かったです。");
    await user.click(screen.getByRole("button", { name: "投稿する" }));

    await waitFor(() => {
      expect(screen.getByText("レビュー投稿に失敗しました。")).toBeInTheDocument();
    });
  });

  test("AI補助はコメント未入力ではボタンが disabled になる", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(
      screen.getByRole("button", { name: "レビュー文を整える" })
    ).toBeDisabled();

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("AI補助成功時、コメントを整文結果に差し替える", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockReviewPageResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          assistedContent:
            "駅から近く、設備も使いやすい素敵なスタジオでした。",
        }),
      });

    render(<ReviewNewPage />);

    await waitForLoaded();

    await user.type(screen.getByLabelText("コメント"), "駅近い。よかった。");

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: "レビュー文を整える" })
      ).toBeEnabled();
    });

    await user.click(screen.getByRole("button", { name: "レビュー文を整える" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch.mock.calls[1][0]).toBe(
      "https://localhost:7226/api/ai/review-assist"
    );

    expect(mockFetch.mock.calls[1][1]).toEqual(
      expect.objectContaining({
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
        },
        cache: "no-store",
        body: JSON.stringify({
          content: "駅近い。よかった。",
          score: 5,
          roomName: "池袋ダンスルーム",
        }),
      })
    );

    await waitFor(() => {
      expect(
        screen.getByDisplayValue(
          "駅から近く、設備も使いやすい素敵なスタジオでした。"
        )
      ).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(
        screen.getByText(
          "AIがレビュー文を整えました。内容を確認してから投稿してください。"
        )
      ).toBeInTheDocument();
    });
  });

  test("AI補助失敗時、APIメッセージを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockReviewPageResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({
          message: "AI補助を利用できません。",
        }),
      });

    render(<ReviewNewPage />);

    await waitForLoaded();

    await user.type(screen.getByLabelText("コメント"), "良かったです。");

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: "レビュー文を整える" })
      ).toBeEnabled();
    });

    await user.click(screen.getByRole("button", { name: "レビュー文を整える" }));

    await waitFor(() => {
      expect(screen.getByText("AI補助を利用できません。")).toBeInTheDocument();
    });
  });

  test("AI補助結果が空の場合、エラーメッセージを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockReviewPageResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          assistedContent: "",
        }),
      });

    render(<ReviewNewPage />);

    await waitForLoaded();

    await user.type(screen.getByLabelText("コメント"), "良かったです。");

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: "レビュー文を整える" })
      ).toBeEnabled();
    });

    await user.click(screen.getByRole("button", { name: "レビュー文を整える" }));

    await waitFor(() => {
      expect(
        screen.getByText("AIレビュー文補助の結果が空でした。")
      ).toBeInTheDocument();
    });
  });

  test("ページングで次ページを取得する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockReviewPageResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          ...mockReviewPageResponse(),
          page: 2,
        }),
      });

    render(<ReviewNewPage />);

    await waitForLoaded();

    await user.click(screen.getByRole("button", { name: "次" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch.mock.calls[1][0]).toBe(
      "https://localhost:7226/api/rooms/1/reviews/new?page=2&pageSize=10&reservationId=10"
    );
  });

  test("戻るリンクの遷移先が正しい", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByRole("link", { name: "戻る" })).toHaveAttribute(
      "href",
      "/reservations"
    );
  });

  test("API呼び出しURLが正しい", async () => {
    setupSuccessInitialRender();

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/rooms/1/reviews/new?page=1&pageSize=10&reservationId=10",
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      })
    );
  });
});