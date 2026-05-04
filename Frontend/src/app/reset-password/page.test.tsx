import { render, screen, waitFor, act } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import ResetPasswordPage from "./page";

const mockFetch = jest.fn();
const mockPush = jest.fn();

let mockSearchParams = new URLSearchParams("token=test-token");

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

  mockSearchParams = new URLSearchParams("token=test-token");
  global.fetch = mockFetch as jest.Mock;

  jest.useFakeTimers();
});

afterEach(() => {
  jest.useRealTimers();
});

describe("ResetPasswordPage", () => {
  test("初期表示を行う", () => {
    render(<ResetPasswordPage />);

    expect(
      screen.getByRole("heading", { name: "新しいパスワード設定" })
    ).toBeInTheDocument();

    expect(
      screen.getByText("新しいパスワードを入力してください。")
    ).toBeInTheDocument();

    expect(screen.getByLabelText("新しいパスワード")).toBeInTheDocument();

    expect(
      screen.getByLabelText("新しいパスワード（確認）")
    ).toBeInTheDocument();

    expect(
      screen.getByRole("button", { name: "パスワードを更新" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("link", { name: "ログイン画面へ戻る" })
    ).toHaveAttribute("href", "/auth/login");
  });

  test("token がない場合、トークン未指定メッセージを表示しフォームを表示しない", () => {
    mockSearchParams = new URLSearchParams();

    render(<ResetPasswordPage />);

    expect(
      screen.getByText("再設定用トークンが見つかりません。")
    ).toBeInTheDocument();

    expect(
      screen.queryByRole("button", { name: "パスワードを更新" })
    ).not.toBeInTheDocument();
  });

  test("未入力で送信するとエラーメッセージを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    render(<ResetPasswordPage />);

    await user.click(screen.getByRole("button", { name: "パスワードを更新" }));

    expect(screen.getByText("入力内容を確認してください。")).toBeInTheDocument();
    expect(mockFetch).not.toHaveBeenCalled();
  });

  test("8文字未満の場合、エラーメッセージを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    render(<ResetPasswordPage />);

    await user.type(screen.getByLabelText("新しいパスワード"), "short");
    await user.type(screen.getByLabelText("新しいパスワード（確認）"), "short");

    await user.click(screen.getByRole("button", { name: "パスワードを更新" }));

    expect(
      screen.getByText("パスワードは8文字以上で入力してください。")
    ).toBeInTheDocument();

    expect(mockFetch).not.toHaveBeenCalled();
  });

  test("確認用パスワードと一致しない場合、エラーメッセージを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    render(<ResetPasswordPage />);

    await user.type(screen.getByLabelText("新しいパスワード"), "password123");
    await user.type(
      screen.getByLabelText("新しいパスワード（確認）"),
      "password456"
    );

    await user.click(screen.getByRole("button", { name: "パスワードを更新" }));

    expect(screen.getByText("パスワードが一致しません。")).toBeInTheDocument();
    expect(mockFetch).not.toHaveBeenCalled();
  });

  test("表示ボタンでパスワード入力欄の type を切り替える", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    render(<ResetPasswordPage />);

    const newPasswordInput = screen.getByLabelText("新しいパスワード");
    const confirmPasswordInput = screen.getByLabelText("新しいパスワード（確認）");

    expect(newPasswordInput).toHaveAttribute("type", "password");
    expect(confirmPasswordInput).toHaveAttribute("type", "password");

    const showButtons = screen.getAllByRole("button", { name: "表示" });

    await user.click(showButtons[0]);
    expect(newPasswordInput).toHaveAttribute("type", "text");

    await user.click(showButtons[1]);
    expect(confirmPasswordInput).toHaveAttribute("type", "text");

    expect(screen.getAllByRole("button", { name: "非表示" }).length)
      .toBeGreaterThanOrEqual(2);
  });

test("正常入力でPOSTし、成功メッセージ表示後ログイン画面へ遷移する", async () => {
  const user = userEvent.setup({
    advanceTimers: jest.advanceTimersByTime,
  });

  mockFetch.mockResolvedValueOnce({
    ok: true,
    status: 200,
    json: async () => ({
      success: true,
      message: "パスワードを再設定しました。",
    }),
  });

  render(<ResetPasswordPage />);

  await user.type(screen.getByLabelText("新しいパスワード"), "password123");
  await user.type(
    screen.getByLabelText("新しいパスワード（確認）"),
    "password123"
  );

  await user.click(screen.getByRole("button", { name: "パスワードを更新" }));

  await waitFor(() => {
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  expect(mockFetch).toHaveBeenCalledWith(
    "http://localhost:8080/api/auth/reset-password",
    expect.objectContaining({
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        token: "test-token",
        newPassword: "password123",
        confirmPassword: "password123",
      }),
    })
  );

  await waitFor(() => {
    expect(
      screen.getByText("パスワードを再設定しました。")
    ).toBeInTheDocument();
  });

  expect(screen.getByText("ログイン画面へ移動します...")).toBeInTheDocument();

  await act(async () => {
    jest.advanceTimersByTime(1500);
  });

  expect(mockPush).toHaveBeenCalledWith("/auth/login");
});
  test("API失敗時、APIから返されたエラーメッセージを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 400,
      json: async () => ({
        message: "トークンの有効期限が切れています。",
      }),
    });

    render(<ResetPasswordPage />);

    await user.type(screen.getByLabelText("新しいパスワード"), "password123");
    await user.type(
      screen.getByLabelText("新しいパスワード（確認）"),
      "password123"
    );

    await user.click(screen.getByRole("button", { name: "パスワードを更新" }));

    await waitFor(() => {
      expect(
        screen.getByText("トークンの有効期限が切れています。")
      ).toBeInTheDocument();
    });

    expect(mockPush).not.toHaveBeenCalled();
  });

  test("API失敗時、message がない場合は既定メッセージを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
      json: async () => ({}),
    });

    render(<ResetPasswordPage />);

    await user.type(screen.getByLabelText("新しいパスワード"), "password123");
    await user.type(
      screen.getByLabelText("新しいパスワード（確認）"),
      "password123"
    );

    await user.click(screen.getByRole("button", { name: "パスワードを更新" }));

    await waitFor(() => {
      expect(
        screen.getByText("パスワード再設定に失敗しました。")
      ).toBeInTheDocument();
    });

    expect(mockPush).not.toHaveBeenCalled();
  });

  test("通信エラー時、エラーメッセージを表示する", async () => {
    const user = userEvent.setup({
      advanceTimers: jest.advanceTimersByTime,
    });

    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<ResetPasswordPage />);

    await user.type(screen.getByLabelText("新しいパスワード"), "password123");
    await user.type(
      screen.getByLabelText("新しいパスワード（確認）"),
      "password123"
    );

    await user.click(screen.getByRole("button", { name: "パスワードを更新" }));

    await waitFor(() => {
      expect(
        screen.getByText("通信に失敗しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });

    expect(mockPush).not.toHaveBeenCalled();
  });
});