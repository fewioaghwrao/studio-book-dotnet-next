"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";

type HostOption = {
  id: number;
  name: string;
  email: string;
};

type RoomFormState = {
  userId: string;
  name: string;
  imageName: string;
  description: string;
  price: string;
  capacity: string;
  postalCode: string;
  address: string;
};

const initialFormState: RoomFormState = {
  userId: "",
  name: "",
  imageName: "room01.jpg",
  description: "",
  price: "",
  capacity: "",
  postalCode: "",
  address: "",
};

const imageOptions = Array.from({ length: 24 }, (_, index) => {
  const no = String(index + 1).padStart(2, "0");
  return `room${no}.jpg`;
});

export default function AdminRoomRegisterPage() {
  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [form, setForm] = useState<RoomFormState>(initialFormState);
  const [hosts, setHosts] = useState<HostOption[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  useEffect(() => {
    let ignore = false;

    const fetchHosts = async () => {
      setIsLoading(true);
      setErrorMessage("");

      try {
        const response = await fetch(`${apiBaseUrl}/api/admin/rooms/host-options`, {
          method: "GET",
          credentials: "include",
          cache: "no-store",
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
          setErrorMessage("スタジオ提供者の取得に失敗しました。");
          return;
        }

        const json = (await response.json()) as HostOption[];

        if (!ignore) {
          setHosts(json);
        }
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

    fetchHosts();

    return () => {
      ignore = true;
    };
  }, [apiBaseUrl]);

  const updateField = (key: keyof RoomFormState, value: string) => {
    setForm((prev) => ({
      ...prev,
      [key]: value,
    }));
  };

  const validate = () => {
    if (!form.userId) return "スタジオ提供者を選択してください。";
    if (!form.name.trim()) return "スタジオ名を入力してください。";
    if (!form.price.trim()) return "基本料金を入力してください。";
    if (Number(form.price) <= 0) return "基本料金は1円以上で入力してください。";
    if (!form.capacity.trim()) return "最大定員を入力してください。";
    if (Number(form.capacity) <= 0) return "最大定員は1人以上で入力してください。";
    if (!form.postalCode.trim()) return "郵便番号を入力してください。";
    if (!form.address.trim()) return "住所を入力してください。";

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

      const response = await fetch(`${apiBaseUrl}/api/admin/rooms`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        credentials: "include",
        body: JSON.stringify({
          userId: Number(form.userId),
          name: form.name,
          imageName: form.imageName,
          description: form.description,
          price: Number(form.price),
          capacity: Number(form.capacity),
          postalCode: form.postalCode,
          address: form.address,
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
        let message = "スタジオの登録に失敗しました。";

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

      setSuccessMessage("スタジオを登録しました。");

      setTimeout(() => {
        window.location.href = "/admin/rooms";
      }, 600);
    } catch {
      setErrorMessage("通信エラーが発生しました。時間をおいて再度お試しください。");
    } finally {
      setIsSaving(false);
    }
  };

  const imageSrc = form.imageName
    ? `/storage/${form.imageName}`
    : "/images/noImage.png";

  if (isLoading) {
    return (
      <div className="mx-auto max-w-4xl px-4 py-12">
        <p className="text-sm text-slate-500">登録画面を読み込み中...</p>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-4xl px-4 py-6 sm:py-10">
      <nav className="mb-5 text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <Link href="/admin/rooms" className="hover:text-sky-700">
          スタジオ一覧
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">スタジオ登録</span>
      </nav>

      <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
        <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 text-center sm:px-8 sm:py-9">
          <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
            ADMIN ROOM REGISTER
          </p>
          <h1 className="mt-3 text-xl font-semibold text-slate-800 sm:text-2xl md:text-3xl">
            スタジオ登録
          </h1>
          <p className="mt-3 text-sm leading-6 text-slate-500">
            管理者としてスタジオの基本情報を登録できます。
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

          <RoomPreview imageSrc={imageSrc} name={form.name} />

<div className="mt-4 rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-700">
  公開デモでは画像アップロードの代わりに、あらかじめ用意したサンプル画像を選択する形式にしています。
</div>

          <form className="mt-6 space-y-5" onSubmit={handleSubmit}>
            <RoomFormFields
              form={form}
              hosts={hosts}
              onChange={updateField}
            />

            <div className="flex flex-col-reverse gap-3 pt-4 sm:flex-row sm:justify-center">
              <Link
                href="/admin/rooms"
                className="inline-flex justify-center rounded-xl border border-stone-300 px-6 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
              >
                戻る
              </Link>

              <button
                type="submit"
                disabled={isSaving}
                className="inline-flex justify-center rounded-xl bg-sky-700 px-8 py-3 text-sm font-medium text-white transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isSaving ? "登録中..." : "登録"}
              </button>
            </div>
          </form>
        </div>
      </section>
    </div>
  );
}

function RoomPreview({ imageSrc, name }: { imageSrc: string; name: string }) {
  return (
    <div className="overflow-hidden rounded-2xl border border-stone-200 bg-stone-100">
      <img
        src={imageSrc}
        alt={name || "スタジオ画像"}
        className="h-64 w-full object-cover"
      />
    </div>
  );
}

function RoomFormFields({
  form,
  hosts,
  onChange,
}: {
  form: RoomFormState;
  hosts: HostOption[];
  onChange: (key: keyof RoomFormState, value: string) => void;
}) {
  return (
    <>
      <TextInput
        label="スタジオ名"
        required
        value={form.name}
        onChange={(value) => onChange("name", value)}
        placeholder="秋葉原ダンススタジオ A"
      />

      <SelectInput
        label="スタジオ画像"
        value={form.imageName}
        onChange={(value) => onChange("imageName", value)}
        options={imageOptions.map((name) => ({
          value: name,
          label: name,
        }))}
      />

      <TextAreaInput
        label="説明"
        value={form.description}
        onChange={(value) => onChange("description", value)}
        placeholder="駅近で使いやすい多目的スタジオです。"
      />

      <TextInput
        label="基本料金"
        required
        type="number"
        value={form.price}
        onChange={(value) => onChange("price", value)}
        placeholder="3000"
      />

      <TextInput
        label="最大定員"
        required
        type="number"
        value={form.capacity}
        onChange={(value) => onChange("capacity", value)}
        placeholder="6"
      />

      <TextInput
        label="郵便番号"
        required
        value={form.postalCode}
        onChange={(value) => onChange("postalCode", value)}
        placeholder="101-0022"
      />

      <TextInput
        label="住所"
        required
        value={form.address}
        onChange={(value) => onChange("address", value)}
        placeholder="東京都千代田区神田練塀町300番地"
      />

      <SelectInput
        label="スタジオ提供者"
        required
        value={form.userId}
        onChange={(value) => onChange("userId", value)}
        options={[
          { value: "", label: "-- 選択してください --" },
          ...hosts.map((host) => ({
            value: String(host.id),
            label: `${host.name}（${host.email}）`,
          })),
        ]}
      />
    </>
  );
}

function RequiredBadge() {
  return (
    <span className="ml-2 rounded-full bg-rose-100 px-2 py-0.5 text-xs font-medium text-rose-700">
      必須
    </span>
  );
}

function TextInput({
  label,
  value,
  onChange,
  placeholder,
  type = "text",
  required = false,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  type?: string;
  required?: boolean;
}) {
  return (
    <div className="grid gap-2 md:grid-cols-[180px_1fr] md:items-center">
      <label className="text-sm font-semibold text-slate-700">
        {label}
        {required && <RequiredBadge />}
      </label>
      <input
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="w-full rounded-xl border border-stone-300 px-4 py-3 text-sm outline-none transition focus:border-sky-500"
      />
    </div>
  );
}

function TextAreaInput({
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
    <div className="grid gap-2 md:grid-cols-[180px_1fr]">
      <label className="text-sm font-semibold text-slate-700">{label}</label>
      <textarea
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        rows={5}
        className="w-full rounded-xl border border-stone-300 px-4 py-3 text-sm outline-none transition focus:border-sky-500"
      />
    </div>
  );
}

function SelectInput({
  label,
  value,
  onChange,
  options,
  required = false,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: { value: string; label: string }[];
  required?: boolean;
}) {
  return (
    <div className="grid gap-2 md:grid-cols-[180px_1fr] md:items-center">
      <label className="text-sm font-semibold text-slate-700">
        {label}
        {required && <RequiredBadge />}
      </label>
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="w-full rounded-xl border border-stone-300 bg-white px-4 py-3 text-sm outline-none transition focus:border-sky-500"
      >
        {options.map((option) => (
          <option key={`${option.value}-${option.label}`} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </div>
  );
}