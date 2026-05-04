import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import GlobalError from "./global-error";

describe("GlobalError", () => {
  test("500エラー画面の基本情報を表示する", () => {
    const reset = jest.fn();

    render(
      <GlobalError
        error={new Error("テスト用エラー")}
        reset={reset}
      />
    );

    expect(screen.getByText("ERROR")).toBeInTheDocument();

    expect(
      screen.getByRole("heading", { name: "500 Internal Server Error" })
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "処理中に予期しないエラーが発生しました。時間をおいて再度お試しください。"
      )
    ).toBeInTheDocument();

    expect(screen.getByText(/Message:/)).toBeInTheDocument();
    expect(screen.getByText("テスト用エラー")).toBeInTheDocument();
  });

  test("再試行ボタンを押すと reset が呼ばれる", async () => {
    const user = userEvent.setup();
    const reset = jest.fn();

    render(
      <GlobalError
        error={new Error("テスト用エラー")}
        reset={reset}
      />
    );

    await user.click(screen.getByRole("button", { name: "再試行" }));

    expect(reset).toHaveBeenCalledTimes(1);
  });

  test("ホームに戻るリンクの遷移先がトップページである", () => {
    const reset = jest.fn();

    render(
      <GlobalError
        error={new Error("テスト用エラー")}
        reset={reset}
      />
    );

    expect(screen.getByRole("link", { name: "ホームに戻る" }))
      .toHaveAttribute("href", "/");
  });

  test("error.message が空の場合、既定メッセージを表示する", () => {
    const reset = jest.fn();

    render(
      <GlobalError
        error={new Error("")}
        reset={reset}
      />
    );

    expect(screen.getByText("Unexpected error")).toBeInTheDocument();
  });
});