import type { NextConfig } from "next";
import { PHASE_PRODUCTION_BUILD } from "next/constants";

const nextConfig: NextConfig = {
  /* config options here */
};

export default function config(phase: string): NextConfig {
  // 本番ビルドで API の URL が未設定だと、ブラウザから https://localhost:7226 を呼ぶ
  // 壊れたサイトが静かにデプロイされてしまうため、ビルドの時点で止める
  if (
    phase === PHASE_PRODUCTION_BUILD &&
    !process.env.NEXT_PUBLIC_API_BASE_URL?.trim()
  ) {
    throw new Error(
      "NEXT_PUBLIC_API_BASE_URL が設定されていません。本番ビルドでは API のベースURL（例: https://studio-book-api-xxxx.herokuapp.com）を環境変数に設定してください。"
    );
  }

  return nextConfig;
}
