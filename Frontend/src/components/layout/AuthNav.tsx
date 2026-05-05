"use client";

import Link from "next/link";
import { useEffect, useMemo, useRef, useState } from "react";
import { apiFetch } from "@/lib/apiFetch";

type CurrentUserResponse = {
  isAuthenticated: boolean;
  user?: {
    id: number;
    name: string;
    email: string;
    roles: string[];
  };
};

type RoleKind = "GENERAL" | "ADMIN" | "HOST" | null;

function normalizeRole(roles: string[] | undefined): RoleKind {
  if (!roles || roles.length === 0) return null;

  const upperRoles = roles.map((x) => x.toUpperCase());

  if (upperRoles.some((x) => x.includes("ADMIN"))) return "ADMIN";
  if (upperRoles.some((x) => x.includes("HOST"))) return "HOST";
  if (upperRoles.some((x) => x.includes("GENERAL") || x.includes("USER")))
    return "GENERAL";

  return null;
}

export default function AuthNav() {
  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [isLoading, setIsLoading] = useState(true);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [userName, setUserName] = useState("");
  const [role, setRole] = useState<RoleKind>(null);
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  const menuRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    let ignore = false;

    const fetchCurrentUser = async () => {
      try {
const token = localStorage.getItem("token");

if (!token) {
  if (!ignore) {
    setIsAuthenticated(false);
    setUserName("");
    setRole(null);
  }
  return;
}

const response = await apiFetch(`${apiBaseUrl}/api/auth/me`, {
  method: "GET",
  headers: {
    Authorization: `Bearer ${token}`,
  },
  cache: "no-store",
});

        if (!response.ok) {
          if (!ignore) {
            setIsAuthenticated(false);
            setUserName("");
            setRole(null);
          }
          return;
        }

        const data = (await response.json()) as CurrentUserResponse;

        if (ignore) return;

        if (data.isAuthenticated && data.user) {
          setIsAuthenticated(true);
          setUserName(data.user.name ?? "");
          setRole(normalizeRole(data.user.roles));
        } else {
          setIsAuthenticated(false);
          setUserName("");
          setRole(null);
        }
      } catch {
        if (!ignore) {
          setIsAuthenticated(false);
          setUserName("");
          setRole(null);
        }
      } finally {
        if (!ignore) {
          setIsLoading(false);
        }
      }
    };

    fetchCurrentUser();

    return () => {
      ignore = true;
    };
  }, [apiBaseUrl]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (!menuRef.current) return;
      if (!menuRef.current.contains(event.target as Node)) {
        setIsMenuOpen(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

const handleLogout = async () => {
  const token = localStorage.getItem("token");

  try {
    await apiFetch(`${apiBaseUrl}/api/auth/logout`, {
      method: "POST",
      headers: token
        ? {
            Authorization: `Bearer ${token}`,
          }
        : {},
    });
  } catch {
    // 失敗時も表示だけはリセットしてトップへ戻す
  } finally {
    localStorage.removeItem("token");

    setIsAuthenticated(false);
    setUserName("");
    setRole(null);
    setIsMenuOpen(false);

    window.location.href = "/";
  }
};

  if (isLoading) {
    return (
      <div className="text-sm text-slate-400">
        読み込み中...
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
      <nav className="flex items-center gap-3 text-sm">
        <Link
          href="/auth/login"
          className="text-slate-600 hover:text-sky-700"
        >
          ログイン
        </Link>
        <Link
          href="/signup"
          className="rounded-xl border border-stone-200 px-4 py-2 text-slate-700 hover:border-sky-300 hover:text-sky-700"
        >
          会員登録
        </Link>
      </nav>
    );
  }

  const menuLabel =
    role === "ADMIN"
      ? "管理者メニュー"
      : role === "HOST"
      ? "スタジオ提供者メニュー"
      : userName || "メニュー";

  return (
    <div className="relative" ref={menuRef}>
      <button
        type="button"
        onClick={() => setIsMenuOpen((prev) => !prev)}
        className="inline-flex items-center rounded-xl border border-stone-200 bg-white px-4 py-2 text-sm text-slate-700 shadow-sm hover:border-sky-300 hover:text-sky-700"
      >
        <span className="max-w-[180px] truncate">{menuLabel}</span>
        <svg
          className="ml-2 h-4 w-4"
          viewBox="0 0 20 20"
          fill="currentColor"
          aria-hidden="true"
        >
          <path
            fillRule="evenodd"
            d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.512a.75.75 0 01-1.08 0L5.21 8.27a.75.75 0 01.02-1.06z"
            clipRule="evenodd"
          />
        </svg>
      </button>

      {isMenuOpen && (
        <div className="absolute right-0 mt-2 w-64 overflow-hidden rounded-2xl border border-stone-200 bg-white shadow-lg">
          <div className="border-b border-stone-100 px-4 py-3">
            <div className="text-sm font-semibold text-slate-800">
              {role === "GENERAL" ? userName : menuLabel}
            </div>
            {role === "GENERAL" && (
              <div className="mt-1 text-xs text-slate-500">
                ログイン中
              </div>
            )}
          </div>

          <div className="py-2">
            {role === "GENERAL" && (
              <>
                <Link
                  href="/user"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-stone-50 hover:text-sky-700"
                  onClick={() => setIsMenuOpen(false)}
                >
                  会員情報
                </Link>
                <Link
                  href="/reservations"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-stone-50 hover:text-sky-700"
                  onClick={() => setIsMenuOpen(false)}
                >
                  予約一覧・履歴
                </Link>
              </>
            )}

            {role === "ADMIN" && (
              <>
                <Link
                  href="/admin"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-stone-50 hover:text-sky-700"
                  onClick={() => setIsMenuOpen(false)}
                >
                  会員情報
                </Link>
                <Link
                  href="/admin/status"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-stone-50 hover:text-sky-700"
                  onClick={() => setIsMenuOpen(false)}
                >
                  データ一覧
                </Link>
                <Link
                  href="/admin/users"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-stone-50 hover:text-sky-700"
                  onClick={() => setIsMenuOpen(false)}
                >
                  会員一覧
                </Link>
                <Link
                  href="/admin/rooms"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-stone-50 hover:text-sky-700"
                  onClick={() => setIsMenuOpen(false)}
                >
                  スタジオ一覧
                </Link>
                <Link
                  href="/admin/reservations"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-stone-50 hover:text-sky-700"
                  onClick={() => setIsMenuOpen(false)}
                >
                  予約一覧
                </Link>
                <Link
                  href="/admin/logs"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-stone-50 hover:text-sky-700"
                  onClick={() => setIsMenuOpen(false)}
                >
                  ログ一覧
                </Link>
                <Link
                  href="/admin/settings"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-stone-50 hover:text-sky-700"
                  onClick={() => setIsMenuOpen(false)}
                >
                  管理設定
                </Link>
                <Link
                  href="/admin/ai-search-logs"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-stone-50 hover:text-sky-700"
                  onClick={() => setIsMenuOpen(false)}
                >
                  AI検索ログ
                </Link>
              </>
            )}

            {role === "HOST" && (
              <>
                <Link
                  href="/host"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-stone-50 hover:text-sky-700"
                  onClick={() => setIsMenuOpen(false)}
                >
                  会員情報
                </Link>
                <Link
                  href="/host/rooms"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-stone-50 hover:text-sky-700"
                  onClick={() => setIsMenuOpen(false)}
                >
                  スタジオ一覧
                </Link>
                <Link
                  href="/host/reservations"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-stone-50 hover:text-sky-700"
                  onClick={() => setIsMenuOpen(false)}
                >
                  予約一覧
                </Link>
                <Link
                  href="/host/reviews"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-stone-50 hover:text-sky-700"
                  onClick={() => setIsMenuOpen(false)}
                >
                  レビュー一覧
                </Link>
                <Link
                  href="/host/sales"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-stone-50 hover:text-sky-700"
                  onClick={() => setIsMenuOpen(false)}
                >
                  売上明細一覧
                </Link>
                <Link
                  href="/host/status"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-stone-50 hover:text-sky-700"
                  onClick={() => setIsMenuOpen(false)}
                >
                  統計一覧
                </Link>
              </>
            )}

            <div className="my-2 border-t border-stone-100" />

            <button
              type="button"
              onClick={handleLogout}
              className="block w-full px-4 py-2 text-left text-sm text-slate-700 hover:bg-stone-50 hover:text-sky-700"
            >
              ログアウト
            </button>
          </div>
        </div>
      )}
    </div>
  );
}