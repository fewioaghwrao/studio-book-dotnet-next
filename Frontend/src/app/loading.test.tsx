import { render, screen } from "@testing-library/react";
import Loading from "./loading";

describe("Loading", () => {
  test("ローディング画面の基本文言を表示する", () => {
    render(<Loading />);

    expect(screen.getByText("画面を読み込んでいます")).toBeInTheDocument();
    expect(
      screen.getByText("データ取得中のため、このままお待ちください。")
    ).toBeInTheDocument();
    expect(screen.getByText("読み込み中...")).toBeInTheDocument();
    expect(
      screen.getByText("画面遷移または一覧データの準備を行っています")
    ).toBeInTheDocument();
    expect(
      screen.getByText("通信状況により数秒かかる場合があります")
    ).toBeInTheDocument();
  });

  test("ローディング表示用のスタイル要素を持つ", () => {
    const { container } = render(<Loading />);

    expect(container.querySelector(".animate-spin")).toBeInTheDocument();
    expect(
      container.querySelector(
        ".animate-\\[loading-bar_1\\.2s_ease-in-out_infinite\\]"
      )
    ).toBeInTheDocument();
  });
});