import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import LogoutConfirmDialog from "./LogoutConfirmDialog";

function renderDialog(
  props: Partial<React.ComponentProps<typeof LogoutConfirmDialog>> = {}
) {
  const onCancel = jest.fn();
  const onConfirm = jest.fn();

  const utils = render(
    <LogoutConfirmDialog
      open
      userName="ホストユーザー"
      roleLabel="スタジオ提供者"
      onCancel={onCancel}
      onConfirm={onConfirm}
      {...props}
    />
  );

  return { ...utils, onCancel, onConfirm };
}

describe("LogoutConfirmDialog", () => {
  test("open=false のときは何も表示しない", () => {
    renderDialog({ open: false });

    expect(screen.queryByRole("alertdialog")).toBeNull();
  });

  test("タイトル・ユーザー名・ロールを表示し、キャンセルに初期フォーカスする", () => {
    renderDialog();

    const dialog = screen.getByRole("alertdialog", { name: "ログアウトしますか？" });

    expect(dialog).toHaveAttribute("aria-modal", "true");
    expect(screen.getByText("ホストユーザー さん")).toBeInTheDocument();
    expect(screen.getByText("スタジオ提供者")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "キャンセル" })).toHaveFocus();
  });

  test("body 直下に描画し、表示中は背面のスクロールを止める", () => {
    const { unmount } = renderDialog();

    expect(screen.getByRole("alertdialog").closest("body")).toBe(document.body);
    expect(document.body.style.overflow).toBe("hidden");

    unmount();
    expect(document.body.style.overflow).toBe("");
  });

  test("ボタンでそれぞれのハンドラを呼ぶ", async () => {
    const user = userEvent.setup();
    const { onCancel, onConfirm } = renderDialog();

    await user.click(screen.getByRole("button", { name: "ログアウトする" }));
    expect(onConfirm).toHaveBeenCalledTimes(1);

    await user.click(screen.getByRole("button", { name: "キャンセル" }));
    expect(onCancel).toHaveBeenCalledTimes(1);
  });

  test("Esc と背景クリックで閉じる", () => {
    const { onCancel } = renderDialog();

    fireEvent.keyDown(document, { key: "Escape" });
    fireEvent.click(screen.getByTestId("logout-dialog-backdrop"));

    expect(onCancel).toHaveBeenCalledTimes(2);
  });

  test("Tab フォーカスはダイアログ内で循環する", async () => {
    const user = userEvent.setup();
    renderDialog();

    const cancel = screen.getByRole("button", { name: "キャンセル" });
    const confirm = screen.getByRole("button", { name: "ログアウトする" });

    expect(cancel).toHaveFocus();
    await user.tab();
    expect(confirm).toHaveFocus();
    await user.tab();
    expect(cancel).toHaveFocus();
    await user.tab({ shift: true });
    expect(confirm).toHaveFocus();
  });

  test("処理中はボタンを無効化し、Esc・背景クリックでも閉じない", () => {
    const { onCancel } = renderDialog({ isProcessing: true });

    expect(screen.getByRole("button", { name: "ログアウト中..." })).toBeDisabled();
    expect(screen.getByRole("button", { name: "キャンセル" })).toBeDisabled();

    fireEvent.keyDown(document, { key: "Escape" });
    fireEvent.click(screen.getByTestId("logout-dialog-backdrop"));

    expect(onCancel).not.toHaveBeenCalled();
  });
});
