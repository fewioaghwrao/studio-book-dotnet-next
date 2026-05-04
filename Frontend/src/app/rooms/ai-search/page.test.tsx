import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import AiRoomSearchPage from "./page";

const mockFetch = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

function mockAiRoomSearchResponse() {
  return {
    query: "落ち着いた雰囲気で、夜に使える撮影向けのスタジオを探したい",
    interpretedConditions: {
      keyword: "撮影",
      area: "新宿区",
      price: 10000,
      capacity: 10,
      capacityCondition: "max",
      purpose: "撮影",
      atmosphere: "落ち着いた雰囲気",
      timePreference: "夜",
      keywords: ["撮影", "夜", "落ち着いた"],
    },
    rooms: [
      {
        id: 1,
        name: "池袋ダンスルーム",
        imageName: "room01.jpg",
        description: "落ち着いた雰囲気で撮影にも使えるスタジオです。",
        postalCode: "170-0013",
        address: "東京都豊島区東池袋",
        price: 3000,
        capacity: 8,
        averageScore: 4.5,
        reviewCount: 12,
        reason:
          "夜の撮影用途に合いやすく、落ち着いた雰囲気の条件に近いためおすすめです。",
      },
      {
        id: 2,
        name: "渋谷撮影スタジオ",
        imageName: null,
        description: "",
        postalCode: "150-0002",
        address: "東京都渋谷区渋谷",
        price: 4500,
        capacity: 12,
        averageScore: null,
        reviewCount: 0,
        reason: "",
      },
    ],
  };
}

function setupSuccessResponse() {
  mockFetch.mockResolvedValueOnce({
    ok: true,
    status: 200,
    json: async () => mockAiRoomSearchResponse(),
  });
}

