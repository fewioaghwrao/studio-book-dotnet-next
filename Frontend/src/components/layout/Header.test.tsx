import { render, screen } from "@testing-library/react";
import Header from "./Header";

jest.mock("./AuthNav", () => {
  return function MockAuthNav() {
    return <nav data-testid="auth-nav">AuthNav Mock</nav>;
  };
});

describe("Header", () => {
  test("ロゴ画像と認証ナビゲーションを表示する", () => {
    render(<Header />);

    const logo = screen.getByAltText("Studio Book ロゴ");

    expect(logo).toBeInTheDocument();
    expect(logo).toHaveAttribute("src", "/images/logo.png");

    expect(screen.getByTestId("auth-nav")).toBeInTheDocument();
    expect(screen.getByText("AuthNav Mock")).toBeInTheDocument();
  });

  test("ロゴリンクの遷移先がトップページである", () => {
    render(<Header />);

    const logo = screen.getByAltText("Studio Book ロゴ");
    const link = logo.closest("a");

    expect(link).toHaveAttribute("href", "/");
  });
});