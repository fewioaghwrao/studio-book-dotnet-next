import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import LoginPage from "@/app/auth/login/page";

describe("LoginPage", () => {
  test("メールアドレス未入力の場合、エラーを表示する", async () => {
    const user = userEvent.setup();

    render(<LoginPage />);

    await user.click(screen.getByRole("button", { name: "ログイン" }));

    expect(
      screen.getByText("メールアドレスを入力してください。")
    ).toBeInTheDocument();
  });

  test("パスワード未入力の場合、エラーを表示する", async () => {
    const user = userEvent.setup();

    render(<LoginPage />);

    await user.type(
      screen.getByLabelText("メールアドレス"),
      "test@example.com"
    );

    await user.click(screen.getByRole("button", { name: "ログイン" }));

    expect(
      screen.getByText("パスワードを入力してください。")
    ).toBeInTheDocument();
  });
});