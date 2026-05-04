import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import RoomsPage from "./page";

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

function mockRoomsResponse() {
  return {
    items: [
      {
        id: 1,
        name: "池袋ダンスルーム",
        imageName: "room01.jpg",
        description: "駅近でダンス練習に使いやすいスタジオです。",
        postalCode: "170-0013",
        address: "東京都豊島区東池袋",
        price: 3000,
        averageScore: 4.5,
        reviewCount: 12,
      },
      {
        id: 2,
        name: "渋谷撮影スタジオ",
        imageName: null,
        description: "",
        postalCode: "150-0002",
        address: "東京都渋谷区渋谷",
        price: 4500,
        averageScore: null,
        reviewCount: 0,
      },
    ],
    keyword: "",
    area: "",
    price: null,
    order: "createdAtDesc",
    page: 1,
    pageSize: 10,
    totalCount: 2,
    totalPages: 1,
  };
}

function setupSuccessInitialRender() {
  mockFetch.mockResolvedValueOnce({
    ok: true,
    status: 200,
    json: async () => mockRoomsResponse(),
  });

  render(<RoomsPage />);
}

async function waitForLoaded() {
  await waitFor(() => {
    expect(screen.getByText("池袋ダンスルーム")).toBeInTheDocument();
  });
}

describe("RoomsPage", () => {
  test("初期表示と読み込み中メッセージを表示する", () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockRoomsResponse(),
    });

    render(<RoomsPage />);

    expect(
      screen.getByRole("heading", { name: "スタジオ一覧" })
    ).toBeInTheDocument();

    expect(screen.getByText("STUDIO LIST")).toBeInTheDocument();

    expect(
      screen.getByText(
        "架空のスタジオデータをもとに、キーワード・エリア・予算で検索できます。"
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText("スタジオ一覧を読み込み中...")
    ).toBeInTheDocument();
  });

  test("API成功時、スタジオ一覧を表示する", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByText("池袋ダンスルーム")).toBeInTheDocument();
    expect(screen.getByText("渋谷撮影スタジオ")).toBeInTheDocument();

    expect(
      screen.getByText("駅近でダンス練習に使いやすいスタジオです。")
    ).toBeInTheDocument();

    expect(screen.getByText("説明はありません。")).toBeInTheDocument();

    expect(screen.getByText("〒170-0013")).toBeInTheDocument();
    expect(screen.getByText("東京都豊島区東池袋")).toBeInTheDocument();

    expect(screen.getByText("〒150-0002")).toBeInTheDocument();
    expect(screen.getByText("東京都渋谷区渋谷")).toBeInTheDocument();

    expect(screen.getByText("3,000円 / h")).toBeInTheDocument();
    expect(screen.getByText("4,500円 / h")).toBeInTheDocument();

    expect(screen.getByText("4.5 / 5.0")).toBeInTheDocument();
    expect(screen.getByText("（12件）")).toBeInTheDocument();
    expect(screen.getByText("レビューなし")).toBeInTheDocument();

    expect(screen.getByText("検索結果：2件")).toBeInTheDocument();
  });

  test("画像がある場合はstorage、ない場合はnoImageを表示する", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByAltText("池袋ダンスルーム")).toHaveAttribute(
      "src",
      "/storage/room01.jpg"
    );

    expect(screen.getByAltText("渋谷撮影スタジオ")).toHaveAttribute(
      "src",
      "/images/noImage.png"
    );
  });

  test("スタジオ詳細リンクの遷移先が正しい", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(
      screen.getByRole("link", { name: /池袋ダンスルーム/ })
    ).toHaveAttribute("href", "/rooms/1");

    expect(
      screen.getByRole("link", { name: /渋谷撮影スタジオ/ })
    ).toHaveAttribute("href", "/rooms/2");
  });

  test("AI検索リンクを表示する", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(
      screen.getByRole("link", { name: "AIで希望に合うスタジオを探す" })
    ).toHaveAttribute("href", "/rooms/ai-search");

    expect(
      screen.getByText("自然文で「夜に使える撮影向け」などの希望を入力できます。")
    ).toBeInTheDocument();
  });

  test("0件の場合、空メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        items: [],
        keyword: "",
        area: "",
        price: null,
        order: "createdAtDesc",
        page: 1,
        pageSize: 10,
        totalCount: 0,
        totalPages: 1,
      }),
    });

    render(<RoomsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("条件に一致するスタジオはありません。")
      ).toBeInTheDocument();
    });

    expect(screen.getByText("検索結果：0件")).toBeInTheDocument();
  });

  test("API失敗時、取得失敗メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<RoomsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("スタジオ一覧の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<RoomsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("キーワード検索すると router.push する", async () => {
    const user = userEvent.setup();

    setupSuccessInitialRender();

    await waitForLoaded();

    await user.type(
      screen.getByPlaceholderText("スタジオ名・所在地"),
      "池袋 ダンス"
    );

    await user.click(screen.getAllByRole("button", { name: "検索" })[0]);

    expect(mockPush).toHaveBeenCalledWith(
      "/rooms?keyword=%E6%B1%A0%E8%A2%8B+%E3%83%80%E3%83%B3%E3%82%B9&order=createdAtDesc&page=1&pageSize=10"
    );
  });

  test("エリア検索すると router.push する", async () => {
    const user = userEvent.setup();

    setupSuccessInitialRender();

    await waitForLoaded();

    const selects = screen.getAllByRole("combobox");

    await user.selectOptions(selects[0], "豊島区");
    await user.click(screen.getAllByRole("button", { name: "検索" })[1]);

    expect(mockPush).toHaveBeenCalledWith(
      "/rooms?area=%E8%B1%8A%E5%B3%B6%E5%8C%BA&order=createdAtDesc&page=1&pageSize=10"
    );
  });

  test("価格検索すると router.push する", async () => {
    const user = userEvent.setup();

    setupSuccessInitialRender();

    await waitForLoaded();

    const selects = screen.getAllByRole("combobox");

    await user.selectOptions(selects[1], "5000");
    await user.click(screen.getAllByRole("button", { name: "検索" })[2]);

    expect(mockPush).toHaveBeenCalledWith(
      "/rooms?price=5000&order=createdAtDesc&page=1&pageSize=10"
    );
  });

  test("並び替えを変更すると router.push する", async () => {
    const user = userEvent.setup();

    setupSuccessInitialRender();

    await waitForLoaded();

    const orderSelect = screen.getByDisplayValue("新着順");

    await user.selectOptions(orderSelect, "priceAsc");

    expect(mockPush).toHaveBeenCalledWith(
      "/rooms?order=priceAsc&page=1&pageSize=10"
    );
  });

  test("検索条件付き初期表示の場合、条件表示とクリアボタンを表示する", async () => {
    mockSearchParams = new URLSearchParams(
      "keyword=池袋&area=豊島区&price=5000&order=priceAsc&page=1"
    );

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockRoomsResponse(),
        totalCount: 1,
        totalPages: 1,
      }),
    });

    render(<RoomsPage />);

    await waitForLoaded();

    expect(screen.getByDisplayValue("池袋")).toBeInTheDocument();
    expect(screen.getByDisplayValue("豊島区")).toBeInTheDocument();
    expect(screen.getByDisplayValue("5,000円以内")).toBeInTheDocument();
    expect(screen.getByDisplayValue("料金が安い順")).toBeInTheDocument();

    expect(screen.getByText(/キーワード：池袋/)).toBeInTheDocument();
    expect(screen.getByText(/エリア：豊島区/)).toBeInTheDocument();
    expect(screen.getByText(/予算：5,000円以内/)).toBeInTheDocument();

    expect(
      screen.getByRole("button", { name: "条件をクリア" })
    ).toBeInTheDocument();
  });

  test("条件をクリアすると /rooms へ遷移する", async () => {
    const user = userEvent.setup();

    mockSearchParams = new URLSearchParams("keyword=池袋");

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockRoomsResponse(),
    });

    render(<RoomsPage />);

    await waitForLoaded();

    await user.click(screen.getByRole("button", { name: "条件をクリア" }));

    expect(mockPush).toHaveBeenCalledWith("/rooms");
  });

  test("ページングで次ページへ遷移する", async () => {
    const user = userEvent.setup();

    mockSearchParams = new URLSearchParams("page=1");

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockRoomsResponse(),
        page: 1,
        totalCount: 25,
        totalPages: 3,
      }),
    });

    render(<RoomsPage />);

    await waitForLoaded();

    await user.click(screen.getByRole("button", { name: "次" }));

    expect(mockPush).toHaveBeenCalledWith(
      "/rooms?order=createdAtDesc&page=2&pageSize=10"
    );
  });

  test("検索条件を保持したままページングする", async () => {
    const user = userEvent.setup();

    mockSearchParams = new URLSearchParams("keyword=池袋&page=1");

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        ...mockRoomsResponse(),
        page: 1,
        totalCount: 25,
        totalPages: 3,
      }),
    });

    render(<RoomsPage />);

    await waitForLoaded();

    await user.click(screen.getByRole("button", { name: "次" }));

    expect(mockPush).toHaveBeenCalledWith(
      "/rooms?keyword=%E6%B1%A0%E8%A2%8B&order=createdAtDesc&page=2&pageSize=10"
    );
  });

  test("API呼び出しURLが正しい", async () => {
    setupSuccessInitialRender();

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/rooms?order=createdAtDesc&page=1&pageSize=10",
      expect.objectContaining({
        method: "GET",
        cache: "no-store",
      })
    );
  });

  test("クエリ付き初期表示の場合、API呼び出しURLが正しい", async () => {
    mockSearchParams = new URLSearchParams(
      "keyword=池袋&area=豊島区&price=5000&order=priceAsc&page=2"
    );

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockRoomsResponse(),
    });

    render(<RoomsPage />);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/rooms?keyword=%E6%B1%A0%E8%A2%8B&area=%E8%B1%8A%E5%B3%B6%E5%8C%BA&price=5000&order=priceAsc&page=2&pageSize=10",
      expect.objectContaining({
        method: "GET",
        cache: "no-store",
      })
    );
  });
});