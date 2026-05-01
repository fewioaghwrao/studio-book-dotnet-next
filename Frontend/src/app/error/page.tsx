"use client";

import ErrorPageTemplate from "@/components/errors/ErrorPageTemplate";
import { useSearchParams } from "next/navigation";

export default function GenericErrorPage() {
  const searchParams = useSearchParams();

  const status = searchParams.get("status") ?? "-";
  const error = searchParams.get("error") ?? undefined;
  const message = searchParams.get("message") ?? undefined;
  const path = searchParams.get("path") ?? undefined;

  return (
    <ErrorPageTemplate
      code={status}
      title="エラーが発生しました"
      description="申し訳ありません。ページの表示中に問題が発生しました。"
      path={path}
      error={error}
      message={message}
      primaryAction={{ href: "/", label: "ホームに戻る" }}
      secondaryAction={{ label: "前のページへ戻る", onClick: "back" }}
    />
  );
}