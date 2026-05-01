"use client";

import ErrorPageTemplate from "@/components/errors/ErrorPageTemplate";
import { useSearchParams } from "next/navigation";

export default function ServiceUnavailablePage() {
  const searchParams = useSearchParams();

  return (
    <ErrorPageTemplate
      code="503"
      title="Service Unavailable"
      description="現在メンテナンス中、または一時的に混み合っています。しばらくしてから再度お試しください。"
      path={searchParams.get("path") ?? undefined}
      message={searchParams.get("message") ?? undefined}
      primaryAction={{ href: "/", label: "ホームに戻る" }}
      secondaryAction={{ label: "再読み込み", onClick: "reload" }}
    />
  );
}