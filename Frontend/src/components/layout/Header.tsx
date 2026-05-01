import Link from "next/link";
import AuthNav from "./AuthNav";

export default function Header() {
  return (
    <header className="sticky top-0 z-30 border-b border-stone-200 bg-white/90 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4">
        <Link href="/" className="flex items-center gap-3">
          <img
            src="/images/logo.png"
            alt="Studio Book ロゴ"
            className="h-10 w-auto"
          />
        </Link>

        <div className="flex items-center gap-4">
          <AuthNav />
        </div>
      </div>
    </header>
  );
}