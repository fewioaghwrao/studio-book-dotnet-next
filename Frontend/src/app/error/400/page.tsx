"use client";

import ErrorPageTemplate from "@/components/errors/ErrorPageTemplate";
import { useSearchParams } from "next/navigation";

export default function BadRequestPage() {
  const searchParams = useSearchParams();

  return (
    <ErrorPageTemplate
      code="400"
      title="Bad Request"
      description="リクエストが正しくありません。入力内容やURLをご確認ください。"
      path={searchParams.get("path") ?? undefined}
      error={searchParams.get("error") ?? undefined}
      message={searchParams.get("message") ?? undefined}
      primaryAction={{ href: "/", label: "ホームに戻る" }}
      secondaryAction={{ label: "前のページへ", onClick: "back" }}
    />
  );
}