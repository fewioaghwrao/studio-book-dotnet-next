"use client";

import ErrorPageTemplate from "@/components/errors/ErrorPageTemplate";
import { useSearchParams } from "next/navigation";

export default function InternalServerErrorPage() {
  const searchParams = useSearchParams();

  return (
    <ErrorPageTemplate
      code="500"
      title="Internal Server Error"
      description="処理中にエラーが発生しました。時間をおいて再度お試しください。"
      error={searchParams.get("error") ?? undefined}
      message={searchParams.get("message") ?? undefined}
      timestamp={searchParams.get("timestamp") ?? undefined}
      primaryAction={{ href: "/", label: "ホームに戻る" }}
    />
  );
}