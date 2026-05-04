import { render, screen } from "@testing-library/react";
import CommercePage from "./page";

describe("CommercePage", () => {
  test("ページタイトルと最終更新日を表示する", () => {
    render(<CommercePage />);

    expect(
      screen.getByRole("heading", { name: "特定商取引法に基づく表記" })
    ).toBeInTheDocument();

    expect(screen.getByText("最終更新日：2025-10-23")).toBeInTheDocument();
  });

  test("パンくずリンクを表示する", () => {
    render(<CommercePage />);

    expect(screen.getByRole("navigation", { name: "breadcrumb" }))
      .toBeInTheDocument();

    expect(screen.getByRole("link", { name: "ホーム" })).toHaveAttribute(
      "href",
      "/"
    );

    expect(screen.getAllByText("特定商取引法に基づく表記").length)
      .toBeGreaterThan(0);
  });

  test("特定商取引法に基づく表記の項目を表示する", () => {
    render(<CommercePage />);

    expect(screen.getByText("販売事業者")).toBeInTheDocument();
    expect(screen.getByText("運営責任者")).toBeInTheDocument();
    expect(screen.getByText("所在地")).toBeInTheDocument();
    expect(screen.getByText("連絡先")).toBeInTheDocument();
    expect(screen.getByText("販売価格")).toBeInTheDocument();
    expect(screen.getByText("商品代金以外の必要料金")).toBeInTheDocument();
    expect(screen.getByText("支払方法")).toBeInTheDocument();
    expect(screen.getByText("支払時期")).toBeInTheDocument();
    expect(screen.getByText("役務の提供時期")).toBeInTheDocument();
    expect(screen.getByText("キャンセル・変更")).toBeInTheDocument();
    expect(screen.getByText("返品・交換")).toBeInTheDocument();
    expect(screen.getByText("動作環境")).toBeInTheDocument();
    expect(screen.getByText("特記事項")).toBeInTheDocument();
  });

  test("表記内容を表示する", () => {
    render(<CommercePage />);

    expect(screen.getAllByText("N.O.").length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText("東京都〇〇区〇〇1-2-3")).toBeInTheDocument();

    expect(
      screen.getByText("各スタジオ詳細ページに表示（時間単価/プランによる）")
    ).toBeInTheDocument();

    expect(
      screen.getByText("決済手数料（必要な場合）、通信料等")
    ).toBeInTheDocument();

    expect(
      screen.getByText("クレジットカード決済（Stripe）ほか")
    ).toBeInTheDocument();

    expect(screen.getByText("予約確定時に即時決済")).toBeInTheDocument();
    expect(screen.getByText("予約日時に提供")).toBeInTheDocument();

    expect(
      screen.getByText(
        "各スタジオのキャンセルポリシーに従います。返金の有無・条件はポリシーに記載。"
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText("役務の性質上、提供後の返品・交換はできません。")
    ).toBeInTheDocument();

    expect(
      screen.getByText("最新の主要ブラウザ（Chrome/Edge/Safari/Firefox）")
    ).toBeInTheDocument();

    expect(
      screen.getByText("未成年者の利用には保護者の同意が必要です。")
    ).toBeInTheDocument();
  });

  test("メールリンクを表示する", () => {
    render(<CommercePage />);

    expect(
      screen.getByRole("link", { name: "info@example.com" })
    ).toHaveAttribute("href", "mailto:info@example.com");
  });

  test("架空表記の注記を表示する", () => {
    render(<CommercePage />);

    expect(screen.getByText("※表記内容は架空のものです。")).toBeInTheDocument();
  });
});