"use client";

import { usePathname } from "next/navigation";
import { isDashboardPath } from "@/components/dashboard/navigation";

/**
 * ヘッダーの横幅コンテナ。
 * ホスト・管理者画面（サイドバー付きの全幅レイアウト）ではロゴ位置をサイドバーに揃えるため全幅にする。
 */
export default function HeaderContainer({
  children,
}: {
  children: React.ReactNode;
}) {
  const pathname = usePathname() ?? "";
  const isDashboard = isDashboardPath(pathname);

  return (
    <div
      data-layout={isDashboard ? "dashboard" : "site"}
      className={[
        "flex items-center justify-between py-4",
        isDashboard ? "w-full px-4 lg:px-6" : "mx-auto max-w-6xl px-4",
      ].join(" ")}
    >
      {children}
    </div>
  );
}
