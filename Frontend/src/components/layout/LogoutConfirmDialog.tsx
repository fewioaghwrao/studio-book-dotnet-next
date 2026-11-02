"use client";

import { useEffect, useId, useRef } from "react";
import { createPortal } from "react-dom";

type LogoutConfirmDialogProps = {
  open: boolean;
  /** ログイン中のユーザー名（表示用、任意） */
  userName?: string;
  /** 管理者・スタジオ提供者などの表示名（任意） */
  roleLabel?: string;
  /** ログアウト処理中は操作をロックし、ボタンに処理中表示を出す */
  isProcessing?: boolean;
  onCancel: () => void;
  onConfirm: () => void;
};

/**
 * ログアウト確認ダイアログ。
 * - 画面中央のモーダル（背景はぼかし＋半透明）
 * - 初期フォーカスは安全側の「キャンセル」。Esc・背景クリックで閉じる（処理中は閉じない）
 * - Tab フォーカスはダイアログ内で循環し、閉じたら元の要素へ戻す
 * - ヘッダーの backdrop-filter の影響を受けないよう body 直下へポータル描画する
 */
export default function LogoutConfirmDialog({
  open,
  userName,
  roleLabel,
  isProcessing = false,
  onCancel,
  onConfirm,
}: LogoutConfirmDialogProps) {
  const titleId = useId();
  const descriptionId = useId();
  const panelRef = useRef<HTMLDivElement | null>(null);
  const cancelButtonRef = useRef<HTMLButtonElement | null>(null);

  // 最新の props をイベントハンドラから参照するための ref
  const stateRef = useRef({ isProcessing, onCancel });
  useEffect(() => {
    stateRef.current = { isProcessing, onCancel };
  });

  useEffect(() => {
    if (!open) return;

    const previouslyFocused = document.activeElement as HTMLElement | null;
    const previousOverflow = document.body.style.overflow;

    document.body.style.overflow = "hidden";
    cancelButtonRef.current?.focus();

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        if (!stateRef.current.isProcessing) {
          event.preventDefault();
          stateRef.current.onCancel();
        }
        return;
      }

      if (event.key !== "Tab" || !panelRef.current) return;

      const focusable = Array.from(
        panelRef.current.querySelectorAll<HTMLElement>("button:not([disabled])")
      );

      if (focusable.length === 0) {
        event.preventDefault();
        return;
      }

      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      const active = document.activeElement;

      if (event.shiftKey && (active === first || !panelRef.current.contains(active))) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && (active === last || !panelRef.current.contains(active))) {
        event.preventDefault();
        first.focus();
      }
    };

    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("keydown", handleKeyDown);
      document.body.style.overflow = previousOverflow;
      previouslyFocused?.focus?.();
    };
  }, [open]);

  if (!open || typeof document === "undefined") {
    return null;
  }

  const handleBackdropClick = () => {
    if (!isProcessing) {
      onCancel();
    }
  };

  return createPortal(
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div
        data-testid="logout-dialog-backdrop"
        aria-hidden="true"
        onClick={handleBackdropClick}
        className="sb-dialog-backdrop absolute inset-0 bg-slate-900/40 backdrop-blur-sm"
      />

      <div
        ref={panelRef}
        role="alertdialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={descriptionId}
        className="sb-dialog-panel relative w-full max-w-sm overflow-hidden rounded-3xl border border-stone-200/80 bg-white shadow-2xl shadow-slate-900/20"
      >
        <div className="px-6 pb-5 pt-7 text-center">
          <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-rose-50 ring-8 ring-rose-50/50">
            <svg
              className="h-7 w-7 text-rose-600"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth={1.8}
              strokeLinecap="round"
              strokeLinejoin="round"
              aria-hidden="true"
            >
              <path d="M15 4h3a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-3" />
              <path d="M10 17l-5-5 5-5" />
              <path d="M5 12h11" />
            </svg>
          </div>

          <h2 id={titleId} className="mt-5 text-lg font-semibold text-slate-800">
            ログアウトしますか？
          </h2>

          <div id={descriptionId} className="mt-2 text-sm leading-6 text-slate-500">
            {(userName || roleLabel) && (
              <p className="mb-3 inline-flex max-w-full items-center gap-2 rounded-full bg-stone-100 px-3 py-1 text-xs font-medium text-slate-600">
                {roleLabel && (
                  <span className="rounded-full bg-white px-2 py-0.5 text-[11px] font-semibold text-sky-700">
                    {roleLabel}
                  </span>
                )}
                {userName && <span className="truncate">{userName} さん</span>}
              </p>
            )}
            {/* 語の途中で折り返さないよう keep-all にし、文節の区切りにだけ改行候補（wbr）を置く */}
            <p className="[word-break:keep-all]">
              現在のセッションを<wbr />終了します。
              <br />
              引き続き利用するには、<wbr />再度ログインが<wbr />必要です。
            </p>
          </div>
        </div>

        <div className="flex flex-col-reverse gap-2 border-t border-stone-100 bg-stone-50/70 px-6 py-4 sm:flex-row sm:justify-end">
          <button
            ref={cancelButtonRef}
            type="button"
            onClick={onCancel}
            disabled={isProcessing}
            className="inline-flex items-center justify-center rounded-xl border border-stone-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:border-stone-400 hover:bg-stone-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-sky-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60"
          >
            キャンセル
          </button>

          <button
            type="button"
            onClick={onConfirm}
            disabled={isProcessing}
            className="inline-flex items-center justify-center gap-2 rounded-xl bg-rose-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-rose-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-rose-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-70"
          >
            {isProcessing && (
              <svg
                className="h-4 w-4 animate-spin"
                viewBox="0 0 24 24"
                fill="none"
                aria-hidden="true"
              >
                <circle cx="12" cy="12" r="9" stroke="currentColor" strokeOpacity="0.3" strokeWidth="3" />
                <path d="M21 12a9 9 0 0 0-9-9" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
              </svg>
            )}
            {isProcessing ? "ログアウト中..." : "ログアウトする"}
          </button>
        </div>
      </div>
    </div>,
    document.body
  );
}
