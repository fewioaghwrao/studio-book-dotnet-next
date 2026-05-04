import { render, screen } from "@testing-library/react";
import Footer from "@/components/layout/Footer";

describe("Footer", () => {
  test("フッターリンクが表示される", () => {
    render(<Footer />);

    expect(screen.getByRole("link", { name: "利用規約" })).toHaveAttribute(
      "href",
      "/terms"
    );
    expect(
      screen.getByRole("link", { name: "プライバシーポリシー" })
    ).toHaveAttribute("href", "/privacy");
    expect(
      screen.getByRole("link", { name: "特定商取引法に基づく表記" })
    ).toHaveAttribute("href", "/legal/commerce");
  });

  test("コピーライトが表示される", () => {
    render(<Footer />);

    expect(screen.getByText(/N\.O\. All rights reserved\./)).toBeInTheDocument();
  });
});