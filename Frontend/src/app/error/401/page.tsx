"use client";

import ErrorPageTemplate from "@/components/errors/ErrorPageTemplate";
import { useSearchParams } from "next/navigation";

export default function UnauthorizedPage() {
  const searchParams = useSearchParams();

  return (
    <ErrorPageTemplate
      code="401"
      title="Unauthorized"
      description="このページを表示するにはログインが必要です。"
      path={searchParams.get("path") ?? undefined}
      message={searchParams.get("message") ?? undefined}
      primaryAction={{ href: "/login", label: "ログインへ" }}
      secondaryAction={{ href: "/", label: "ホームに戻る" }}
    />
  );
}