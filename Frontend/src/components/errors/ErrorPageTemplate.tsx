import Link from "next/link";
import { ReactNode } from "react";

type ErrorPageTemplateProps = {
  code: string;
  title: string;
  description: string;
  path?: string;
  error?: string;
  message?: string;
  timestamp?: string;
  primaryAction?: {
    href: string;
    label: string;
  };
  secondaryAction?: {
    href?: string;
    label: string;
    onClick?: "back" | "reload";
  };
  children?: ReactNode;
};

export default function ErrorPageTemplate({
  code,
  title,
  description,
  path,
  error,
  message,
  timestamp,
  primaryAction,
  secondaryAction,
  children,
}: ErrorPageTemplateProps) {
  return (
    <main className="mx-auto w-full max-w-5xl px-4 py-10 md:py-14">
      <div className="rounded-3xl border border-stone-200 bg-white p-6 shadow-sm md:p-8">
        <div className="mb-6">
          <p className="text-sm font-semibold tracking-[0.2em] text-sky-700">
            ERROR
          </p>
          <h1 className="mt-3 text-3xl font-semibold text-slate-800 md:text-4xl">
            {code} {title}
          </h1>
          <p className="mt-3 text-sm leading-7 text-slate-500 md:text-base">
            {description}
          </p>
        </div>

        {(path || error || message || timestamp) && (
          <div className="rounded-2xl border border-stone-200 bg-stone-50 p-5 text-sm text-slate-700">
            <div className="space-y-2">
              {path && (
                <div>
                  <span className="font-semibold">Path:</span> {path}
                </div>
              )}
              <div>
                <span className="font-semibold">Status:</span> {code}
              </div>
              {error && (
                <div>
                  <span className="font-semibold">Error:</span> {error}
                </div>
              )}
              {message && (
                <div>
                  <span className="font-semibold">Message:</span> {message}
                </div>
              )}
              {timestamp && (
                <div>
                  <span className="font-semibold">Timestamp:</span> {timestamp}
                </div>
              )}
            </div>
          </div>
        )}

        {children && <div className="mt-6">{children}</div>}

        <div className="mt-6 flex flex-wrap gap-3">
          {primaryAction && (
            <Link
              href={primaryAction.href}
              className="rounded-xl bg-sky-700 px-5 py-3 text-sm font-medium text-white hover:opacity-90"
            >
              {primaryAction.label}
            </Link>
          )}

          {secondaryAction?.href && (
            <Link
              href={secondaryAction.href}
              className="rounded-xl border border-stone-300 px-5 py-3 text-sm font-medium text-slate-700 hover:border-sky-300 hover:text-sky-700"
            >
              {secondaryAction.label}
            </Link>
          )}

          {secondaryAction?.onClick === "back" && (
            <button
              type="button"
              onClick={() => window.history.back()}
              className="rounded-xl border border-stone-300 px-5 py-3 text-sm font-medium text-slate-700 hover:border-sky-300 hover:text-sky-700"
            >
              {secondaryAction.label}
            </button>
          )}

          {secondaryAction?.onClick === "reload" && (
            <button
              type="button"
              onClick={() => window.location.reload()}
              className="rounded-xl border border-stone-300 px-5 py-3 text-sm font-medium text-slate-700 hover:border-sky-300 hover:text-sky-700"
            >
              {secondaryAction.label}
            </button>
          )}
        </div>
      </div>
    </main>
  );
}