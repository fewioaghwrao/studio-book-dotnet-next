import { render, screen } from "@testing-library/react";
import UnauthorizedPage from "./page";

jest.mock("next/navigation", () => ({
  useSearchParams: () => new URLSearchParams(),
}));

describe("UnauthorizedPage", () => {
  test("ログインへのリンクはログイン画面（/auth/login）を指す", () => {
    render(<UnauthorizedPage />);

    expect(screen.getByRole("link", { name: "ログインへ" })).toHaveAttribute(
      "href",
      "/auth/login"
    );
    expect(screen.getByRole("link", { name: "ホームに戻る" })).toHaveAttribute(
      "href",
      "/"
    );
  });
});
