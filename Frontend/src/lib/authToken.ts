/**
 * ログイン中のアクセストークン（JWT）の保存・取得・削除。
 *
 * 認証方式は Bearer に統一しています。
 * - ログイン成功時に API が返す JWT をブラウザの localStorage に保存する
 * - API 呼び出し時は apiFetch が `Authorization: Bearer <token>` ヘッダーを付ける
 * - Cookie は使わない（フロントと API が別ドメインのため）
 *
 * localStorage は JavaScript から読めるため、XSS 対策（ユーザー入力をそのまま HTML として
 * 描画しない、外部スクリプトを増やさない等）が前提になります。
 */
export const AUTH_TOKEN_STORAGE_KEY = "token";

export function getAuthToken(): string | null {
  if (typeof window === "undefined") {
    return null;
  }

  try {
    const token = window.localStorage.getItem(AUTH_TOKEN_STORAGE_KEY);
    return token && token.trim() ? token : null;
  } catch {
    // プライベートモード等で localStorage が使えない場合は未ログイン扱い
    return null;
  }
}

export function setAuthToken(token: string): void {
  try {
    window.localStorage.setItem(AUTH_TOKEN_STORAGE_KEY, token);
  } catch {
    // 保存できない環境ではそのまま（以降の API 呼び出しは未ログイン扱いになる）
  }
}

export function clearAuthToken(): void {
  if (typeof window === "undefined") {
    return;
  }

  try {
    window.localStorage.removeItem(AUTH_TOKEN_STORAGE_KEY);
  } catch {
    // 何もしない
  }
}
