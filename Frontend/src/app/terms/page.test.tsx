import { render, screen } from "@testing-library/react";
import TermsPage from "./page";

describe("TermsPage", () => {
  test("ページタイトルと最終更新日を表示する", () => {
    render(<TermsPage />);

    expect(
      screen.getByRole("heading", { name: "利用規約", level: 1 })
    ).toBeInTheDocument();

    expect(screen.getByText("最終更新日：2025-10-23")).toBeInTheDocument();
  });

  test("パンくずリンクを表示する", () => {
    render(<TermsPage />);

    expect(
      screen.getByRole("navigation", { name: "breadcrumb" })
    ).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "ホーム" })).toHaveAttribute(
      "href",
      "/"
    );

    expect(screen.getAllByText("利用規約").length).toBeGreaterThan(0);
  });

  test("各条項の見出しを表示する", () => {
    render(<TermsPage />);

    expect(
      screen.getByRole("heading", { name: "第1条（適用）" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "第2条（定義）" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "第3条（登録）" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "第4条（料金・支払）" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "第5条（禁止事項）" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "第6条（免責）" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "第7条（知的財産）" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "第8条（規約の変更）" })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "第9条（準拠法・管轄）" })
    ).toBeInTheDocument();
  });

  test("定義の項目を表示する", () => {
    render(<TermsPage />);

    expect(
      screen.getByText("「ユーザー」…本サービスを利用するすべての者")
    ).toBeInTheDocument();

    expect(
      screen.getByText("「ホスト」…スタジオを登録・貸出するユーザー")
    ).toBeInTheDocument();

    expect(
      screen.getByText("「ゲスト」…スタジオを予約・利用するユーザー")
    ).toBeInTheDocument();
  });

  test("禁止事項の項目を表示する", () => {
    render(<TermsPage />);

    expect(screen.getByText("法令・公序良俗に反する行為"))
      .toBeInTheDocument();

    expect(screen.getByText("無断転貸、設備の破損・汚損"))
      .toBeInTheDocument();

    expect(screen.getByText("システムへの不正アクセス 等"))
      .toBeInTheDocument();
  });

  test("本文の主要文言を表示する", () => {
    render(<TermsPage />);

    expect(
      screen.getByText(
        "本規約は、当運営者が提供するスタジオ時間貸し予約サービス「架空スタジオ予約サービス」（以下「本サービス」）の利用条件を定めるものです。"
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "ユーザーは正確かつ最新の情報を提供するものとし、当運営者は不適当と判断した場合登録を拒否できます。"
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "表示料金を所定の方法（例：クレジットカード）で支払うものとします。キャンセルはキャンセルポリシーに従います。"
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "当運営者はユーザー間のトラブル等について責任を負いません。不可抗力による停止等についても同様です。"
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "本サービスのコンテンツの権利は当運営者または正当な権利者に帰属します。"
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "当運営者は必要に応じて本規約を変更し、本サイト上で告知します。"
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "日本法に準拠し、当運営者所在地を管轄する地方裁判所を第一審の専属的合意管轄とします。"
      )
    ).toBeInTheDocument();
  });
});