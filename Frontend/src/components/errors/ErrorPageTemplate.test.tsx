import { render, screen } from "@testing-library/react";
import ErrorPageTemplate from "@/components/errors/ErrorPageTemplate";

describe("ErrorPageTemplate", () => {
  test("エラー情報を表示する", () => {
    render(
      <ErrorPageTemplate
        code="404"
        title="Not Found"
        description="ページが見つかりません。"
        path="/rooms/999"
        error="NotFound"
        message="対象データが存在しません。"
        timestamp="2026-05-04T10:00:00"
        primaryAction={{ href: "/", label: "トップへ戻る" }}
      />
    );

    expect(screen.getByRole("heading", { name: "404 Not Found" })).toBeInTheDocument();
    expect(screen.getByText("ページが見つかりません。")).toBeInTheDocument();
    expect(screen.getByText(/\/rooms\/999/)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "トップへ戻る" })).toHaveAttribute(
      "href",
      "/"
    );
  });
});