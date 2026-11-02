import { fireEvent, render, screen, within } from "@testing-library/react";
import DashboardShell from "./DashboardShell";

const mockPathname = jest.fn<string, []>();

jest.mock("next/navigation", () => ({
  usePathname: () => mockPathname(),
}));

function getSidebar() {
  return screen.getByTestId("dashboard-sidebar");
}

beforeEach(() => {
  mockPathname.mockReset();
  window.localStorage.clear();
});

describe("DashboardShell", () => {
  test("管理者メニューの項目とコンテンツを表示する", () => {
    mockPathname.mockReturnValue("/admin/status");

    render(
      <DashboardShell area="admin">
        <p>コンテンツ</p>
      </DashboardShell>
    );

    expect(screen.getByText("コンテンツ")).toBeInTheDocument();

    const sidebar = within(getSidebar());

    for (const label of [
      "統計",
      "会員",
      "スタジオ",
      "予約",
      "監査ログ",
      "AI検索ログ",
      "管理設定",
      "管理者情報",
    ]) {
      expect(sidebar.getByRole("link", { name: label })).toBeInTheDocument();
    }
  });

  test("現在のページのリンクに aria-current を付ける", () => {
    mockPathname.mockReturnValue("/admin/users/12");

    render(
      <DashboardShell area="admin">
        <p>本文</p>
      </DashboardShell>
    );

    const sidebar = within(getSidebar());

    expect(sidebar.getByRole("link", { name: "会員" })).toHaveAttribute(
      "aria-current",
      "page"
    );
    expect(sidebar.getByRole("link", { name: "管理者情報" })).not.toHaveAttribute(
      "aria-current"
    );
  });

  test("管理者情報は /admin と /admin/edit のときだけ active になる", () => {
    mockPathname.mockReturnValue("/admin/edit");

    render(
      <DashboardShell area="admin">
        <p>本文</p>
      </DashboardShell>
    );

    expect(
      within(getSidebar()).getByRole("link", { name: "管理者情報" })
    ).toHaveAttribute("aria-current", "page");
  });

  test("ホストのスタジオ配下では、そのスタジオの設定メニューを表示する", () => {
    mockPathname.mockReturnValue("/host/rooms/7/price-rules");

    render(
      <DashboardShell area="host">
        <p>本文</p>
      </DashboardShell>
    );

    const sidebar = within(getSidebar());

    expect(sidebar.getByText("スタジオ #7 の設定")).toBeInTheDocument();
    expect(sidebar.getByRole("link", { name: "営業時間" })).toHaveAttribute(
      "href",
      "/host/rooms/7/business-hours"
    );
    expect(sidebar.getByRole("link", { name: "休館日" })).toHaveAttribute(
      "href",
      "/host/rooms/7/closures"
    );
    expect(sidebar.getByRole("link", { name: "料金ルール" })).toHaveAttribute(
      "aria-current",
      "page"
    );
    // 親の「スタジオ管理」も active のまま
    expect(sidebar.getByRole("link", { name: "スタジオ管理" })).toHaveAttribute(
      "aria-current",
      "page"
    );
  });

  test("スタジオ一覧ではスタジオ別の設定メニューを表示しない", () => {
    mockPathname.mockReturnValue("/host/rooms");

    render(
      <DashboardShell area="host">
        <p>本文</p>
      </DashboardShell>
    );

    expect(within(getSidebar()).queryByRole("link", { name: "料金ルール" })).toBeNull();
  });

  test("折りたたみボタンでサイドバーを切り替え、状態を保存する", () => {
    mockPathname.mockReturnValue("/host/status");

    render(
      <DashboardShell area="host">
        <p>本文</p>
      </DashboardShell>
    );

    expect(getSidebar()).toHaveAttribute("data-collapsed", "false");

    fireEvent.click(screen.getByRole("button", { name: "サイドバーを折りたたむ" }));

    expect(getSidebar()).toHaveAttribute("data-collapsed", "true");
    expect(window.localStorage.getItem("studiobook.dashboard.sidebarCollapsed")).toBe("1");

    // 折りたたみ中もリンク名は読み上げ用に残る
    expect(within(getSidebar()).getByRole("link", { name: "統計" })).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "サイドバーを開く" }));
    expect(getSidebar()).toHaveAttribute("data-collapsed", "false");
  });

  test("保存済みの折りたたみ状態を復元する", () => {
    window.localStorage.setItem("studiobook.dashboard.sidebarCollapsed", "1");
    mockPathname.mockReturnValue("/host/status");

    render(
      <DashboardShell area="host">
        <p>本文</p>
      </DashboardShell>
    );

    expect(getSidebar()).toHaveAttribute("data-collapsed", "true");
  });

  test("モバイル用ドロワーを開閉できる", () => {
    mockPathname.mockReturnValue("/host/sales");

    render(
      <DashboardShell area="host">
        <p>本文</p>
      </DashboardShell>
    );

    expect(screen.queryByRole("dialog")).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "メニューを開く" }));

    const dialog = screen.getByRole("dialog");
    expect(within(dialog).getByRole("link", { name: "売上" })).toHaveAttribute(
      "aria-current",
      "page"
    );

    fireEvent.keyDown(document, { key: "Escape" });
    expect(screen.queryByRole("dialog")).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "メニューを開く" }));
    fireEvent.click(within(screen.getByRole("dialog")).getByRole("link", { name: "予約" }));
    expect(screen.queryByRole("dialog")).toBeNull();
  });
});
