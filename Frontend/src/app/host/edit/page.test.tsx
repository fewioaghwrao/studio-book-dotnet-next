import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import HostEditPage from "./page";

const mockFetch = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

function mockHostUserResponse() {
  return {
    isAuthenticated: true,
    user: {
      id: 1,
      name: "ホスト 太郎",
      kana: "ホスト タロウ",
      email: "host@example.com",
      postalCode: "101-0022",
      address: "東京都千代田区神田練塀町300番地",
      phoneNumber: "090-1234-5678",
      usageType: "business",
      roles: ["Host"],
    },
  };
}

async function setupSuccessInitialRender() {
  mockFetch.mockResolvedValueOnce({
    ok: true,
    status: 200,
    json: async () => mockHostUserResponse(),
  });

  render(<HostEditPage />);

  await waitFor(() => {
    expect(
      screen.getByRole("heading", { name: "会員情報編集（ホスト）" })
    ).toBeInTheDocument();
  });
}

describe("HostEditPage", () => {
  test("読み込み中メッセージを表示する", () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockHostUserResponse(),
    });

    render(<HostEditPage />);

    expect(screen.getByText("会員情報を読み込み中...")).toBeInTheDocument();
  });

  test("会員情報を取得してフォームに表示する", async () => {
    await setupSuccessInitialRender();

    expect(screen.getByText("HOST EDIT PROFILE")).toBeInTheDocument();

    expect(
      screen.getByText("スタジオ提供者として登録されている会員情報を変更できます。")
    ).toBeInTheDocument();

    expect(screen.getByDisplayValue("ホスト 太郎")).toBeInTheDocument();
    expect(screen.getByDisplayValue("ホスト タロウ")).toBeInTheDocument();
    expect(screen.getByDisplayValue("101-0022")).toBeInTheDocument();
    expect(
      screen.getByDisplayValue("東京都千代田区神田練塀町300番地")
    ).toBeInTheDocument();
    expect(screen.getByDisplayValue("090-1234-5678")).toBeInTheDocument();
    expect(screen.getByDisplayValue("host@example.com")).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "戻る" })).toHaveAttribute(
      "href",
      "/host"
    );
  });

  test("API失敗時、取得失敗メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<HostEditPage />);

    await waitFor(() => {
      expect(
        screen.getByText("会員情報の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("GET通信エラー時、通信エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<HostEditPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("氏名未入力の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    await setupSuccessInitialRender();

    const nameInput = screen.getByDisplayValue("ホスト 太郎");

    await user.clear(nameInput);
    await user.click(screen.getByRole("button", { name: "更新" }));

    expect(screen.getByText("氏名を入力してください。")).toBeInTheDocument();
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("フリガナ未入力の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    await setupSuccessInitialRender();

    const kanaInput = screen.getByDisplayValue("ホスト タロウ");

    await user.clear(kanaInput);
    await user.click(screen.getByRole("button", { name: "更新" }));

    expect(screen.getByText("フリガナを入力してください。")).toBeInTheDocument();
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("郵便番号未入力の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    await setupSuccessInitialRender();

    const postalCodeInput = screen.getByDisplayValue("101-0022");

    await user.clear(postalCodeInput);
    await user.click(screen.getByRole("button", { name: "更新" }));

    expect(screen.getByText("郵便番号を入力してください。")).toBeInTheDocument();
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("住所未入力の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    await setupSuccessInitialRender();

    const addressInput = screen.getByDisplayValue(
      "東京都千代田区神田練塀町300番地"
    );

    await user.clear(addressInput);
    await user.click(screen.getByRole("button", { name: "更新" }));

    expect(screen.getByText("住所を入力してください。")).toBeInTheDocument();
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("電話番号未入力の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    await setupSuccessInitialRender();

    const phoneInput = screen.getByDisplayValue("090-1234-5678");

    await user.clear(phoneInput);
    await user.click(screen.getByRole("button", { name: "更新" }));

    expect(screen.getByText("電話番号を入力してください。")).toBeInTheDocument();
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("メールアドレス未入力の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    await setupSuccessInitialRender();

    const emailInput = screen.getByDisplayValue("host@example.com");

    await user.clear(emailInput);
    await user.click(screen.getByRole("button", { name: "更新" }));

    expect(
      screen.getByText("メールアドレスを入力してください。")
    ).toBeInTheDocument();
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("メールアドレス形式不正の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    await setupSuccessInitialRender();

    const emailInput = screen.getByDisplayValue("host@example.com");

    await user.clear(emailInput);
    await user.type(emailInput, "invalid-email");

    await user.click(screen.getByRole("button", { name: "更新" }));

    expect(
      screen.getByText("メールアドレスの形式を確認してください。")
    ).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("正常入力で更新するとPUTリクエストを送信する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockHostUserResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
      });

    render(<HostEditPage />);

    const nameInput = await screen.findByDisplayValue("ホスト 太郎");
    const phoneInput = screen.getByDisplayValue("090-1234-5678");

    await user.clear(nameInput);
    await user.type(nameInput, "ホスト 太郎 改");

    await user.clear(phoneInput);
    await user.type(phoneInput, "080-1111-2222");

    await user.click(screen.getByRole("button", { name: "更新" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch).toHaveBeenLastCalledWith(
      "https://localhost:7226/api/auth/me",
      expect.objectContaining({
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        credentials: "include",
        body: JSON.stringify({
          name: "ホスト 太郎 改",
          kana: "ホスト タロウ",
          postalCode: "101-0022",
          address: "東京都千代田区神田練塀町300番地",
          phoneNumber: "080-1111-2222",
          email: "host@example.com",
        }),
      })
    );

    expect(screen.getByText("会員情報を更新しました。")).toBeInTheDocument();
  });

  test("PUT失敗時、APIから返されたエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockHostUserResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({
          message: "メールアドレスは既に使用されています。",
        }),
      });

    render(<HostEditPage />);

    await screen.findByDisplayValue("ホスト 太郎");

    await user.click(screen.getByRole("button", { name: "更新" }));

    await waitFor(() => {
      expect(
        screen.getByText("メールアドレスは既に使用されています。")
      ).toBeInTheDocument();
    });
  });

  test("PUT失敗時、message がない場合は既定メッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockHostUserResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: async () => ({}),
      });

    render(<HostEditPage />);

    await screen.findByDisplayValue("ホスト 太郎");

    await user.click(screen.getByRole("button", { name: "更新" }));

    await waitFor(() => {
      expect(
        screen.getByText("会員情報の更新に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("PUT通信エラー時、通信エラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockHostUserResponse(),
      })
      .mockRejectedValueOnce(new Error("Network Error"));

    render(<HostEditPage />);

    await screen.findByDisplayValue("ホスト 太郎");

    await user.click(screen.getByRole("button", { name: "更新" }));

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("API呼び出しURLが正しい", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockHostUserResponse(),
    });

    render(<HostEditPage />);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/auth/me",
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      })
    );
  });
});