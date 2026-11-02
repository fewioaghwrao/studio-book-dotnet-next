import { render, screen } from "@testing-library/react";
import HeaderContainer from "./HeaderContainer";

const mockPathname = jest.fn<string | null, []>();

jest.mock("next/navigation", () => ({
  usePathname: () => mockPathname(),
}));

function renderContainer() {
  render(
    <HeaderContainer>
      <span>ロゴ</span>
    </HeaderContainer>
  );

  return screen.getByText("ロゴ").parentElement as HTMLElement;
}

describe("HeaderContainer", () => {
  test("一般画面では中央寄せの固定幅", () => {
    mockPathname.mockReturnValue("/rooms/1");

    const container = renderContainer();

    expect(container).toHaveAttribute("data-layout", "site");
    expect(container).toHaveClass("max-w-6xl");
  });

  test.each(["/admin", "/admin/status", "/host/rooms/3"])(
    "%s ではサイドバーに合わせて全幅",
    (pathname) => {
      mockPathname.mockReturnValue(pathname);

      const container = renderContainer();

      expect(container).toHaveAttribute("data-layout", "dashboard");
      expect(container).not.toHaveClass("max-w-6xl");
    }
  );

  test("pathname が取得できない場合は一般画面として扱う", () => {
    mockPathname.mockReturnValue(null);

    expect(renderContainer()).toHaveAttribute("data-layout", "site");
  });
});
