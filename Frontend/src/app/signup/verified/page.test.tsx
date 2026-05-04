import { render, screen } from "@testing-library/react";
import SignupVerifiedPage from "./page";

describe("SignupVerifiedPage", () => {
  test("メール認証完了メッセージを表示する", () => {
    render(<SignupVerifiedPage />);

    expect(
      screen.getByRole("heading", { name: "メール認証が完了しました" })
    ).toBeInTheDocument();

    expect(
      screen.getByText(/アカウントの有効化が完了しました。/)
    ).toBeInTheDocument();

    expect(
      screen.getByText(/ログイン画面からサインインしてください。/)
    ).toBeInTheDocument();
  });

  test("ログイン画面へのリンクを表示する", () => {
    render(<SignupVerifiedPage />);

    expect(
      screen.getByRole("link", { name: "ログイン画面へ" })
    ).toHaveAttribute("href", "/login");
  });

  test("トップへ戻るリンクを表示する", () => {
    render(<SignupVerifiedPage />);

    expect(
      screen.getByRole("link", { name: "トップへ戻る" })
    ).toHaveAttribute("href", "/");
  });
});