describe("AiRoomSearchPage", () => {
  test("初期表示を行う", () => {
    render(<AiRoomSearchPage />);

    expect(screen.getByText("AI STUDIO SEARCH")).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "AIでスタジオを探す" })
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        /「落ち着いた雰囲気」「夜に使いたい」「撮影向け」など/
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText(/この機能はポートフォリオ用のデモ機能です/)
    ).toBeInTheDocument();

    expect(
      screen.getByLabelText("探したいスタジオの希望条件")
    ).toBeInTheDocument();

    expect(screen.getByRole("button", { name: "AIで探す" })).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "通常検索へ戻る" })).toHaveAttribute(
      "href",
      "/rooms"
    );

    expect(screen.getByText("入力例")).toBeInTheDocument();
  });

  test("パンくずリンクを表示する", () => {
    render(<AiRoomSearchPage />);

    expect(screen.getByRole("link", { name: "ホーム" })).toHaveAttribute(
      "href",
      "/"
    );

    expect(screen.getByRole("link", { name: "スタジオ一覧" })).toHaveAttribute(
      "href",
      "/rooms"
    );

    expect(screen.getByText("AIスタジオ検索")).toBeInTheDocument();
  });

  test("初期入力値と文字数を表示する", () => {
    render(<AiRoomSearchPage />);

    expect(
      screen.getByDisplayValue(
        "落ち着いた雰囲気で、夜に使える撮影向けのスタジオを探したい"
      )
    ).toBeInTheDocument();

    expect(screen.getByText("29 / 200")).toBeInTheDocument();
  });

  test("入力例をクリックすると検索文に反映する", async () => {
    const user = userEvent.setup();

    render(<AiRoomSearchPage />);

    await user.click(
      screen.getByRole("button", {
        name: "10人くらいで使える、駅近っぽい配信向けスタジオ",
      })
    );

    expect(
      screen.getByDisplayValue(
        "10人くらいで使える、駅近っぽい配信向けスタジオ"
      )
    ).toBeInTheDocument();
  });

  test("未入力で検索するとエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    render(<AiRoomSearchPage />);

    const textarea = screen.getByLabelText("探したいスタジオの希望条件");

    await user.clear(textarea);
    await user.click(screen.getByRole("button", { name: "AIで探す" }));

    expect(
      screen.getByText("検索したい内容を入力してください。")
    ).toBeInTheDocument();

    expect(mockFetch).not.toHaveBeenCalled();
  });

  test("200文字まで入力できる", async () => {
    const user = userEvent.setup();

    render(<AiRoomSearchPage />);

    const textarea = screen.getByLabelText("探したいスタジオの希望条件");

    await user.clear(textarea);
    await user.type(textarea, "a".repeat(201));

    expect(textarea).toHaveValue("a".repeat(200));
    expect(screen.getByText("200 / 200")).toBeInTheDocument();
  });

  test("検索中メッセージとボタン文言を表示する", async () => {
    const user = userEvent.setup();

    mockFetch.mockImplementation(
      () =>
        new Promise((resolve) => {
          setTimeout(() => {
            resolve({
              ok: true,
              status: 200,
              json: async () => mockAiRoomSearchResponse(),
            });
          }, 100);
        })
    );

    render(<AiRoomSearchPage />);

    await user.click(screen.getByRole("button", { name: "AIで探す" }));

    expect(screen.getByRole("button", { name: "AI検索中..." })).toBeDisabled();

    expect(
      screen.getByText("AIが検索条件を解析しています...")
    ).toBeInTheDocument();
  });

  test("API成功時、AI解釈条件とおすすめ結果を表示する", async () => {
    const user = userEvent.setup();

    setupSuccessResponse();

    render(<AiRoomSearchPage />);

    await user.click(screen.getByRole("button", { name: "AIで探す" }));

    await waitFor(() => {
      expect(screen.getByText("AIが解釈した条件")).toBeInTheDocument();
    });

    expect(screen.getByText("用途")).toBeInTheDocument();
    expect(screen.getAllByText("撮影").length).toBeGreaterThan(0);

    expect(screen.getByText("雰囲気")).toBeInTheDocument();
    expect(screen.getByText("落ち着いた雰囲気")).toBeInTheDocument();

    expect(screen.getByText("エリア")).toBeInTheDocument();
    expect(screen.getByText("新宿区")).toBeInTheDocument();

    expect(screen.getByText("予算")).toBeInTheDocument();
    expect(screen.getByText("10,000円以内")).toBeInTheDocument();

    expect(screen.getByText("人数")).toBeInTheDocument();
    expect(screen.getByText("10人以下")).toBeInTheDocument();

  expect(screen.getByText("時間帯")).toBeInTheDocument();
expect(screen.getAllByText("夜").length).toBeGreaterThan(0);

    expect(screen.getByText("キーワード")).toBeInTheDocument();
    expect(screen.getAllByText("撮影").length).toBeGreaterThan(0);
    expect(screen.getByText("落ち着いた")).toBeInTheDocument();

    expect(screen.getByText("検索文")).toBeInTheDocument();

    expect(screen.getByText("AIおすすめ結果：2件")).toBeInTheDocument();

    expect(screen.getByText("池袋ダンスルーム")).toBeInTheDocument();
    expect(screen.getByText("渋谷撮影スタジオ")).toBeInTheDocument();

    expect(
      screen.getByText(
        "夜の撮影用途に合いやすく、落ち着いた雰囲気の条件に近いためおすすめです。"
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "入力された希望条件に関連するスタジオとして候補に入りました。"
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText("落ち着いた雰囲気で撮影にも使えるスタジオです。")
    ).toBeInTheDocument();

    expect(screen.getByText("説明はありません。")).toBeInTheDocument();

    expect(screen.getByText("〒170-0013")).toBeInTheDocument();
    expect(screen.getByText("東京都豊島区東池袋")).toBeInTheDocument();

    expect(screen.getByText("〒150-0002")).toBeInTheDocument();
    expect(screen.getByText("東京都渋谷区渋谷")).toBeInTheDocument();

    expect(screen.getByText("3,000円 / h")).toBeInTheDocument();
    expect(screen.getByText("4,500円 / h")).toBeInTheDocument();

    expect(screen.getByText("定員：8人")).toBeInTheDocument();
    expect(screen.getByText("定員：12人")).toBeInTheDocument();

    expect(screen.getByText("4.5 / 5.0")).toBeInTheDocument();
    expect(screen.getByText("（12件）")).toBeInTheDocument();
    expect(screen.getByText("レビューなし")).toBeInTheDocument();
  });

  test("画像あり・画像なしの表示を確認する", async () => {
    const user = userEvent.setup();

    setupSuccessResponse();

    render(<AiRoomSearchPage />);

    await user.click(screen.getByRole("button", { name: "AIで探す" }));

    await waitFor(() => {
      expect(screen.getByText("AIおすすめ結果：2件")).toBeInTheDocument();
    });

    expect(screen.getByAltText("池袋ダンスルーム")).toHaveAttribute(
      "src",
      "/storage/room01.jpg"
    );

    expect(screen.getByAltText("渋谷撮影スタジオ")).toHaveAttribute(
      "src",
      "/images/noImage.png"
    );
  });

  test("検索結果カードのリンク先が正しい", async () => {
    const user = userEvent.setup();

    setupSuccessResponse();

    render(<AiRoomSearchPage />);

    await user.click(screen.getByRole("button", { name: "AIで探す" }));

    await waitFor(() => {
      expect(screen.getByText("AIおすすめ結果：2件")).toBeInTheDocument();
    });

    expect(
      screen.getByRole("link", { name: /池袋ダンスルーム/ })
    ).toHaveAttribute("href", "/rooms/1");

    expect(
      screen.getByRole("link", { name: /渋谷撮影スタジオ/ })
    ).toHaveAttribute("href", "/rooms/2");
  });

  test("検索結果0件の場合、空メッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockAiRoomSearchResponse(),
        rooms: [],
        interpretedConditions: {
          ...mockAiRoomSearchResponse().interpretedConditions,
          price: null,
          capacity: null,
          capacityCondition: null,
          keywords: [],
        },
      }),
    });

    render(<AiRoomSearchPage />);

    await user.click(screen.getByRole("button", { name: "AIで探す" }));

    await waitFor(() => {
      expect(screen.getByText("AIおすすめ結果：0件")).toBeInTheDocument();
    });

    expect(
      screen.getByText("条件に近いスタジオは見つかりませんでした。")
    ).toBeInTheDocument();

    expect(
      screen.getByText("条件を少し広げて、もう一度検索してみてください。")
    ).toBeInTheDocument();

    expect(screen.getAllByText("指定なし").length).toBeGreaterThan(0);
  });

  test("429の場合、利用回数上限メッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 429,
    });

    render(<AiRoomSearchPage />);

    await user.click(screen.getByRole("button", { name: "AIで探す" }));

    await waitFor(() => {
      expect(
        screen.getByText(
          "AI検索の利用回数が一時的に上限に達しました。少し時間をおいてから再度お試しください。"
        )
      ).toBeInTheDocument();
    });
  });

  test("API失敗時、APIメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 400,
      json: async () => ({
        message: "検索条件を解釈できませんでした。",
      }),
    });

    render(<AiRoomSearchPage />);

    await user.click(screen.getByRole("button", { name: "AIで探す" }));

    await waitFor(() => {
      expect(
        screen.getByText("検索条件を解釈できませんでした。")
      ).toBeInTheDocument();
    });
  });

  test("API失敗時、message がない場合は既定メッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
      json: async () => ({}),
    });

    render(<AiRoomSearchPage />);

    await user.click(screen.getByRole("button", { name: "AIで探す" }));

    await waitFor(() => {
      expect(
        screen.getByText(
          "AI検索の実行に失敗しました。時間をおいて再度お試しください。"
        )
      ).toBeInTheDocument();
    });
  });

  test("API失敗時、JSON解析できない場合も既定メッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
      json: async () => {
        throw new Error("Invalid JSON");
      },
    });

    render(<AiRoomSearchPage />);

    await user.click(screen.getByRole("button", { name: "AIで探す" }));

    await waitFor(() => {
      expect(
        screen.getByText(
          "AI検索の実行に失敗しました。時間をおいて再度お試しください。"
        )
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<AiRoomSearchPage />);

    await user.click(screen.getByRole("button", { name: "AIで探す" }));

    await waitFor(() => {
      expect(
        screen.getByText(
          "通信エラーが発生しました。バックエンドの起動状態を確認してください。"
        )
      ).toBeInTheDocument();
    });
  });

  test("API呼び出し内容が正しい", async () => {
    const user = userEvent.setup();

    setupSuccessResponse();

    render(<AiRoomSearchPage />);

    await user.click(screen.getByRole("button", { name: "AIで探す" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/ai/room-search",
      expect.objectContaining({
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        cache: "no-store",
        body: JSON.stringify({
          query:
            "落ち着いた雰囲気で、夜に使える撮影向けのスタジオを探したい",
        }),
      })
    );
  });
});