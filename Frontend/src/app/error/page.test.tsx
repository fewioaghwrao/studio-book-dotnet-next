import { render, screen } from "@testing-library/react";
import GenericErrorPage from "./page";

let mockSearchParams = new URLSearchParams();

jest.mock("next/navigation", () => ({
  useSearchParams: () => mockSearchParams,
}));

beforeEach(() => {
  mockSearchParams = new URLSearchParams();
});

describe("GenericErrorPage", () => {
  test("クエリパラメータなしの場合、汎用エラー画面を表示する", () => {
    render(<GenericErrorPage />);

    expect(
      screen.getByRole("heading", { name: "- エラーが発生しました" })
    ).toBeInTheDocument();

    expect(
      screen.getByText("申し訳ありません。ページの表示中に問題が発生しました。")
    ).toBeInTheDocument();

    expect(screen.queryByText(/Status:/)).not.toBeInTheDocument();

    expect(screen.getByRole("link", { name: "ホームに戻る" })).toHaveAttribute(
      "href",
      "/"
    );

    expect(
      screen.getByRole("button", { name: "前のページへ戻る" })
    ).toBeInTheDocument();
  });

  test("クエリパラメータをエラー画面に表示する", () => {
    mockSearchParams = new URLSearchParams({
      status: "500",
      error: "Internal Server Error",
      message: "サーバーエラーが発生しました。",
      path: "/admin/users",
    });

    render(<GenericErrorPage />);

    expect(
      screen.getByRole("heading", { name: "500 エラーが発生しました" })
    ).toBeInTheDocument();

    expect(screen.getByText(/Path:/)).toBeInTheDocument();
    expect(screen.getByText(/\/admin\/users/)).toBeInTheDocument();

    expect(screen.getByText(/Status:/)).toBeInTheDocument();
    expect(screen.getAllByText(/500/).length).toBeGreaterThan(0);

    expect(screen.getByText(/Error:/)).toBeInTheDocument();
    expect(screen.getByText(/Internal Server Error/)).toBeInTheDocument();

    expect(screen.getByText(/Message:/)).toBeInTheDocument();
    expect(screen.getByText(/サーバーエラーが発生しました。/)).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "ホームに戻る" })).toHaveAttribute(
      "href",
      "/"
    );

    expect(
      screen.getByRole("button", { name: "前のページへ戻る" })
    ).toBeInTheDocument();
  });
});