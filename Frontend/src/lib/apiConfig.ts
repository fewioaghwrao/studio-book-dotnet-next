/**
 * バックエンドAPIのベースURL。
 *
 * - 本番・プレビュー：環境変数 NEXT_PUBLIC_API_BASE_URL を必ず設定する
 *   （未設定のまま `next build` するとビルドを失敗させる。next.config.ts を参照）
 * - 開発・テスト：未設定なら Visual Studio 既定の https://localhost:7226 を使う
 *
 * NEXT_PUBLIC_ の環境変数はビルド時に値が埋め込まれるため、
 * `process.env.NEXT_PUBLIC_API_BASE_URL` をこのファイルでそのまま参照する必要がある。
 */
export const DEVELOPMENT_API_BASE_URL = "https://localhost:7226";

export function normalizeApiBaseUrl(value: string | undefined | null): string | null {
  const trimmed = value?.trim();

  if (!trimmed) {
    return null;
  }

  // 末尾の / を取り除き、`${apiBaseUrl}/api/...` で二重スラッシュにならないようにする
  return trimmed.replace(/\/+$/, "");
}

export function getApiBaseUrl(): string {
  return (
    normalizeApiBaseUrl(process.env.NEXT_PUBLIC_API_BASE_URL) ??
    DEVELOPMENT_API_BASE_URL
  );
}
