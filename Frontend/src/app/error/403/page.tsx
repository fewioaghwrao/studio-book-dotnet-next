"use client";

import ErrorPageTemplate from "@/components/errors/ErrorPageTemplate";
import { useSearchParams } from "next/navigation";

export default function ForbiddenPage() {
  const searchParams = useSearchParams();

  return (
    <ErrorPageTemplate
      code="403"
      title="Forbidden"
      description="このページにアクセスする権限がありません。"
      path={searchParams.get("path") ?? undefined}
      message={searchParams.get("message") ?? undefined}
      primaryAction={{ href: "/", label: "ホームに戻る" }}
      secondaryAction={{ label: "前のページへ", onClick: "back" }}
    />
  );
}