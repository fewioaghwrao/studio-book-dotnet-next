import ErrorPageTemplate from "@/components/errors/ErrorPageTemplate";

export default function NotFoundPage() {
  return (
    <ErrorPageTemplate
      code="404"
      title="Not Found"
      description="お探しのページは移動または削除された可能性があります。"
      primaryAction={{ href: "/", label: "ホームに戻る" }}
    />
  );
}