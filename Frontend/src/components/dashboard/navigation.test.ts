import {
  getHostRoomId,
  getHostRoomSubNavigation,
  isDashboardPath,
  isNavItemActive,
  type NavItem,
} from "./navigation";

describe("isNavItemActive", () => {
  const sales: NavItem = { href: "/host/sales", label: "売上", icon: "yen" };
  const top: NavItem = {
    href: "/host",
    label: "会員情報",
    icon: "user",
    exact: true,
    alsoMatch: ["/host/edit"],
  };

  test("同じパスと配下のパスを active とする", () => {
    expect(isNavItemActive(sales, "/host/sales")).toBe(true);
    expect(isNavItemActive(sales, "/host/sales/15")).toBe(true);
    expect(isNavItemActive(sales, "/host/sales/")).toBe(true);
  });

  test("前方一致だけのパスは active にしない", () => {
    expect(isNavItemActive(sales, "/host/salesreport")).toBe(false);
  });

  test("exact の項目は配下を active にせず、alsoMatch は active にする", () => {
    expect(isNavItemActive(top, "/host")).toBe(true);
    expect(isNavItemActive(top, "/host/edit")).toBe(true);
    expect(isNavItemActive(top, "/host/rooms")).toBe(false);
  });
});

describe("getHostRoomSubNavigation", () => {
  test("スタジオ配下ではサブメニューを返す", () => {
    const items = getHostRoomSubNavigation("/host/rooms/3/closures");

    expect(items?.map((x) => x.href)).toEqual([
      "/host/rooms/3",
      "/host/rooms/3/business-hours",
      "/host/rooms/3/closures",
      "/host/rooms/3/price-rules",
    ]);
    expect(getHostRoomId("/host/rooms/3/closures")).toBe("3");
  });

  test("スタジオ一覧やその他の画面では null", () => {
    expect(getHostRoomSubNavigation("/host/rooms")).toBeNull();
    expect(getHostRoomSubNavigation("/host/sales/3")).toBeNull();
    expect(getHostRoomId("/host/rooms")).toBeNull();
  });
});

describe("isDashboardPath", () => {
  test("/admin と /host 配下だけ true", () => {
    expect(isDashboardPath("/admin")).toBe(true);
    expect(isDashboardPath("/host/sales/1")).toBe(true);
    expect(isDashboardPath("/")).toBe(false);
    expect(isDashboardPath("/hosting")).toBe(false);
    expect(isDashboardPath("/rooms/1")).toBe(false);
  });
});
