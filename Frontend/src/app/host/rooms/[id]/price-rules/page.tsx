"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { useParams } from "next/navigation";

type PriceRule = {
  id: number;
  ruleType: string;
  weekday: number | null;
  startHour: string | null;
  endHour: string | null;
  multiplier: number | null;
  flatFee: number | null;
  note: string | null;
};

type PriceRulesResponse = {
  roomId: number;
  roomName: string;
  rules: PriceRule[];
};

type RuleType = "" | "multiplier" | "flat_fee";

const weekdayOptions = [
  { label: "全て", value: "" },
  { label: "日", value: "0" },
  { label: "月", value: "1" },
  { label: "火", value: "2" },
  { label: "水", value: "3" },
  { label: "木", value: "4" },
  { label: "金", value: "5" },
  { label: "土", value: "6" },
];

const weekdayLabels: Record<number, string> = {
  0: "日",
  1: "月",
  2: "火",
  3: "水",
  4: "木",
  5: "金",
  6: "土",
};

function createTimeOptions() {
  const options: string[] = [];

  for (let h = 0; h < 24; h++) {
    for (let m = 0; m < 60; m += 15) {
      options.push(`${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}`);
    }
  }

  return options;
}

function normalizeTime(value: string | null) {
  if (!value) return null;
  return value.slice(0, 5);
}

function formatRuleType(value: string) {
  if (value === "multiplier") return "倍率";
  if (value === "flat_fee") return "固定費";
  return value;
}

function formatWeekday(value: number | null) {
  if (value === null || value === undefined) return "全て";
  return `${weekdayLabels[value] ?? "-"}曜`;
}

