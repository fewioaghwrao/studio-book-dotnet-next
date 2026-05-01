import Link from "next/link";

export default function CommercePage() {
  const lastUpdated = "2025-10-23";

  const rows = [
    ["販売事業者", "N.O."],
    ["運営責任者", "N.O."],
    ["所在地", "東京都〇〇区〇〇1-2-3"],
    ["連絡先", "E-mail：info@example.com"],
    ["販売価格", "各スタジオ詳細ページに表示（時間単価/プランによる）"],
    ["商品代金以外の必要料金", "決済手数料（必要な場合）、通信料等"],
    ["支払方法", "クレジットカード決済（Stripe）ほか"],
    ["支払時期", "予約確定時に即時決済"],
    ["役務の提供時期", "予約日時に提供"],
    [
      "キャンセル・変更",
      "各スタジオのキャンセルポリシーに従います。返金の有無・条件はポリシーに記載。",
    ],
    ["返品・交換", "役務の性質上、提供後の返品・交換はできません。"],
    ["動作環境", "最新の主要ブラウザ（Chrome/Edge/Safari/Firefox）"],
    ["特記事項", "未成年者の利用には保護者の同意が必要です。"],
  ];

  return (
    <main>
      <div className="mx-auto max-w-5xl px-4 py-8 md:py-10">
        <nav aria-label="breadcrumb" className="mb-6 text-sm text-slate-500">
          <ol className="flex flex-wrap items-center gap-2">
            <li>
              <Link href="/" className="hover:text-sky-700">
                ホーム
              </Link>
            </li>
            <li>{">"}</li>
            <li className="text-slate-700">特定商取引法に基づく表記</li>
          </ol>
        </nav>

        <h1 className="text-3xl font-semibold text-slate-800">
          特定商取引法に基づく表記
        </h1>
        <p className="mt-3 text-sm text-slate-500">
          最終更新日：{lastUpdated}
        </p>
        <div className="mt-4 border-t border-stone-200" />

        <div className="mt-8 overflow-hidden rounded-2xl border border-stone-200 bg-white">
          <div className="overflow-x-auto">
            <table className="min-w-full border-collapse text-sm">
              <tbody>
                {rows.map(([label, value]) => (
                  <tr key={label} className="border-t border-stone-200 first:border-t-0">
                    <th className="w-[30%] bg-stone-50 px-4 py-4 text-left font-semibold text-slate-700">
                      {label}
                    </th>
                    <td className="px-4 py-4 text-slate-700">
                      {label === "連絡先" ? (
                        <>
                          E-mail：
                          <a
                            href="mailto:info@example.com"
                            className="text-sky-700 hover:text-sky-800"
                          >
                            info@example.com
                          </a>
                        </>
                      ) : (
                        value
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        <p className="mt-4 text-xs text-slate-500">※表記内容は架空のものです。</p>
      </div>
    </main>
  );
}