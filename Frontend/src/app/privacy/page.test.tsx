import { render, screen } from "@testing-library/react";
import PrivacyPage from "./page";

describe("PrivacyPage", () => {
  test("ページタイトルと最終更新日を表示する", () => {
    render(<PrivacyPage />);

    expect(
      screen.getByRole("heading", { name: "プライバシーポリシー", level: 1 })
    ).toBeInTheDocument();

    expect(screen.getByText("最終更新日：2025-10-23")).toBeInTheDocument();
  });

  test("パンくずリンクを表示する", () => {
    render(<PrivacyPage />);

    expect(
      screen.getByRole("navigation", { name: "breadcrumb" })
    ).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "ホーム" })).toHaveAttribute(
      "href",
      "/"
    );

    expect(screen.getAllByText("プライバシーポリシー").length).toBeGreaterThan(0);
  });

  test("各セクション見出しを表示する", () => {
    render(<PrivacyPage />);

    expect(
      screen.getByRole("heading", { name: "1. 基本方針" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "2. 取得する情報" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "3. 利用目的" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "4. 第三者提供" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "5. 委託先の監督" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "6. 安全管理措置" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "7. Cookie・解析" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "8. 開示・訂正・削除" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "9. お問い合わせ" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "10. 改定" })
    ).toBeInTheDocument();
  });

  test("取得する情報の項目を表示する", () => {
    render(<PrivacyPage />);

    expect(
      screen.getByText("登録情報（氏名、メールアドレス、電話番号 など）")
    ).toBeInTheDocument();

    expect(
      screen.getByText("予約・決済情報（利用日時、金額、決済手段 など）")
    ).toBeInTheDocument();

    expect(
      screen.getByText("アクセスログ、IPアドレス、ブラウザ情報、Cookie など")
    ).toBeInTheDocument();
  });

  test("利用目的の項目を表示する", () => {
    render(<PrivacyPage />);

    expect(screen.getByText("本サービスの提供・運営・本人確認"))
      .toBeInTheDocument();

    expect(screen.getByText("予約・決済の確認、連絡対応"))
      .toBeInTheDocument();

    expect(screen.getByText("料金請求・返金処理")).toBeInTheDocument();

    expect(screen.getByText("問い合わせ対応、サービス改善・新機能開発"))
      .toBeInTheDocument();

    expect(screen.getByText("不正利用防止、法令遵守")).toBeInTheDocument();
  });

  test("本文の主要文言を表示する", () => {
    render(<PrivacyPage />);

    expect(
      screen.getByText("当運営者は、個人情報保護法等を遵守し、適切に取り扱います。")
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "本人同意、法令に基づく場合、または委託先への必要な提供を除き、第三者提供は行いません。"
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText("個人情報の取扱いを委託する場合、適切な監督を行います。")
    ).toBeInTheDocument();

    expect(
      screen.getByText("アクセス制御、暗号化、ログ監査等の対策を講じます。")
    ).toBeInTheDocument();

    expect(
      screen.getByText("体験向上・利用分析のためCookieを使用します。設定により無効化できます。")
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "ユーザーは当運営者に対し、保有個人データの開示・訂正・削除等を請求できます。"
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText("本ポリシーは必要に応じて改定し、サイト上に公表します。")
    ).toBeInTheDocument();
  });

test("問い合わせ情報とメールリンクを表示する", () => {
  render(<PrivacyPage />);

  expect(
    screen.getByText((content) => content.includes("N.O.（運営者）"))
  ).toBeInTheDocument();

  expect(
    screen.getByRole("link", { name: "info@example.com" })
  ).toHaveAttribute("href", "mailto:info@example.com");

  expect(
    screen.getByText((content) =>
      content.includes("所在地：東京都〇〇区〇〇1-2-3")
    )
  ).toBeInTheDocument();
});
});