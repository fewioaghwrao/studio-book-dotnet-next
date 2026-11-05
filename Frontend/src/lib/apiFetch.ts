import { clearAuthToken, getAuthToken } from "./authToken";

/**
 * API 呼び出しの共通処理。
 * - 保存済みのアクセストークンがあれば `Authorization: Bearer` ヘッダーを付ける
 * - トークンを付けたのに 401 が返った場合（有効期限切れ・無効）は、保存済みトークンを削除する
 *   （画面側の 401 処理＝ログイン画面への誘導などはこれまでどおり各ページで行う）
 */
export async function apiFetch(url: string, options: RequestInit = {}): Promise<Response> {
  const token = getAuthToken();

  const { headers: optionHeaders, ...rest } = options;

  const response = await fetch(url, {
    ...rest,
    headers: {
      "Content-Type": "application/json",
      ...(optionHeaders as Record<string, string>),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });

  if (token && response?.status === 401) {
    clearAuthToken();
  }

  return response;
}
