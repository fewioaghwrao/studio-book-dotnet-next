"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { apiFetch } from "@/lib/apiFetch";

type AdminSettingsResponse = {
  taxRatePercent: number;
  adminFeeRatePercent: number;
};

type FormState = {
  taxRatePercent: string;
  adminFeeRatePercent: string;
};

const initialFormState: FormState = {
  taxRatePercent: "",
  adminFeeRatePercent: "",
};

export default function AdminSettingsPage() {
  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [form, setForm] = useState<FormState>(initialFormState);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  useEffect(() => {
    let ignore = false;

    const fetchSettings = async () => {
      setIsLoading(true);
      setErrorMessage("");

      try {
        const response = await apiFetch(`${apiBaseUrl}/api/admin/settings`, {
          method: "GET",          cache: "no-store",
        });

        if (response.status === 401) {
          window.location.href = "/auth/login";
          return;
        }

        if (response.status === 403) {
          setErrorMessage("管理者のみアクセスできます。");
          return;
        }

        if (!response.ok) {
          setErrorMessage("管理設定の取得に失敗しました。");
          return;
        }

        const data = (await response.json()) as AdminSettingsResponse;

        if (ignore) return;

        setForm({
          taxRatePercent: String(data.taxRatePercent ?? ""),
          adminFeeRatePercent: String(data.adminFeeRatePercent ?? ""),
        });
      } catch {
        if (!ignore) {
          setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
        }
      } finally {
        if (!ignore) {
          setIsLoading(false);
        }
      }
    };

    fetchSettings();

    return () => {
      ignore = true;
    };
  }, [apiBaseUrl]);

  const updateField = (key: keyof FormState, value: string) => {
    setForm((prev) => ({
      ...prev,
      [key]: value,
    }));
  };

  const validate = () => {
    const taxRate = Number(form.taxRatePercent);
    const adminFeeRate = Number(form.adminFeeRatePercent);

    if (form.taxRatePercent.trim() === "") {
      return "税率を入力してください。";
    }

    if (Number.isNaN(taxRate) || taxRate < 0 || taxRate > 100) {
      return "税率は0〜100の範囲で入力してください。";
    }

    if (form.adminFeeRatePercent.trim() === "") {
      return "手数料を入力してください。";
    }

    if (Number.isNaN(adminFeeRate) || adminFeeRate < 0 || adminFeeRate > 100) {
      return "手数料は0〜100の範囲で入力してください。";
    }

    return "";
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setErrorMessage("");
    setSuccessMessage("");

    const validationMessage = validate();
    if (validationMessage) {
      setErrorMessage(validationMessage);
      return;
    }

    try {
      setIsSaving(true);

      const response = await apiFetch(`${apiBaseUrl}/api/admin/settings`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },        body: JSON.stringify({
          taxRatePercent: Number(form.taxRatePercent),
          adminFeeRatePercent: Number(form.adminFeeRatePercent),
        }),
      });

      if (response.status === 401) {
        window.location.href = "/auth/login";
        return;
      }

      if (response.status === 403) {
        setErrorMessage("管理者のみアクセスできます。");
        return;
      }

      if (!response.ok) {
        let message = "管理設定の保存に失敗しました。";

        try {
          const data = (await response.json()) as { message?: string };
          if (data?.message) {
            message = data.message;
          }
        } catch {
          // 既定メッセージを使用
        }

        setErrorMessage(message);
        return;
      }

      const data = (await response.json()) as AdminSettingsResponse;

      setForm({
        taxRatePercent: String(data.taxRatePercent ?? ""),
        adminFeeRatePercent: String(data.adminFeeRatePercent ?? ""),
      });

      setSuccessMessage("管理設定を保存しました。");
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-12">
        <p className="text-sm text-slate-500">管理設定を読み込み中...</p>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-3xl px-4 py-6 sm:py-10">
      <nav className="mb-5 text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">管理設定</span>
      </nav>

      <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
        <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 text-center sm:px-8 sm:py-9">
          <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
            ADMIN SETTINGS
          </p>
          <h1 className="mt-3 text-xl font-semibold text-slate-800 sm:text-2xl md:text-3xl">
            管理設定
          </h1>
          <p className="mt-3 text-sm leading-6 text-slate-500">
            税率・手数料など、予約料金計算に使用する設定値を管理します。
          </p>
        </div>

        <div className="p-5 sm:p-8">

          {errorMessage && (
            <div className="mb-5 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
              {errorMessage}
            </div>
          )}

          {successMessage && (
            <div className="mb-5 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
              {successMessage}
            </div>
          )}

          <form className="space-y-5" onSubmit={handleSubmit} noValidate>
            <PercentInput
              label="税率"
              value={form.taxRatePercent}
              onChange={(value) => updateField("taxRatePercent", value)}
              placeholder="10"
            />

            <PercentInput
              label="手数料"
              value={form.adminFeeRatePercent}
              onChange={(value) => updateField("adminFeeRatePercent", value)}
              placeholder="15"
            />

            <div className="pt-2 text-sm text-slate-500">
              入力範囲：0〜100。DBには 10% → 0.10 のように小数として保存します。
            </div>

            <div className="flex flex-col-reverse gap-3 pt-4 sm:flex-row sm:justify-center">
              <Link
                href="/admin"
                className="inline-flex justify-center rounded-xl border border-stone-300 px-6 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
              >
                戻る
              </Link>

              <button
                type="submit"
                disabled={isSaving}
                className="inline-flex justify-center rounded-xl bg-sky-700 px-8 py-3 text-sm font-medium text-white transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isSaving ? "保存中..." : "保存"}
              </button>
            </div>
          </form>
        </div>
      </section>
    </div>
  );
}

function PercentInput({
  label,
  value,
  onChange,
  placeholder,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}) {
  return (
    <div className="grid gap-2 md:grid-cols-[160px_1fr] md:items-center">
      <label className="text-sm font-semibold text-slate-700">
        {label}
        <span className="ml-2 rounded-full bg-rose-100 px-2 py-0.5 text-xs font-medium text-rose-700">
          必須
        </span>
      </label>

      <div>
        <div className="flex overflow-hidden rounded-xl border border-stone-300 bg-white focus-within:border-sky-500">
          <input
            type="number"
            min="0"
            max="100"
            step="0.01"
            value={value}
            onChange={(e) => onChange(e.target.value)}
            placeholder={placeholder}
            className="w-full px-4 py-3 text-sm outline-none"
          />
          <span className="flex items-center border-l border-stone-200 bg-stone-50 px-4 text-sm font-medium text-slate-600">
            %
          </span>
        </div>
        <p className="mt-1 text-xs text-slate-500">入力範囲：0〜100</p>
      </div>
    </div>
  );
}