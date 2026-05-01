export default function Loading() {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/20 backdrop-blur-[2px]">
      <div className="w-full max-w-md rounded-2xl border border-slate-200 bg-white shadow-xl">
        <div className="border-b border-slate-200 px-6 py-4">
          <div className="text-sm font-semibold text-slate-800">
            画面を読み込んでいます
          </div>
          <div className="mt-1 text-xs text-slate-500">
            データ取得中のため、このままお待ちください。
          </div>
        </div>

        <div className="px-6 py-6">
          <div className="flex items-center gap-4">
            <div className="relative h-12 w-12 shrink-0">
              <div className="absolute inset-0 rounded-full border-4 border-slate-200" />
              <div className="absolute inset-0 animate-spin rounded-full border-4 border-transparent border-t-sky-700 border-r-sky-700" />
            </div>

            <div className="min-w-0 flex-1">
              <div className="text-sm font-medium text-slate-700">
                読み込み中...
              </div>
              <div className="mt-2 h-2 w-full overflow-hidden rounded-full bg-slate-200">
                <div className="h-full w-1/3 animate-[loading-bar_1.2s_ease-in-out_infinite] rounded-full bg-sky-700" />
              </div>
              <div className="mt-2 text-xs text-slate-500">
                画面遷移または一覧データの準備を行っています
              </div>
            </div>
          </div>
        </div>

        <div className="border-t border-slate-200 bg-slate-50 px-6 py-3">
          <div className="text-xs text-slate-500">
            通信状況により数秒かかる場合があります
          </div>
        </div>
      </div>
    </div>
  );
}