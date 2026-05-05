import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import UserEditPage from "./page";

const mockFetch = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

function mockUserResponse() {
  return {
    isAuthenticated: true,
    user: {
      id: 1,
      name: "一般 太郎",
      kana: "イッパン タロウ",
      email: "user@example.com",
      postalCode: "123-4567",
      address: "東京都新宿区サンプル1-2-3",
      phoneNumber: "09012345678",
      usageType: "personal",
      roles: ["GeneralUser"],
    },
  };
}

function setupInitialLoadSuccess() {
  mockFetch.mockResolvedValueOnce({
    ok: true,
    status: 200,
    json: async () => mockUserResponse(),
  });
}

async function waitForLoaded() {
  await waitFor(() => {
    expect(
      screen.getByRole("heading", { name: "会員情報編集" })
    ).toBeInTheDocument();
  });
}

function changeInput(label: string, value: string) {
  fireEvent.change(screen.getByLabelText(new RegExp(label)), {
    target: { value },
  });
}

describe("UserEditPage", () => {
  test("読み込み中メッセージを表示する", () => {
    setupInitialLoadSuccess();

    render(<UserEditPage />);

    expect(screen.getByText("会員情報を読み込み中...")).toBeInTheDocument();
  });

  test("GET成功時、会員情報編集フォームに初期値を表示する", async () => {
    setupInitialLoadSuccess();

    render(<UserEditPage />);

    await waitForLoaded();

    expect(screen.getByText("EDIT PROFILE")).toBeInTheDocument();

    expect(
      screen.getByText("登録されている会員情報を変更できます。")
    ).toBeInTheDocument();

    expect(screen.getByLabelText(/氏名/)).toHaveValue("一般 太郎");
    expect(screen.getByLabelText(/フリガナ/)).toHaveValue("イッパン タロウ");
    expect(screen.getByLabelText(/郵便番号/)).toHaveValue("123-4567");
    expect(screen.getByLabelText(/住所/)).toHaveValue(
      "東京都新宿区サンプル1-2-3"
    );
    expect(screen.getByLabelText(/電話番号/)).toHaveValue("09012345678");
    expect(screen.getByLabelText(/メールアドレス/)).toHaveValue(
      "user@example.com"
    );

    expect(screen.getAllByText("必須").length).toBe(6);
  });

  test("パンくずと戻るリンクを表示する", async () => {
    setupInitialLoadSuccess();

    render(<UserEditPage />);

    await waitForLoaded();

    expect(screen.getByRole("link", { name: "ホーム" })).toHaveAttribute(
      "href",
      "/"
    );

    expect(screen.getByRole("link", { name: "会員情報" })).toHaveAttribute(
      "href",
      "/user"
    );

    expect(screen.getByRole("link", { name: "戻る" })).toHaveAttribute(
      "href",
      "/user"
    );
  });

  test("氏名未入力で送信するとエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    setupInitialLoadSuccess();

    render(<UserEditPage />);

    await waitForLoaded();

    changeInput("氏名", "");

    await user.click(screen.getByRole("button", { name: "更新" }));

    expect(screen.getByText("氏名を入力してください。")).toBeInTheDocument();
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("フリガナ未入力で送信するとエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    setupInitialLoadSuccess();

    render(<UserEditPage />);

    await waitForLoaded();

    changeInput("フリガナ", "");

    await user.click(screen.getByRole("button", { name: "更新" }));

    expect(screen.getByText("フリガナを入力してください。")).toBeInTheDocument();
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("郵便番号未入力で送信するとエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    setupInitialLoadSuccess();

    render(<UserEditPage />);

    await waitForLoaded();

    changeInput("郵便番号", "");

    await user.click(screen.getByRole("button", { name: "更新" }));

    expect(screen.getByText("郵便番号を入力してください。")).toBeInTheDocument();
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("住所未入力で送信するとエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    setupInitialLoadSuccess();

    render(<UserEditPage />);

    await waitForLoaded();

    changeInput("住所", "");

    await user.click(screen.getByRole("button", { name: "更新" }));

    expect(screen.getByText("住所を入力してください。")).toBeInTheDocument();
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("電話番号未入力で送信するとエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    setupInitialLoadSuccess();

    render(<UserEditPage />);

    await waitForLoaded();

    changeInput("電話番号", "");

    await user.click(screen.getByRole("button", { name: "更新" }));

    expect(screen.getByText("電話番号を入力してください。")).toBeInTheDocument();
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("メールアドレス未入力で送信するとエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    setupInitialLoadSuccess();

    render(<UserEditPage />);

    await waitForLoaded();

    changeInput("メールアドレス", "");

    await user.click(screen.getByRole("button", { name: "更新" }));

    expect(
      screen.getByText("メールアドレスを入力してください。")
    ).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

test("メールアドレス形式不正で送信するとエラーメッセージを表示する", async () => {
  const user = userEvent.setup();

  setupInitialLoadSuccess();

  render(<UserEditPage />);

  await waitForLoaded();

  changeInput("メールアドレス", "invalid-email");

  const form = screen.getByRole("button", { name: "更新" }).closest("form");
  expect(form).not.toBeNull();

  form?.setAttribute("novalidate", "true");

  await user.click(screen.getByRole("button", { name: "更新" }));

  expect(
    screen.getByText("メールアドレスの形式を確認してください。")
  ).toBeInTheDocument();

  expect(mockFetch).toHaveBeenCalledTimes(1);
});

  test("正常入力でPUTを呼び、成功メッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockUserResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
      });

    render(<UserEditPage />);

    await waitForLoaded();

    changeInput("氏名", "更新 太郎");
    changeInput("フリガナ", "コウシン タロウ");
    changeInput("郵便番号", "111-2222");
    changeInput("住所", "東京都千代田区更新1-2-3");
    changeInput("電話番号", "08011112222");
    changeInput("メールアドレス", "updated@example.com");

    await user.click(screen.getByRole("button", { name: "更新" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch.mock.calls[1][0]).toBe(
      "https://localhost:7226/api/auth/me"
    );

    expect(mockFetch.mock.calls[1][1]).toEqual(
      expect.objectContaining({
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },        body: JSON.stringify({
          name: "更新 太郎",
          kana: "コウシン タロウ",
          postalCode: "111-2222",
          address: "東京都千代田区更新1-2-3",
          phoneNumber: "08011112222",
          email: "updated@example.com",
        }),
      })
    );

    await waitFor(() => {
      expect(screen.getByText("会員情報を更新しました。")).toBeInTheDocument();
    });
  });

  test("PUT失敗時、APIメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockUserResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({
          message: "このメールアドレスは使用できません。",
        }),
      });

    render(<UserEditPage />);

    await waitForLoaded();

    await user.click(screen.getByRole("button", { name: "更新" }));

    await waitFor(() => {
      expect(
        screen.getByText("このメールアドレスは使用できません。")
      ).toBeInTheDocument();
    });
  });

  test("PUT失敗時、message がない場合は既定エラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockUserResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: async () => ({}),
      });

    render(<UserEditPage />);

    await waitForLoaded();

    await user.click(screen.getByRole("button", { name: "更新" }));

    await waitFor(() => {
      expect(
        screen.getByText("会員情報の更新に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("PUT失敗時、JSON解析できない場合も既定エラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockUserResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: async () => {
          throw new Error("Invalid JSON");
        },
      });

    render(<UserEditPage />);

    await waitForLoaded();

    await user.click(screen.getByRole("button", { name: "更新" }));

    await waitFor(() => {
      expect(
        screen.getByText("会員情報の更新に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("GET失敗時、エラーメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<UserEditPage />);

    await waitFor(() => {
      expect(
        screen.getByText("会員情報の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("GET通信エラー時、エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<UserEditPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("PUT通信エラー時、エラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockUserResponse(),
      })
      .mockRejectedValueOnce(new Error("Network Error"));

    render(<UserEditPage />);

    await waitForLoaded();

    await user.click(screen.getByRole("button", { name: "更新" }));

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("GET API呼び出し内容が正しい", async () => {
    setupInitialLoadSuccess();

    render(<UserEditPage />);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/auth/me",
      expect.objectContaining({
        method: "GET",        cache: "no-store",
      })
    );
  });
});