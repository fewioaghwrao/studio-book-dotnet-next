import {
  AUTH_TOKEN_STORAGE_KEY,
  clearAuthToken,
  getAuthToken,
  setAuthToken,
} from "./authToken";

beforeEach(() => {
  window.localStorage.clear();
  jest.restoreAllMocks();
});

describe("authToken", () => {
  test("保存・取得・削除ができる", () => {
    expect(getAuthToken()).toBeNull();

    setAuthToken("jwt-token");
    expect(window.localStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBe("jwt-token");
    expect(getAuthToken()).toBe("jwt-token");

    clearAuthToken();
    expect(getAuthToken()).toBeNull();
  });

  test("空文字・空白だけのトークンは未ログイン扱い", () => {
    window.localStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "   ");

    expect(getAuthToken()).toBeNull();
  });

  test("localStorage が使えない環境でも例外を投げない", () => {
    jest.spyOn(Storage.prototype, "getItem").mockImplementation(() => {
      throw new Error("blocked");
    });
    jest.spyOn(Storage.prototype, "setItem").mockImplementation(() => {
      throw new Error("blocked");
    });
    jest.spyOn(Storage.prototype, "removeItem").mockImplementation(() => {
      throw new Error("blocked");
    });

    expect(getAuthToken()).toBeNull();
    expect(() => setAuthToken("jwt-token")).not.toThrow();
    expect(() => clearAuthToken()).not.toThrow();
  });
});