export default function HostPriceRulesPage() {
  const params = useParams<{ id: string }>();

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const timeOptions = useMemo(() => createTimeOptions(), []);

  const [roomName, setRoomName] = useState("");
  const [rules, setRules] = useState<PriceRule[]>([]);

  const [ruleType, setRuleType] = useState<RuleType>("");
  const [weekday, setWeekday] = useState("");
  const [startHour, setStartHour] = useState("");
  const [endHour, setEndHour] = useState("");
  const [multiplier, setMultiplier] = useState("");
  const [flatFee, setFlatFee] = useState("");
  const [note, setNote] = useState("");

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [deletingId, setDeletingId] = useState<number | null>(null);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  const fetchRules = async () => {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const response = await fetch(
        `${apiBaseUrl}/api/host/rooms/${params.id}/price-rules`,
        {
          method: "GET",
          credentials: "include",
          cache: "no-store",
        }
      );

      if (response.status === 401) {
        window.location.href = "/auth/login";
        return;
      }

      if (response.status === 403) {
        setErrorMessage("ホストユーザーのみアクセスできます。");
        return;
      }

      if (response.status === 404) {
        setErrorMessage("スタジオが見つかりません。");
        return;
      }

      if (!response.ok) {
        setErrorMessage("料金ルールの取得に失敗しました。");
        return;
      }

      const data = (await response.json()) as PriceRulesResponse;

      setRoomName(data.roomName);
      setRules(
        data.rules.map((rule) => ({
          ...rule,
          startHour: normalizeTime(rule.startHour),
          endHour: normalizeTime(rule.endHour),
        }))
      );
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (params.id) {
      fetchRules();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [apiBaseUrl, params.id]);

  const clearForm = () => {
    setRuleType("");
    setWeekday("");
    setStartHour("");
    setEndHour("");
    setMultiplier("");
    setFlatFee("");
    setNote("");
  };

  const validate = () => {
    if (!ruleType) {
      return "タイプを選択してください。";
    }

    if (ruleType === "multiplier") {
      if (!startHour || !endHour) {
        return "倍率ルールでは開始時刻・終了時刻を選択してください。";
      }

      if (!multiplier) {
        return "倍率を入力してください。";
      }

      if (Number(multiplier) <= 0) {
        return "倍率は0より大きい値を入力してください。";
      }

      if (flatFee) {
        return "倍率の場合、固定費は入力できません。";
      }
    }

    if (ruleType === "flat_fee") {
      if (!flatFee) {
        return "固定費を入力してください。";
      }

      if (Number(flatFee) < 0) {
        return "固定費は0以上で入力してください。";
      }

      if (startHour || endHour || multiplier) {
        return "固定費の場合、開始/終了時刻と倍率は入力できません。";
      }
    }

    return "";
  };

  const handleRuleTypeChange = (value: RuleType) => {
    setRuleType(value);
    setErrorMessage("");
    setSuccessMessage("");

    if (value === "multiplier") {
      setFlatFee("");
    } else if (value === "flat_fee") {
      setStartHour("");
      setEndHour("");
      setMultiplier("");
    } else {
      setStartHour("");
      setEndHour("");
      setMultiplier("");
      setFlatFee("");
    }
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    const validationMessage = validate();

    if (validationMessage) {
      setErrorMessage(validationMessage);
      setSuccessMessage("");
      return;
    }

    setIsSaving(true);
    setErrorMessage("");
    setSuccessMessage("");

    try {
      const response = await fetch(
        `${apiBaseUrl}/api/host/rooms/${params.id}/price-rules`,
        {
          method: "POST",
          credentials: "include",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            ruleType,
            weekday: weekday === "" ? null : Number(weekday),
            startHour: startHour || null,
            endHour: endHour || null,
            multiplier: multiplier ? Number(multiplier) : null,
            flatFee: flatFee ? Number(flatFee) : null,
            note: note.trim() || null,
          }),
        }
      );

      if (response.status === 401) {
        window.location.href = "/auth/login";
        return;
      }

      if (!response.ok) {
        const data = await response.json().catch(() => null);
        setErrorMessage(data?.message ?? "料金ルールの追加に失敗しました。");
        return;
      }

      clearForm();
      setSuccessMessage("料金ルールを追加しました。");
      await fetchRules();
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = async (ruleId: number) => {
    const ok = window.confirm("この料金ルールを削除します。よろしいですか？");

    if (!ok) return;

    setDeletingId(ruleId);
    setErrorMessage("");
    setSuccessMessage("");

    try {
      const response = await fetch(
        `${apiBaseUrl}/api/host/rooms/${params.id}/price-rules/${ruleId}`,
        {
          method: "DELETE",
          credentials: "include",
        }
      );

      if (response.status === 401) {
        window.location.href = "/auth/login";
        return;
      }

      if (!response.ok) {
        const data = await response.json().catch(() => null);
        setErrorMessage(data?.message ?? "料金ルールの削除に失敗しました。");
        return;
      }

      setSuccessMessage("料金ルールを削除しました。");
      await fetchRules();
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    } finally {
      setDeletingId(null);
    }
  };

  if (isLoading) {
    return (
      <div className="mx-auto max-w-6xl px-4 py-12">
        <div className="rounded-[28px] border border-stone-200 bg-white p-8 shadow-sm">
          <p className="text-sm text-slate-500">料金ルールを読み込み中...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-6xl px-4 py-6 sm:py-10">
      <nav className="mb-5 overflow-x-auto whitespace-nowrap text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <Link href="/host/rooms" className="hover:text-sky-700">
          スタジオ一覧
        </Link>
        <span className="mx-2">&gt;</span>
        <Link href={`/host/rooms/${params.id}`} className="hover:text-sky-700">
          スタジオ詳細
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">料金ルール設定</span>
      </nav>

      <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
        <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 sm:px-8 sm:py-9">
          <div className="mx-auto max-w-3xl text-center">
            <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
              PRICE RULES
            </p>

            <h1 className="mt-3 text-xl font-semibold text-slate-800 sm:text-2xl md:text-3xl">
              {roomName ? `${roomName} の料金ルール設定` : "料金ルール設定"}
            </h1>

            <p className="mt-3 text-sm leading-6 text-slate-500">
              曜日や時間帯に応じた倍率、または曜日単位の固定費を設定できます。
            </p>
          </div>
        </div>

        <div className="px-5 py-5 sm:px-8 sm:py-7">
          {errorMessage && (
            <div className="mb-5 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
              {errorMessage}
            </div>
          )}

          {successMessage && (
            <div className="mb-5 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
              {successMessage}
            </div>
          )}

          <form
            onSubmit={handleSubmit}
            noValidate
            className="rounded-3xl border border-stone-200 bg-stone-50 p-4 sm:p-6"
          >
            <div className="mb-5">
              <h2 className="text-lg font-semibold text-slate-800">
                料金ルールを追加
              </h2>
              <p className="mt-1 text-sm text-slate-500">
                倍率は時間帯指定、固定費は曜日単位の追加料金として登録します。
              </p>
            </div>

            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  タイプ
                </label>
                <select
                  value={ruleType}
                  onChange={(e) => handleRuleTypeChange(e.target.value as RuleType)}
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                >
                  <option value="">選択</option>
                  <option value="multiplier">倍率</option>
                  <option value="flat_fee">固定費</option>
                </select>
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  曜日
                </label>
                <select
                  value={weekday}
                  onChange={(e) => setWeekday(e.target.value)}
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                >
                  {weekdayOptions.map((option) => (
                    <option key={option.label} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  開始時刻
                </label>
                <select
                  value={startHour}
                  disabled={ruleType !== "multiplier"}
                  onChange={(e) => setStartHour(e.target.value)}
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500 disabled:bg-stone-100 disabled:text-slate-400"
                >
                  <option value="">-- 選択 --</option>
                  {timeOptions.map((time) => (
                    <option key={time} value={time}>
                      {time}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  終了時刻
                </label>
                <select
                  value={endHour}
                  disabled={ruleType !== "multiplier"}
                  onChange={(e) => setEndHour(e.target.value)}
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500 disabled:bg-stone-100 disabled:text-slate-400"
                >
                  <option value="">-- 選択 --</option>
                  {timeOptions.map((time) => (
                    <option key={time} value={time}>
                      {time}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  倍率
                </label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  value={multiplier}
                  disabled={ruleType !== "multiplier"}
                  onChange={(e) => setMultiplier(e.target.value)}
                  placeholder="例 1.50"
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500 disabled:bg-stone-100 disabled:text-slate-400"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  固定費
                </label>
                <input
                  type="number"
                  step="1"
                  min="0"
                  value={flatFee}
                  disabled={ruleType !== "flat_fee"}
                  onChange={(e) => setFlatFee(e.target.value)}
                  placeholder="例 2000"
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500 disabled:bg-stone-100 disabled:text-slate-400"
                />
              </div>

              <div className="md:col-span-2">
                <label className="mb-1 block text-xs font-semibold text-slate-500">
                  メモ
                </label>
                <input
                  type="text"
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                  placeholder="例 土日料金、夜間料金など"
                  className="w-full rounded-xl border border-stone-300 bg-white px-3 py-3 text-sm text-slate-700 outline-none transition focus:border-sky-500"
                />
              </div>
            </div>

            <div className="mt-6 flex justify-end">
              <button
                type="submit"
                disabled={isSaving}
                className="w-full rounded-xl bg-sky-700 px-6 py-3 text-sm font-medium text-white shadow-sm transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50 sm:w-auto"
              >
                {isSaving ? "追加中..." : "追加する"}
              </button>
            </div>
          </form>

          <div className="mt-8">
            <div className="mb-4 flex flex-col gap-1 sm:flex-row sm:items-end sm:justify-between">
              <div>
                <h2 className="text-lg font-semibold text-slate-800">
                  現在の設定
                </h2>
                <p className="mt-1 text-sm text-slate-500">
                  保存済みの料金ルール一覧です。
                </p>
              </div>

              <p className="text-sm text-slate-500">全 {rules.length} 件</p>
            </div>

            {rules.length === 0 ? (
              <div className="rounded-2xl border border-dashed border-stone-300 bg-stone-50 px-4 py-10 text-center">
                <p className="text-sm text-slate-500">
                  保存済みのルールはありません。
                </p>
              </div>
            ) : (
              <>
                <div className="grid gap-4 md:hidden">
                  {rules.map((rule) => (
                    <article
                      key={rule.id}
                      className="rounded-2xl border border-stone-200 bg-white p-4 shadow-sm"
                    >
                      <div className="mb-4 flex items-start justify-between gap-3">
                        <div>
                          <p className="text-xs font-medium tracking-[0.18em] text-sky-700">
                            RULE #{rule.id}
                          </p>
                          <h3 className="mt-1 text-lg font-semibold text-slate-800">
                            {formatRuleType(rule.ruleType)}
                          </h3>
                        </div>

                        <span className="rounded-full bg-sky-50 px-3 py-1 text-xs font-medium text-sky-700">
                          {formatWeekday(rule.weekday)}
                        </span>
                      </div>

                      <dl className="grid grid-cols-2 gap-3 text-sm">
                        <div>
                          <dt className="text-xs font-medium text-slate-400">
                            開始
                          </dt>
                          <dd className="mt-1 text-slate-700">
                            {rule.startHour ?? "-"}
                          </dd>
                        </div>
                        <div>
                          <dt className="text-xs font-medium text-slate-400">
                            終了
                          </dt>
                          <dd className="mt-1 text-slate-700">
                            {rule.endHour ?? "-"}
                          </dd>
                        </div>
                        <div>
                          <dt className="text-xs font-medium text-slate-400">
                            倍率
                          </dt>
                          <dd className="mt-1 text-slate-700">
                            {rule.multiplier ?? "-"}
                          </dd>
                        </div>
                        <div>
                          <dt className="text-xs font-medium text-slate-400">
                            固定費
                          </dt>
                          <dd className="mt-1 text-slate-700">
                            {rule.flatFee != null
                              ? `${rule.flatFee.toLocaleString()}円`
                              : "-"}
                          </dd>
                        </div>
                        <div className="col-span-2">
                          <dt className="text-xs font-medium text-slate-400">
                            メモ
                          </dt>
                          <dd className="mt-1 text-slate-700">
                            {rule.note || "-"}
                          </dd>
                        </div>
                      </dl>

                      <button
                        type="button"
                        onClick={() => handleDelete(rule.id)}
                        disabled={deletingId === rule.id}
                        className="mt-4 w-full rounded-xl border border-rose-200 px-4 py-3 text-sm font-medium text-rose-700 transition hover:bg-rose-50 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        {deletingId === rule.id ? "削除中..." : "削除"}
                      </button>
                    </article>
                  ))}
                </div>

                <div className="hidden overflow-x-auto md:block">
                  <table className="w-full min-w-[900px] border-separate border-spacing-y-3">
                    <thead>
                      <tr className="text-left text-xs font-semibold tracking-wide text-slate-500">
                        <th className="px-4 py-2">タイプ</th>
                        <th className="px-4 py-2">曜日</th>
                        <th className="px-4 py-2">開始</th>
                        <th className="px-4 py-2">終了</th>
                        <th className="px-4 py-2">倍率</th>
                        <th className="px-4 py-2">固定費</th>
                        <th className="px-4 py-2">メモ</th>
                        <th className="px-4 py-2">操作</th>
                      </tr>
                    </thead>

                    <tbody>
                      {rules.map((rule) => (
                        <tr key={rule.id} className="bg-stone-50">
                          <td className="rounded-l-2xl px-4 py-4 text-sm font-semibold text-slate-700">
                            {formatRuleType(rule.ruleType)}
                          </td>
                          <td className="px-4 py-4 text-sm text-slate-700">
                            {formatWeekday(rule.weekday)}
                          </td>
                          <td className="px-4 py-4 text-sm text-slate-700">
                            {rule.startHour ?? "-"}
                          </td>
                          <td className="px-4 py-4 text-sm text-slate-700">
                            {rule.endHour ?? "-"}
                          </td>
                          <td className="px-4 py-4 text-sm text-slate-700">
                            {rule.multiplier ?? "-"}
                          </td>
                          <td className="px-4 py-4 text-sm text-slate-700">
                            {rule.flatFee != null
                              ? `${rule.flatFee.toLocaleString()}円`
                              : "-"}
                          </td>
                          <td className="px-4 py-4 text-sm text-slate-700">
                            {rule.note || "-"}
                          </td>
                          <td className="rounded-r-2xl px-4 py-4">
                            <button
                              type="button"
                              onClick={() => handleDelete(rule.id)}
                              disabled={deletingId === rule.id}
                              className="rounded-xl border border-rose-200 px-4 py-2 text-sm font-medium text-rose-700 transition hover:bg-rose-50 disabled:cursor-not-allowed disabled:opacity-50"
                            >
                              {deletingId === rule.id ? "削除中..." : "削除"}
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </>
            )}
          </div>
        </div>

        <div className="border-t border-stone-100 bg-white/95 px-5 py-4 sm:px-8">
          <div className="flex justify-end">
            <Link
              href={`/host/rooms/${params.id}`}
              className="w-full rounded-xl border border-stone-300 px-5 py-3 text-center text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700 sm:w-auto"
            >
              詳細へ戻る
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}