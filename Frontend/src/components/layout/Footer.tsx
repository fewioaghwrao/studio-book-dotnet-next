import Link from "next/link";

export default function Footer() {
  return (
    <footer className="border-t border-stone-200 bg-stone-100">
      <div className="mx-auto max-w-6xl px-4 py-6 text-center text-sm text-slate-500">
        <div className="flex flex-wrap items-center justify-center gap-x-4 gap-y-2">
          <Link href="/terms" className="hover:text-sky-700">
            利用規約
          </Link>
          <Link href="/privacy" className="hover:text-sky-700">
            プライバシーポリシー
          </Link>
          <Link href="/legal/commerce" className="hover:text-sky-700">
            特定商取引法に基づく表記
          </Link>
        </div>
        <div className="mt-2">&copy; N.O. All rights reserved.</div>
      </div>
    </footer>
  );
}