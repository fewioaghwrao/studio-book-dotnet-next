import { render, screen } from "@testing-library/react";
import SignupVerifyErrorPage from "./page";

describe("SignupVerifyErrorPage", () => {
  test("メール認証失敗メッセージを表示する", () => {
    render(<SignupVerifyErrorPage />);

    expect(
      screen.getByRole("heading", { name: "メール認証に失敗しました" })
    ).toBeInTheDocument();

    expect(
      screen.getByText(/認証URLが無効、または有効期限切れの可能性があります。/)
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        /もう一度会員登録を行うか、認証メールの再送機能をご利用ください。/
      )
    ).toBeInTheDocument();
  });

  test("会員登録へのリンクを表示する", () => {
    render(<SignupVerifyErrorPage />);

    expect(
      screen.getByRole("link", { name: "会員登録へ" })
    ).toHaveAttribute("href", "/signup");
  });

  test("トップへ戻るリンクを表示する", () => {
    render(<SignupVerifyErrorPage />);

    expect(
      screen.getByRole("link", { name: "トップへ戻る" })
    ).toHaveAttribute("href", "/");
  });
});