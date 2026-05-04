import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import SignupPage from "./page";

const mockFetch = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

async function fillValidForm() {
  const user = userEvent.setup();

  await user.type(screen.getByLabelText("氏名"), "サンプル 太郎");
  await user.type(screen.getByLabelText("フリガナ"), "サンプル タロウ");
  await user.type(screen.getByLabelText("メールアドレス"), "sample@example.com");
  await user.type(screen.getByLabelText("郵便番号"), "123-4567");
  await user.type(screen.getByLabelText("住所"), "東京都新宿区サンプル1-2-3");
  await user.type(screen.getByLabelText("電話番号"), "09012345678");
  await user.selectOptions(screen.getByLabelText("利用区分"), "business");
  await user.type(screen.getByLabelText("パスワード"), "password123");
  await user.type(
    screen.getByLabelText("パスワード（確認用）"),
    "password123"
  );

  return user;
}

describe("SignupPage", () => {
  test("初期表示を行う", () => {
    render(<SignupPage />);

    expect(screen.getByText("SIGN UP")).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: /Studio Book の会員登録で/ })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "会員登録" })
    ).toBeInTheDocument();

    expect(
      screen.getByText("必要事項を入力してアカウントを作成してください。")
    ).toBeInTheDocument();

    expect(screen.getByText("公開デモについて")).toBeInTheDocument();

    expect(
      screen.getByText(
        "この画面はポートフォリオ用のデモ環境です。実在する個人情報・メールアドレス・電話番号は入力しないでください。"
      )
    ).toBeInTheDocument();

    expect(screen.getByLabelText("氏名")).toBeInTheDocument();
    expect(screen.getByLabelText("フリガナ")).toBeInTheDocument();
    expect(screen.getByLabelText("メールアドレス")).toBeInTheDocument();
    expect(screen.getByLabelText("郵便番号")).toBeInTheDocument();
    expect(screen.getByLabelText("住所")).toBeInTheDocument();
    expect(screen.getByLabelText("電話番号")).toBeInTheDocument();
    expect(screen.getByLabelText("利用区分")).toBeInTheDocument();
    expect(screen.getByLabelText("パスワード")).toBeInTheDocument();
    expect(screen.getByLabelText("パスワード（確認用）")).toBeInTheDocument();

    expect(screen.getByRole("button", { name: "会員登録" })).toBeInTheDocument();

    expect(screen.getByText("すでにアカウントをお持ちの方")).toBeInTheDocument();

    expect(
      screen.getByRole("link", { name: "ログインはこちら" })
    ).toHaveAttribute("href", "/auth/login");
  });

  test("利用区分の初期値は個人利用", () => {
    render(<SignupPage />);

    expect(screen.getByLabelText("利用区分")).toHaveValue("personal");
  });

  test("氏名未入力で送信するとエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    render(<SignupPage />);

    await user.click(screen.getByRole("button", { name: "会員登録" }));

    expect(screen.getByText("氏名を入力してください。")).toBeInTheDocument();
    expect(mockFetch).not.toHaveBeenCalled();
  });

  test("フリガナ未入力で送信するとエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    render(<SignupPage />);

    await user.type(screen.getByLabelText("氏名"), "サンプル 太郎");

    await user.click(screen.getByRole("button", { name: "会員登録" }));

    expect(screen.getByText("フリガナを入力してください。")).toBeInTheDocument();
    expect(mockFetch).not.toHaveBeenCalled();
  });

  test("メールアドレス未入力で送信するとエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    render(<SignupPage />);

    await user.type(screen.getByLabelText("氏名"), "サンプル 太郎");
    await user.type(screen.getByLabelText("フリガナ"), "サンプル タロウ");

    await user.click(screen.getByRole("button", { name: "会員登録" }));

    expect(
      screen.getByText("メールアドレスを入力してください。")
    ).toBeInTheDocument();

    expect(mockFetch).not.toHaveBeenCalled();
  });

  test("郵便番号未入力で送信するとエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    render(<SignupPage />);

    await user.type(screen.getByLabelText("氏名"), "サンプル 太郎");
    await user.type(screen.getByLabelText("フリガナ"), "サンプル タロウ");
    await user.type(screen.getByLabelText("メールアドレス"), "sample@example.com");

    await user.click(screen.getByRole("button", { name: "会員登録" }));

    expect(screen.getByText("郵便番号を入力してください。")).toBeInTheDocument();
    expect(mockFetch).not.toHaveBeenCalled();
  });

  test("住所未入力で送信するとエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    render(<SignupPage />);

    await user.type(screen.getByLabelText("氏名"), "サンプル 太郎");
    await user.type(screen.getByLabelText("フリガナ"), "サンプル タロウ");
    await user.type(screen.getByLabelText("メールアドレス"), "sample@example.com");
    await user.type(screen.getByLabelText("郵便番号"), "123-4567");

    await user.click(screen.getByRole("button", { name: "会員登録" }));

    expect(screen.getByText("住所を入力してください。")).toBeInTheDocument();
    expect(mockFetch).not.toHaveBeenCalled();
  });

  test("電話番号未入力で送信するとエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    render(<SignupPage />);

    await user.type(screen.getByLabelText("氏名"), "サンプル 太郎");
    await user.type(screen.getByLabelText("フリガナ"), "サンプル タロウ");
    await user.type(screen.getByLabelText("メールアドレス"), "sample@example.com");
    await user.type(screen.getByLabelText("郵便番号"), "123-4567");
    await user.type(screen.getByLabelText("住所"), "東京都新宿区サンプル1-2-3");

    await user.click(screen.getByRole("button", { name: "会員登録" }));

    expect(screen.getByText("電話番号を入力してください。")).toBeInTheDocument();
    expect(mockFetch).not.toHaveBeenCalled();
  });

  test("パスワード未入力で送信するとエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    render(<SignupPage />);

    await user.type(screen.getByLabelText("氏名"), "サンプル 太郎");
    await user.type(screen.getByLabelText("フリガナ"), "サンプル タロウ");
    await user.type(screen.getByLabelText("メールアドレス"), "sample@example.com");
    await user.type(screen.getByLabelText("郵便番号"), "123-4567");
    await user.type(screen.getByLabelText("住所"), "東京都新宿区サンプル1-2-3");
    await user.type(screen.getByLabelText("電話番号"), "09012345678");

    await user.click(screen.getByRole("button", { name: "会員登録" }));

    expect(
      screen.getByText("パスワードを入力してください。")
    ).toBeInTheDocument();

    expect(mockFetch).not.toHaveBeenCalled();
  });

  test("パスワードが8文字未満の場合、エラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    render(<SignupPage />);

    await user.type(screen.getByLabelText("氏名"), "サンプル 太郎");
    await user.type(screen.getByLabelText("フリガナ"), "サンプル タロウ");
    await user.type(screen.getByLabelText("メールアドレス"), "sample@example.com");
    await user.type(screen.getByLabelText("郵便番号"), "123-4567");
    await user.type(screen.getByLabelText("住所"), "東京都新宿区サンプル1-2-3");
    await user.type(screen.getByLabelText("電話番号"), "09012345678");
    await user.type(screen.getByLabelText("パスワード"), "short");

    await user.click(screen.getByRole("button", { name: "会員登録" }));

    expect(
      screen.getByText("パスワードは8文字以上で入力してください。")
    ).toBeInTheDocument();

    expect(mockFetch).not.toHaveBeenCalled();
  });

  test("確認用パスワード未入力で送信するとエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    render(<SignupPage />);

    await user.type(screen.getByLabelText("氏名"), "サンプル 太郎");
    await user.type(screen.getByLabelText("フリガナ"), "サンプル タロウ");
    await user.type(screen.getByLabelText("メールアドレス"), "sample@example.com");
    await user.type(screen.getByLabelText("郵便番号"), "123-4567");
    await user.type(screen.getByLabelText("住所"), "東京都新宿区サンプル1-2-3");
    await user.type(screen.getByLabelText("電話番号"), "09012345678");
    await user.type(screen.getByLabelText("パスワード"), "password123");

    await user.click(screen.getByRole("button", { name: "会員登録" }));

    expect(
      screen.getByText("確認用パスワードを入力してください。")
    ).toBeInTheDocument();

    expect(mockFetch).not.toHaveBeenCalled();
  });

  test("パスワード不一致の場合、エラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    render(<SignupPage />);

    await user.type(screen.getByLabelText("氏名"), "サンプル 太郎");
    await user.type(screen.getByLabelText("フリガナ"), "サンプル タロウ");
    await user.type(screen.getByLabelText("メールアドレス"), "sample@example.com");
    await user.type(screen.getByLabelText("郵便番号"), "123-4567");
    await user.type(screen.getByLabelText("住所"), "東京都新宿区サンプル1-2-3");
    await user.type(screen.getByLabelText("電話番号"), "09012345678");
    await user.type(screen.getByLabelText("パスワード"), "password123");
    await user.type(
      screen.getByLabelText("パスワード（確認用）"),
      "password456"
    );

    await user.click(screen.getByRole("button", { name: "会員登録" }));

    expect(screen.getByText("パスワードが一致しません。")).toBeInTheDocument();
    expect(mockFetch).not.toHaveBeenCalled();
  });

  test("パスワード表示ボタンでtypeを切り替える", async () => {
    const user = userEvent.setup();

    render(<SignupPage />);

    const passwordInput = screen.getByLabelText("パスワード");
    const confirmationInput = screen.getByLabelText("パスワード（確認用）");

    expect(passwordInput).toHaveAttribute("type", "password");
    expect(confirmationInput).toHaveAttribute("type", "password");

    const showButtons = screen.getAllByRole("button", { name: "表示" });

    await user.click(showButtons[0]);
    expect(passwordInput).toHaveAttribute("type", "text");

    await user.click(showButtons[1]);
    expect(confirmationInput).toHaveAttribute("type", "text");

    expect(screen.getAllByRole("button", { name: "非表示" }).length)
      .toBeGreaterThanOrEqual(2);
  });

  test("正常入力で会員登録APIを呼び、成功メッセージを表示してフォームをクリアする", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        success: true,
        message:
          "認証メールを送信しました。メール内のリンクから登録を完了してください。",
      }),
    });

    render(<SignupPage />);

    const user = await fillValidForm();

    await user.click(screen.getByRole("button", { name: "会員登録" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/auth/signup",
      expect.objectContaining({
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        credentials: "include",
        body: JSON.stringify({
          name: "サンプル 太郎",
          kana: "サンプル タロウ",
          email: "sample@example.com",
          postalCode: "123-4567",
          address: "東京都新宿区サンプル1-2-3",
          phoneNumber: "09012345678",
          usageType: "business",
          password: "password123",
          passwordConfirmation: "password123",
        }),
      })
    );

    await waitFor(() => {
      expect(
        screen.getByText(
          "認証メールを送信しました。メール内のリンクから登録を完了してください。"
        )
      ).toBeInTheDocument();
    });

    expect(screen.getByLabelText("氏名")).toHaveValue("");
    expect(screen.getByLabelText("フリガナ")).toHaveValue("");
    expect(screen.getByLabelText("メールアドレス")).toHaveValue("");
    expect(screen.getByLabelText("郵便番号")).toHaveValue("");
    expect(screen.getByLabelText("住所")).toHaveValue("");
    expect(screen.getByLabelText("電話番号")).toHaveValue("");
    expect(screen.getByLabelText("利用区分")).toHaveValue("personal");
    expect(screen.getByLabelText("パスワード")).toHaveValue("");
    expect(screen.getByLabelText("パスワード（確認用）")).toHaveValue("");
  });

  test("成功レスポンスにmessageがない場合、既定成功メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        success: true,
      }),
    });

    render(<SignupPage />);

    const user = await fillValidForm();

    await user.click(screen.getByRole("button", { name: "会員登録" }));

    await waitFor(() => {
      expect(
        screen.getByText(
          "認証メールを送信しました。メール内のリンクから登録を完了してください。"
        )
      ).toBeInTheDocument();
    });
  });

  test("API失敗時、APIから返されたメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 400,
      json: async () => ({
        message: "このメールアドレスは既に登録されています。",
      }),
    });

    render(<SignupPage />);

    const user = await fillValidForm();

    await user.click(screen.getByRole("button", { name: "会員登録" }));

    await waitFor(() => {
      expect(
        screen.getByText("このメールアドレスは既に登録されています。")
      ).toBeInTheDocument();
    });
  });

  test("API失敗時、messageがない場合は既定エラーメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
      json: async () => ({}),
    });

    render(<SignupPage />);

    const user = await fillValidForm();

    await user.click(screen.getByRole("button", { name: "会員登録" }));

    await waitFor(() => {
      expect(
        screen.getByText("会員登録に失敗しました。入力内容をご確認ください。")
      ).toBeInTheDocument();
    });
  });

  test("API失敗時、JSON解析できない場合も既定エラーメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
      json: async () => {
        throw new Error("Invalid JSON");
      },
    });

    render(<SignupPage />);

    const user = await fillValidForm();

    await user.click(screen.getByRole("button", { name: "会員登録" }));

    await waitFor(() => {
      expect(
        screen.getByText("会員登録に失敗しました。入力内容をご確認ください。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    const consoleErrorSpy = jest
      .spyOn(console, "error")
      .mockImplementation(() => {});

    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<SignupPage />);

    const user = await fillValidForm();

    await user.click(screen.getByRole("button", { name: "会員登録" }));

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });

    consoleErrorSpy.mockRestore();
  });

  test("送信中はボタン文言が登録中になる", async () => {
    const user = userEvent.setup();

    mockFetch.mockImplementation(
      () =>
        new Promise((resolve) => {
          setTimeout(() => {
            resolve({
              ok: true,
              status: 200,
              json: async () => ({
                success: true,
              }),
            });
          }, 100);
        })
    );

    render(<SignupPage />);

    await user.type(screen.getByLabelText("氏名"), "サンプル 太郎");
    await user.type(screen.getByLabelText("フリガナ"), "サンプル タロウ");
    await user.type(screen.getByLabelText("メールアドレス"), "sample@example.com");
    await user.type(screen.getByLabelText("郵便番号"), "123-4567");
    await user.type(screen.getByLabelText("住所"), "東京都新宿区サンプル1-2-3");
    await user.type(screen.getByLabelText("電話番号"), "09012345678");
    await user.type(screen.getByLabelText("パスワード"), "password123");
    await user.type(
      screen.getByLabelText("パスワード（確認用）"),
      "password123"
    );

    await user.click(screen.getByRole("button", { name: "会員登録" }));

    expect(screen.getByRole("button", { name: "登録中..." })).toBeDisabled();
  });
});