import { render, screen } from "@testing-library/react";
import RootLayout, { metadata } from "./layout";

jest.mock("next/font/google", () => ({
  Geist: () => ({
    variable: "mock-geist-sans",
  }),
  Geist_Mono: () => ({
    variable: "mock-geist-mono",
  }),
}));

jest.mock("@/components/layout/Header", () => {
  return function MockHeader() {
    return <header data-testid="header">Header Mock</header>;
  };
});

jest.mock("@/components/layout/Footer", () => {
  return function MockFooter() {
    return <footer data-testid="footer">Footer Mock</footer>;
  };
});

describe("RootLayout", () => {
  test("metadata が設定されている", () => {
    expect(metadata.title).toBe("Studio Book - スタジオ予約サービス");
    expect(metadata.description).toBe(
      "Studio Book is a platform for booking studio sessions."
    );
  });

  test("Header、children、Footer を表示する", () => {
    render(
      <RootLayout>
        <div>テスト用コンテンツ</div>
      </RootLayout>
    );

    expect(screen.getByTestId("header")).toBeInTheDocument();
    expect(screen.getByText("テスト用コンテンツ")).toBeInTheDocument();
    expect(screen.getByTestId("footer")).toBeInTheDocument();
  });

  test("html と body に基本属性・クラスが設定されている", () => {
    render(
      <RootLayout>
        <div>テスト用コンテンツ</div>
      </RootLayout>
    );

    const html = document.documentElement;
    const body = document.body;

    expect(html).toHaveAttribute("lang", "ja");
    expect(html.className).toContain("mock-geist-sans");
    expect(html.className).toContain("mock-geist-mono");
    expect(html.className).toContain("h-full");
    expect(html.className).toContain("antialiased");

    expect(body.className).toContain("min-h-full");
    expect(body.className).toContain("bg-stone-50");
    expect(body.className).toContain("text-slate-800");
    expect(body.className).toContain("flex");
    expect(body.className).toContain("flex-col");
  });
});