import { render, screen } from "@testing-library/react";
import NotFoundPage from "./not-found";

jest.mock("@/components/errors/ErrorPageTemplate", () => {
  return function MockErrorPageTemplate({
    code,
    title,
    description,
    primaryAction,
  }: {
    code: string;
    title: string;
    description: string;
    primaryAction?: {
      href: string;
      label: string;
    };
  }) {
    return (
      <div>
        <div data-testid="code">{code}</div>
        <div data-testid="title">{title}</div>
        <div data-testid="description">{description}</div>
        {primaryAction && (
          <a href={primaryAction.href}>{primaryAction.label}</a>
        )}
      </div>
    );
  };
});

describe("NotFoundPage", () => {
  test("404用のエラーページを表示する", () => {
    render(<NotFoundPage />);

    expect(screen.getByTestId("code")).toHaveTextContent("404");
    expect(screen.getByTestId("title")).toHaveTextContent("Not Found");
    expect(screen.getByTestId("description")).toHaveTextContent(
      "お探しのページは移動または削除された可能性があります。"
    );

    expect(screen.getByRole("link", { name: "ホームに戻る" })).toHaveAttribute(
      "href",
      "/"
    );
  });
});