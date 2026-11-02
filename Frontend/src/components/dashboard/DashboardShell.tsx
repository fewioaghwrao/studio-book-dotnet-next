"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import NavIcon from "./NavIcon";
import {
  DASHBOARD_NAVIGATION,
  type DashboardArea,
  type NavItem,
  getHostRoomId,
  getHostRoomSubNavigation,
  isNavItemActive,
} from "./navigation";

const COLLAPSED_STORAGE_KEY = "studiobook.dashboard.sidebarCollapsed";

function readCollapsed(): boolean {
  try {
    return window.localStorage.getItem(COLLAPSED_STORAGE_KEY) === "1";
  } catch {
    return false;
  }
}

function writeCollapsed(value: boolean) {
  try {
    window.localStorage.setItem(COLLAPSED_STORAGE_KEY, value ? "1" : "0");
  } catch {
    // 保存できない環境（プライベートモード等）では保持しない
  }
}

type SidebarNavProps = {
  area: DashboardArea;
  pathname: string;
  collapsed: boolean;
  onNavigate?: () => void;
};

function SidebarLink({
  item,
  pathname,
  collapsed,
  onNavigate,
  nested = false,
}: {
  item: NavItem;
  pathname: string;
  collapsed: boolean;
  onNavigate?: () => void;
  nested?: boolean;
}) {
  const active = isNavItemActive(item, pathname);

  return (
    <Link
      href={item.href}
      onClick={onNavigate}
      aria-current={active ? "page" : undefined}
      title={collapsed ? item.label : undefined}
      className={[
        "flex items-center gap-3 rounded-xl text-sm transition",
        collapsed ? "justify-center px-0 py-2.5" : nested ? "py-2 pl-9 pr-3" : "px-3 py-2.5",
        active
          ? "bg-sky-50 font-semibold text-sky-800"
          : "text-slate-600 hover:bg-stone-100 hover:text-slate-900",
      ].join(" ")}
    >
      <NavIcon
        name={item.icon}
        className={[
          nested ? "h-4 w-4" : "h-5 w-5",
          "shrink-0",
          active ? "text-sky-700" : "text-slate-400",
        ].join(" ")}
      />
      {collapsed ? (
        <span className="sr-only">{item.label}</span>
      ) : (
        <span className="truncate">{item.label}</span>
      )}
    </Link>
  );
}

