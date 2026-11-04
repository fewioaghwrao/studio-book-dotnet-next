import {
  DEVELOPMENT_API_BASE_URL,
  getApiBaseUrl,
  normalizeApiBaseUrl,
} from "./apiConfig";

const ORIGINAL = process.env.NEXT_PUBLIC_API_BASE_URL;

afterEach(() => {
  if (ORIGINAL === undefined) {
    delete process.env.NEXT_PUBLIC_API_BASE_URL;
  } else {
    process.env.NEXT_PUBLIC_API_BASE_URL = ORIGINAL;
  }
});

describe("normalizeApiBaseUrl", () => {
  test("前後の空白と末尾の / を取り除く", () => {
    expect(normalizeApiBaseUrl(" https://api.example.com/ ")).toBe(
      "https://api.example.com"
    );
    expect(normalizeApiBaseUrl("https://api.example.com//")).toBe(
      "https://api.example.com"
    );
  });

  test("未設定・空文字は null", () => {
    expect(normalizeApiBaseUrl(undefined)).toBeNull();
    expect(normalizeApiBaseUrl("")).toBeNull();
    expect(normalizeApiBaseUrl("   ")).toBeNull();
  });
});

describe("getApiBaseUrl", () => {
  test("環境変数が設定されていればその値を使う", () => {
    process.env.NEXT_PUBLIC_API_BASE_URL = "https://api.example.com/";

    expect(getApiBaseUrl()).toBe("https://api.example.com");
  });

  test("未設定なら開発用の既定値を使う", () => {
    delete process.env.NEXT_PUBLIC_API_BASE_URL;

    expect(getApiBaseUrl()).toBe(DEVELOPMENT_API_BASE_URL);
    expect(DEVELOPMENT_API_BASE_URL).toBe("https://localhost:7226");
  });
});
