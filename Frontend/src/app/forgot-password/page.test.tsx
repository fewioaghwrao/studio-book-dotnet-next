import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import ForgotPasswordPage from "./page";

const mockFetch = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

describe("ForgotPasswordPage", () => {
test("初期表示を行う", () => {
  render(<ForgotPasswordPage />);

  expect(
    screen.getByRole("heading", { name: "パスワード再設定" })
  ).toBeInTheDocument();

  expect(
    screen.getByText(/登録済みのメールアドレスを入力してください。/)
  ).toBeInTheDocument();

  expect(screen.getByLabelText("メールアドレス")).toBeInTheDocument();

  expect(
    screen.getByRole("button", { name: "再設定メールを送信" })
  ).toBeInTheDocument();

  expect(
    screen.getByRole("link", { name: "ログイン画面へ戻る" })
  ).toHaveAttribute("href", "/auth/login");
});
  test("メール未入力の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    render(<ForgotPasswordPage />);

    await user.click(screen.getByRole("button", { name: "再設定メールを送信" }));

    expect(
      screen.getByText("メールアドレスを入力してください。")
    ).toBeInTheDocument();

    expect(mockFetch).not.toHaveBeenCalled();
  });

  test("API成功時、成功メッセージを表示して入力欄をクリアする", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        success: true,
        message: "再設定メールを送信しました。",
      }),
    });

    render(<ForgotPasswordPage />);

    const emailInput = screen.getByLabelText("メールアドレス") as HTMLInputElement;

    await user.type(emailInput, "user@example.com");
    await user.click(screen.getByRole("button", { name: "再設定メールを送信" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "http://localhost:8080/api/auth/forgot-password",
      expect.objectContaining({
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          email: "user@example.com",
        }),
      })
    );

    expect(screen.getByText("再設定メールを送信しました。")).toBeInTheDocument();
    expect(emailInput).toHaveValue("");
  });

  test("API成功時、message がない場合は既定の成功メッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        success: true,
      }),
    });

    render(<ForgotPasswordPage />);

    await user.type(screen.getByLabelText("メールアドレス"), "user@example.com");
    await user.click(screen.getByRole("button", { name: "再設定メールを送信" }));

    await waitFor(() => {
      expect(
        screen.getByText(
          "登録されているメールアドレスの場合、再設定メールを送信しました。"
        )
      ).toBeInTheDocument();
    });
  });

  test("API失敗時、APIから返されたエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 400,
      json: async () => ({
        code: "INVALID_EMAIL",
        message: "メールアドレスが見つかりません。",
      }),
    });

    render(<ForgotPasswordPage />);

    await user.type(screen.getByLabelText("メールアドレス"), "missing@example.com");
    await user.click(screen.getByRole("button", { name: "再設定メールを送信" }));

    await waitFor(() => {
      expect(
        screen.getByText("メールアドレスが見つかりません。")
      ).toBeInTheDocument();
    });
  });

  test("API失敗時、message がない場合は既定のエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
      json: async () => ({
        code: "SERVER_ERROR",
      }),
    });

    render(<ForgotPasswordPage />);

    await user.type(screen.getByLabelText("メールアドレス"), "user@example.com");
    await user.click(screen.getByRole("button", { name: "再設定メールを送信" }));

    await waitFor(() => {
      expect(screen.getByText("送信に失敗しました。")).toBeInTheDocument();
    });
  });

  test("通信エラー時、エラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<ForgotPasswordPage />);

    await user.type(screen.getByLabelText("メールアドレス"), "user@example.com");
    await user.click(screen.getByRole("button", { name: "再設定メールを送信" }));

    await waitFor(() => {
      expect(
        screen.getByText("通信に失敗しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });
});