function SidebarNav({ area, pathname, collapsed, onNavigate }: SidebarNavProps) {
  const navigation = DASHBOARD_NAVIGATION[area];
  const roomSubNav = area === "host" ? getHostRoomSubNavigation(pathname) : null;
  const roomId = area === "host" ? getHostRoomId(pathname) : null;

  return (
    <nav aria-label={navigation.title} className="space-y-5">
      {navigation.groups.map((group) => (
        <div key={group.title}>
          {collapsed ? (
            <div className="mx-auto mb-2 h-px w-6 bg-stone-200" aria-hidden="true" />
          ) : (
            <p className="mb-1.5 px-3 text-[11px] font-semibold tracking-[0.16em] text-slate-400">
              {group.title}
            </p>
          )}

          <ul className="space-y-1">
            {group.items.map((item) => (
              <li key={item.href}>
                <SidebarLink
                  item={item}
                  pathname={pathname}
                  collapsed={collapsed}
                  onNavigate={onNavigate}
                />

                {item.href === "/host/rooms" && roomSubNav && (
                  <div className="mt-1">
                    {!collapsed && (
                      <p className="px-3 pb-1 pl-9 text-[11px] text-slate-400">
                        スタジオ #{roomId} の設定
                      </p>
                    )}
                    <ul className="space-y-0.5">
                      {roomSubNav.map((subItem) => (
                        <li key={subItem.href}>
                          <SidebarLink
                            item={subItem}
                            pathname={pathname}
                            collapsed={collapsed}
                            onNavigate={onNavigate}
                            nested
                          />
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </li>
            ))}
          </ul>
        </div>
      ))}
    </nav>
  );
}

type DashboardShellProps = {
  area: DashboardArea;
  children: React.ReactNode;
};

/**
 * ホスト・管理者画面の共通レイアウト（左サイドバー＋コンテンツ）。
 * - lg 以上：固定サイドバー（折りたたみ可能、状態はブラウザに保存）
 * - lg 未満：上部の「メニュー」ボタンからドロワーで表示
 */
export default function DashboardShell({ area, children }: DashboardShellProps) {
  const pathname = usePathname() ?? "";
  const navigation = DASHBOARD_NAVIGATION[area];

  const [collapsed, setCollapsed] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);

  // サーバー描画との不一致を避けるため、保存済みの折りたたみ状態はマウント後に反映する
  useEffect(() => {
    if (readCollapsed()) {
      // eslint-disable-next-line react-hooks/set-state-in-effect -- 初回マウント時に localStorage の値を反映するだけ
      setCollapsed(true);
    }
  }, []);

  useEffect(() => {
    if (!drawerOpen) return;

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setDrawerOpen(false);
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [drawerOpen]);

  const toggleCollapsed = () => {
    setCollapsed((prev) => {
      const next = !prev;
      writeCollapsed(next);
      return next;
    });
  };

  const closeDrawer = () => setDrawerOpen(false);

  return (
    <div className="flex w-full">
      {/* デスクトップ用サイドバー */}
      <aside
        data-testid="dashboard-sidebar"
        data-collapsed={collapsed ? "true" : "false"}
        className={[
          "sticky top-[73px] hidden h-[calc(100vh-73px)] shrink-0 flex-col border-r border-stone-200 bg-white lg:flex",
          "transition-[width] duration-200",
          collapsed ? "w-[72px]" : "w-60",
        ].join(" ")}
      >
        <div
          className={[
            "flex items-center border-b border-stone-100 py-4",
            collapsed ? "justify-center px-2" : "justify-between px-4",
          ].join(" ")}
        >
          {!collapsed && (
            <div className="min-w-0">
              <p className="text-[11px] font-semibold tracking-[0.2em] text-sky-700">
                STUDIO BOOK
              </p>
              <p className="mt-0.5 flex items-center gap-2 text-sm font-semibold text-slate-800">
                <span className="truncate">{navigation.title}</span>
              </p>
            </div>
          )}

          <button
            type="button"
            onClick={toggleCollapsed}
            aria-label={collapsed ? "サイドバーを開く" : "サイドバーを折りたたむ"}
            aria-expanded={!collapsed}
            className="rounded-lg p-1.5 text-slate-400 transition hover:bg-stone-100 hover:text-slate-700"
          >
            <svg
              className={["h-5 w-5 transition-transform", collapsed ? "rotate-180" : ""].join(" ")}
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth={1.8}
              strokeLinecap="round"
              strokeLinejoin="round"
              aria-hidden="true"
            >
              <path d="M15 18l-6-6 6-6" />
            </svg>
          </button>
        </div>

        <div className={["flex-1 overflow-y-auto py-4", collapsed ? "px-2" : "px-3"].join(" ")}>
          <SidebarNav area={area} pathname={pathname} collapsed={collapsed} />
        </div>

        {!collapsed && (
          <div className="border-t border-stone-100 px-4 py-3">
            <span className="inline-flex rounded-full bg-stone-100 px-2.5 py-1 text-[11px] font-semibold text-slate-500">
              {navigation.badge}
            </span>
          </div>
        )}
      </aside>

      <div className="min-w-0 flex-1">
        {/* モバイル・タブレット用メニューバー */}
        <div className="sticky top-[73px] z-20 flex items-center gap-3 border-b border-stone-200 bg-white/95 px-4 py-2.5 backdrop-blur lg:hidden">
          <button
            type="button"
            onClick={() => setDrawerOpen(true)}
            aria-label="メニューを開く"
            aria-expanded={drawerOpen}
            className="inline-flex items-center gap-2 rounded-xl border border-stone-200 px-3 py-1.5 text-sm text-slate-700 hover:border-sky-300 hover:text-sky-700"
          >
            <svg
              className="h-4 w-4"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth={1.8}
              strokeLinecap="round"
              aria-hidden="true"
            >
              <path d="M4 6h16M4 12h16M4 18h16" />
            </svg>
            メニュー
          </button>
          <span className="truncate text-sm font-semibold text-slate-700">
            {navigation.title}
          </span>
        </div>

        {children}
      </div>

      {/* モバイル用ドロワー */}
      {drawerOpen && (
        <div className="fixed inset-0 z-40 lg:hidden" role="dialog" aria-modal="true" aria-label={navigation.title}>
          <button
            type="button"
            aria-label="メニューを閉じる"
            onClick={closeDrawer}
            className="absolute inset-0 bg-slate-900/30"
          />
          <div className="absolute inset-y-0 left-0 flex w-72 max-w-[85vw] flex-col bg-white shadow-xl">
            <div className="flex items-center justify-between border-b border-stone-100 px-4 py-4">
              <div>
                <p className="text-[11px] font-semibold tracking-[0.2em] text-sky-700">
                  STUDIO BOOK
                </p>
                <p className="mt-0.5 text-sm font-semibold text-slate-800">
                  {navigation.title}
                </p>
              </div>
              <button
                type="button"
                onClick={closeDrawer}
                aria-label="閉じる"
                className="rounded-lg p-1.5 text-slate-400 hover:bg-stone-100 hover:text-slate-700"
              >
                <svg
                  className="h-5 w-5"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth={1.8}
                  strokeLinecap="round"
                  aria-hidden="true"
                >
                  <path d="M6 6l12 12M18 6L6 18" />
                </svg>
              </button>
            </div>
            <div className="flex-1 overflow-y-auto px-3 py-4">
              <SidebarNav
                area={area}
                pathname={pathname}
                collapsed={false}
                onNavigate={closeDrawer}
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
