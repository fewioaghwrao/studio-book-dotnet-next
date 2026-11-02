import type { NavIconName } from "./NavIcon";

export type DashboardArea = "admin" | "host";

export type NavItem = {
  href: string;
  label: string;
  icon: NavIconName;
  /**
   * active 判定に追加で一致させるパス（例：会員情報の編集画面）。
   * 指定がなければ href 自身とその配下（href/...）を active とする。
   */
  alsoMatch?: string[];
  /** true のとき href 配下は active にしない（/admin, /host のトップ用） */
  exact?: boolean;
};

export type NavGroup = {
  title: string;
  items: NavItem[];
};

export type DashboardNavigation = {
  /** サイドバー上部の表示名 */
  title: string;
  /** バッジ表示（Admin / Host） */
  badge: string;
  groups: NavGroup[];
};

export const DASHBOARD_NAVIGATION: Record<DashboardArea, DashboardNavigation> = {
  admin: {
    title: "管理者メニュー",
    badge: "Admin",
    groups: [
      {
        title: "概要",
        items: [{ href: "/admin/status", label: "統計", icon: "chart" }],
      },
      {
        title: "管理",
        items: [
          { href: "/admin/users", label: "会員", icon: "users" },
          { href: "/admin/rooms", label: "スタジオ", icon: "building" },
          { href: "/admin/reservations", label: "予約", icon: "calendar" },
        ],
      },
      {
        title: "ログ・設定",
        items: [
          { href: "/admin/logs", label: "監査ログ", icon: "log" },
          { href: "/admin/ai-search-logs", label: "AI検索ログ", icon: "sparkles" },
          { href: "/admin/settings", label: "管理設定", icon: "settings" },
        ],
      },
      {
        title: "アカウント",
        items: [
          {
            href: "/admin",
            label: "管理者情報",
            icon: "user",
            exact: true,
            alsoMatch: ["/admin/edit"],
          },
        ],
      },
    ],
  },
  host: {
    title: "スタジオ提供者メニュー",
    badge: "Host",
    groups: [
      {
        title: "概要",
        items: [{ href: "/host/status", label: "統計", icon: "chart" }],
      },
      {
        title: "スタジオ",
        items: [{ href: "/host/rooms", label: "スタジオ管理", icon: "building" }],
      },
      {
        title: "運営",
        items: [
          { href: "/host/reservations", label: "予約", icon: "calendar" },
          { href: "/host/sales", label: "売上", icon: "yen" },
          { href: "/host/reviews", label: "レビュー", icon: "star" },
        ],
      },
      {
        title: "アカウント",
        items: [
          {
            href: "/host",
            label: "会員情報",
            icon: "user",
            exact: true,
            alsoMatch: ["/host/edit"],
          },
        ],
      },
    ],
  },
};

/**
 * ホストのスタジオ配下（/host/rooms/{id}/...）にいるときだけ表示するサブメニュー。
 * 営業時間・休館日・料金ルールはスタジオごとの設定のため、スタジオ選択後に表示します。
 */
export function getHostRoomSubNavigation(pathname: string): NavItem[] | null {
  const match = /^\/host\/rooms\/(\d+)(?:\/|$)/.exec(pathname);

  if (!match) {
    return null;
  }

  const base = `/host/rooms/${match[1]}`;

  return [
    { href: base, label: "基本情報", icon: "info", exact: true },
    { href: `${base}/business-hours`, label: "営業時間", icon: "clock" },
    { href: `${base}/closures`, label: "休館日", icon: "closed" },
    { href: `${base}/price-rules`, label: "料金ルール", icon: "tag" },
  ];
}

export function getHostRoomId(pathname: string): string | null {
  const match = /^\/host\/rooms\/(\d+)(?:\/|$)/.exec(pathname);
  return match ? match[1] : null;
}

/** サイドバー付きレイアウト（/admin, /host 配下）かどうか */
export function isDashboardPath(pathname: string): boolean {
  return /^\/(admin|host)(\/|$)/.test(pathname);
}

export function isNavItemActive(item: NavItem, pathname: string): boolean {
  const normalized = pathname.length > 1 ? pathname.replace(/\/+$/, "") : pathname;

  if (item.alsoMatch?.includes(normalized)) {
    return true;
  }

  if (normalized === item.href) {
    return true;
  }

  if (item.exact) {
    return false;
  }

  return normalized.startsWith(`${item.href}/`);
}
