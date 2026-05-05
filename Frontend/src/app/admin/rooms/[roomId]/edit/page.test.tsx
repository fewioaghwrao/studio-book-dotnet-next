import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import AdminRoomEditPage from "./page";

const mockFetch = jest.fn();

jest.mock("next/navigation", () => ({
  useParams: () => ({
    roomId: "1",
  }),
}));

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

function mockRoomResponse() {
  return {
    id: 1,
    userId: 10,
    hostName: "ホスト太郎",
    name: "池袋ダンスルーム",
    imageName: "room01.jpg",
    description: "駅近のダンス向けスタジオです。",
    price: 3000,
    capacity: 6,
    postalCode: "170-0013",
    address: "東京都豊島区東池袋",
  };
}

function mockHostOptionsResponse() {
  return [
    {
      id: 10,
      name: "ホスト太郎",
      email: "host@example.com",
    },
    {
      id: 11,
      name: "ホスト次郎",
      email: "host2@example.com",
    },
  ];
}

describe("AdminRoomEditPage", () => {
  test("読み込み中メッセージを表示する", () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockRoomResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockHostOptionsResponse(),
      });

    render(<AdminRoomEditPage />);

    expect(screen.getByText("編集画面を読み込み中...")).toBeInTheDocument();
  });

  test("初期データ取得後、スタジオ編集フォームを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockRoomResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockHostOptionsResponse(),
      });

    render(<AdminRoomEditPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "スタジオ基本情報編集" })
      ).toBeInTheDocument();
    });

    expect(screen.getByDisplayValue("池袋ダンスルーム")).toBeInTheDocument();
    expect(
      screen.getByDisplayValue("駅近のダンス向けスタジオです。")
    ).toBeInTheDocument();
    expect(screen.getByDisplayValue("3000")).toBeInTheDocument();
    expect(screen.getByDisplayValue("6")).toBeInTheDocument();
    expect(screen.getByDisplayValue("170-0013")).toBeInTheDocument();
    expect(screen.getByDisplayValue("東京都豊島区東池袋")).toBeInTheDocument();

    expect(
      screen.getByRole("option", {
        name: "ホスト太郎（host@example.com）",
      })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("option", {
        name: "ホスト次郎（host2@example.com）",
      })
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "公開デモでは画像アップロードの代わりに、あらかじめ用意したサンプル画像を選択する形式にしています。"
      )
    ).toBeInTheDocument();

    expect(screen.getByAltText("池袋ダンスルーム")).toHaveAttribute(
      "src",
      "/storage/room01.jpg"
    );
  });

test("戻るリンクの遷移先がスタジオ基本情報画面である", async () => {
  mockFetch
    .mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockRoomResponse(),
    })
    .mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockHostOptionsResponse(),
    });

  render(<AdminRoomEditPage />);

  await waitFor(() => {
    expect(screen.getByDisplayValue("池袋ダンスルーム")).toBeInTheDocument();
  });

  expect(screen.getByRole("link", { name: "戻る" })).toHaveAttribute(
    "href",
    "/admin/rooms/1"
  );
});

  test("403の場合、管理者のみアクセス可能メッセージを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: false,
        status: 403,
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockHostOptionsResponse(),
      });

    render(<AdminRoomEditPage />);

    await waitFor(() => {
      expect(screen.getByText("管理者のみアクセスできます。")).toBeInTheDocument();
    });

    expect(screen.getByRole("link", { name: "スタジオ一覧へ戻る" }))
      .toHaveAttribute("href", "/admin/rooms");
  });

  test("404の場合、スタジオが見つからないメッセージを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: false,
        status: 404,
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockHostOptionsResponse(),
      });

    render(<AdminRoomEditPage />);

    await waitFor(() => {
      expect(screen.getByText("スタジオが見つかりません。")).toBeInTheDocument();
    });

    expect(screen.getByRole("link", { name: "スタジオ一覧へ戻る" }))
      .toHaveAttribute("href", "/admin/rooms");
  });

  test("初期データ取得失敗時、エラーメッセージを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: false,
        status: 500,
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockHostOptionsResponse(),
      });

    render(<AdminRoomEditPage />);

    await waitFor(() => {
      expect(
        screen.getByText("スタジオ編集情報の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<AdminRoomEditPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("スタジオ名未入力の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockRoomResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockHostOptionsResponse(),
      });

    render(<AdminRoomEditPage />);

    const nameInput = await screen.findByDisplayValue("池袋ダンスルーム");

    await user.clear(nameInput);
    await user.click(screen.getByRole("button", { name: "更新" }));

    expect(
      screen.getByText("スタジオ名を入力してください。")
    ).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(2);
  });

  test("基本料金が0以下の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockRoomResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockHostOptionsResponse(),
      });

    render(<AdminRoomEditPage />);

    const priceInput = await screen.findByDisplayValue("3000");

    await user.clear(priceInput);
    await user.type(priceInput, "0");

    await user.click(screen.getByRole("button", { name: "更新" }));

    expect(
      screen.getByText("基本料金は1円以上で入力してください。")
    ).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(2);
  });

  test("最大定員が0以下の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockRoomResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockHostOptionsResponse(),
      });

    render(<AdminRoomEditPage />);

    const capacityInput = await screen.findByDisplayValue("6");

    await user.clear(capacityInput);
    await user.type(capacityInput, "0");

    await user.click(screen.getByRole("button", { name: "更新" }));

    expect(
      screen.getByText("最大定員は1人以上で入力してください。")
    ).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(2);
  });

  test("フォームを更新するとPUTリクエストを送信する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockRoomResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockHostOptionsResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
      });

    render(<AdminRoomEditPage />);

    const nameInput = await screen.findByDisplayValue("池袋ダンスルーム");
    const priceInput = screen.getByDisplayValue("3000");
    const capacityInput = screen.getByDisplayValue("6");

    await user.clear(nameInput);
    await user.type(nameInput, "池袋ダンスルーム 改修後");

    await user.clear(priceInput);
    await user.type(priceInput, "3500");

    await user.clear(capacityInput);
    await user.type(capacityInput, "8");

    await user.click(screen.getByRole("button", { name: "更新" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(3);
    });

    expect(mockFetch).toHaveBeenLastCalledWith(
      "https://localhost:7226/api/admin/rooms/1",
      expect.objectContaining({
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },        body: JSON.stringify({
          userId: 10,
          name: "池袋ダンスルーム 改修後",
          imageName: "room01.jpg",
          description: "駅近のダンス向けスタジオです。",
          price: 3500,
          capacity: 8,
          postalCode: "170-0013",
          address: "東京都豊島区東池袋",
        }),
      })
    );

    expect(
      screen.getByText("スタジオ情報を更新しました。")
    ).toBeInTheDocument();
  });

  test("PUT失敗時、APIから返されたエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockRoomResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockHostOptionsResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({
          message: "スタジオ名は既に使用されています。",
        }),
      });

    render(<AdminRoomEditPage />);

    await screen.findByDisplayValue("池袋ダンスルーム");

    await user.click(screen.getByRole("button", { name: "更新" }));

    await waitFor(() => {
      expect(
        screen.getByText("スタジオ名は既に使用されています。")
      ).toBeInTheDocument();
    });
  });
});