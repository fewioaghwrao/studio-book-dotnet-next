import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import AdminRoomRegisterPage from "./page";

const mockFetch = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

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

async function setupSuccessInitialRender() {
  mockFetch.mockResolvedValueOnce({
    ok: true,
    status: 200,
    json: async () => mockHostOptionsResponse(),
  });

  render(<AdminRoomRegisterPage />);

  await waitFor(() => {
    expect(
      screen.getByRole("heading", { name: "スタジオ登録" })
    ).toBeInTheDocument();
  });
}

describe("AdminRoomRegisterPage", () => {
  test("読み込み中メッセージを表示する", () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockHostOptionsResponse(),
    });

    render(<AdminRoomRegisterPage />);

    expect(screen.getByText("登録画面を読み込み中...")).toBeInTheDocument();
  });

  test("初期データ取得後、スタジオ登録フォームを表示する", async () => {
    await setupSuccessInitialRender();

    expect(
      screen.getByText("管理者としてスタジオの基本情報を登録できます。")
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "公開デモでは画像アップロードの代わりに、あらかじめ用意したサンプル画像を選択する形式にしています。"
      )
    ).toBeInTheDocument();

    expect(screen.getByAltText("スタジオ画像")).toHaveAttribute(
      "src",
      "/storage/room01.jpg"
    );

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

    expect(screen.getByRole("link", { name: "戻る" })).toHaveAttribute(
      "href",
      "/admin/rooms"
    );
  });

  test("403の場合、管理者のみアクセス可能メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 403,
    });

    render(<AdminRoomRegisterPage />);

    await waitFor(() => {
      expect(screen.getByText("管理者のみアクセスできます。")).toBeInTheDocument();
    });
  });

  test("スタジオ提供者取得失敗時、エラーメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<AdminRoomRegisterPage />);

    await waitFor(() => {
      expect(
        screen.getByText("スタジオ提供者の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<AdminRoomRegisterPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("スタジオ提供者未選択の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    await setupSuccessInitialRender();

    await user.click(screen.getByRole("button", { name: "登録" }));

    expect(
      screen.getByText("スタジオ提供者を選択してください。")
    ).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("スタジオ名未入力の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    await setupSuccessInitialRender();

    await user.selectOptions(
      screen.getByDisplayValue("-- 選択してください --"),
      "10"
    );

    await user.click(screen.getByRole("button", { name: "登録" }));

    expect(
      screen.getByText("スタジオ名を入力してください。")
    ).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("基本料金が0以下の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    await setupSuccessInitialRender();

    await user.selectOptions(
      screen.getByDisplayValue("-- 選択してください --"),
      "10"
    );
    await user.type(
      screen.getByPlaceholderText("秋葉原ダンススタジオ A"),
      "池袋ダンスルーム"
    );
    await user.type(screen.getByPlaceholderText("3000"), "0");

    await user.click(screen.getByRole("button", { name: "登録" }));

    expect(
      screen.getByText("基本料金は1円以上で入力してください。")
    ).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("最大定員が0以下の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    await setupSuccessInitialRender();

    await user.selectOptions(
      screen.getByDisplayValue("-- 選択してください --"),
      "10"
    );
    await user.type(
      screen.getByPlaceholderText("秋葉原ダンススタジオ A"),
      "池袋ダンスルーム"
    );
    await user.type(screen.getByPlaceholderText("3000"), "3000");
    await user.type(screen.getByPlaceholderText("6"), "0");

    await user.click(screen.getByRole("button", { name: "登録" }));

    expect(
      screen.getByText("最大定員は1人以上で入力してください。")
    ).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("必須項目を入力して登録するとPOSTリクエストを送信する", async () => {
    const user = userEvent.setup();

    mockFetch
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

    render(<AdminRoomRegisterPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "スタジオ登録" })
      ).toBeInTheDocument();
    });

    await user.selectOptions(
      screen.getByDisplayValue("-- 選択してください --"),
      "10"
    );

    await user.type(
      screen.getByPlaceholderText("秋葉原ダンススタジオ A"),
      "池袋ダンスルーム"
    );

    await user.type(
      screen.getByPlaceholderText("駅近で使いやすい多目的スタジオです。"),
      "駅近のダンス向けスタジオです。"
    );

    await user.type(screen.getByPlaceholderText("3000"), "3500");
    await user.type(screen.getByPlaceholderText("6"), "8");
    await user.type(screen.getByPlaceholderText("101-0022"), "170-0013");

    await user.type(
      screen.getByPlaceholderText("東京都千代田区神田練塀町300番地"),
      "東京都豊島区東池袋"
    );

    await user.click(screen.getByRole("button", { name: "登録" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch).toHaveBeenLastCalledWith(
      "https://localhost:7226/api/admin/rooms",
      expect.objectContaining({
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        credentials: "include",
        body: JSON.stringify({
          userId: 10,
          name: "池袋ダンスルーム",
          imageName: "room01.jpg",
          description: "駅近のダンス向けスタジオです。",
          price: 3500,
          capacity: 8,
          postalCode: "170-0013",
          address: "東京都豊島区東池袋",
        }),
      })
    );

    expect(screen.getByText("スタジオを登録しました。")).toBeInTheDocument();
  });

  test("POST失敗時、APIから返されたエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
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

    render(<AdminRoomRegisterPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "スタジオ登録" })
      ).toBeInTheDocument();
    });

    await user.selectOptions(
      screen.getByDisplayValue("-- 選択してください --"),
      "10"
    );

    await user.type(
      screen.getByPlaceholderText("秋葉原ダンススタジオ A"),
      "池袋ダンスルーム"
    );
    await user.type(screen.getByPlaceholderText("3000"), "3500");
    await user.type(screen.getByPlaceholderText("6"), "8");
    await user.type(screen.getByPlaceholderText("101-0022"), "170-0013");
    await user.type(
      screen.getByPlaceholderText("東京都千代田区神田練塀町300番地"),
      "東京都豊島区東池袋"
    );

    await user.click(screen.getByRole("button", { name: "登録" }));

    await waitFor(() => {
      expect(
        screen.getByText("スタジオ名は既に使用されています。")
      ).toBeInTheDocument();
    });
  });
});