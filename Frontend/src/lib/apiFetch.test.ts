import { apiFetch } from "./apiFetch";
import { AUTH_TOKEN_STORAGE_KEY } from "./authToken";

const mockFetch = jest.fn();

beforeEach(() => {
  window.localStorage.clear();
  mockFetch.mockReset();
  global.fetch = mockFetch as unknown as typeof fetch;
});

describe("apiFetch", () => {
  test("保存済みのトークンを Authorization: Bearer ヘッダーで送る", async () => {
    window.localStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "jwt-token");
    mockFetch.mockResolvedValueOnce({ ok: true, status: 200 });

    await apiFetch("https://api.example.com/api/auth/me", { method: "GET" });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://api.example.com/api/auth/me",
      expect.objectContaining({
        method: "GET",
        headers: expect.objectContaining({
          "Content-Type": "application/json",
          Authorization: "Bearer jwt-token",
        }),
      })
    );
  });

  test("トークンがなければ Authorization ヘッダーを付けない", async () => {
    mockFetch.mockResolvedValueOnce({ ok: true, status: 200 });

    await apiFetch("https://api.example.com/api/rooms");

    const [, init] = mockFetch.mock.calls[0];
    expect(init.headers).not.toHaveProperty("Authorization");
  });

  test("Cookie を送る設定（credentials）は付けない", async () => {
    window.localStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "jwt-token");
    mockFetch.mockResolvedValueOnce({ ok: true, status: 200 });

    await apiFetch("https://api.example.com/api/auth/me");

    const [, init] = mockFetch.mock.calls[0];
    expect(init).not.toHaveProperty("credentials");
  });

  test("トークンを付けて 401 が返ったら、保存済みトークンを削除する", async () => {
    window.localStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "expired-token");
    mockFetch.mockResolvedValueOnce({ ok: false, status: 401 });

    const response = await apiFetch("https://api.example.com/api/auth/me");

    expect(response.status).toBe(401);
    expect(window.localStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBeNull();
  });

  test("401 以外のエラーではトークンを削除しない", async () => {
    window.localStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "jwt-token");
    mockFetch.mockResolvedValueOnce({ ok: false, status: 403 });

    await apiFetch("https://api.example.com/api/admin/status");

    expect(window.localStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBe("jwt-token");
  });
});
