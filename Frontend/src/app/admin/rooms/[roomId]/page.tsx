"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useParams } from "next/navigation";

type AdminRoomDetail = {
  id: number;
  userId: number;
  hostName: string;
  name: string;
  imageName: string;
  description: string;
  price: number;
  capacity: number;
  postalCode: string;
  address: string;
};

function formatYen(value: number) {
  return new Intl.NumberFormat("ja-JP", {
    style: "currency",
    currency: "JPY",
    maximumFractionDigits: 0,
  }).format(value);
}

export default function AdminRoomDetailPage() {
  const params = useParams<{ roomId: string }>();

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const [room, setRoom] = useState<AdminRoomDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    let ignore = false;

    const fetchRoom = async () => {
      setIsLoading(true);
      setErrorMessage("");

      try {
        const response = await fetch(
          `${apiBaseUrl}/api/admin/rooms/${params.roomId}`,
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
          setErrorMessage("管理者のみアクセスできます。");
          return;
        }

        if (response.status === 404) {
          setErrorMessage("スタジオが見つかりません。");
          return;
        }

        if (!response.ok) {
          setErrorMessage("スタジオ基本情報の取得に失敗しました。");
          return;
        }

        const json = (await response.json()) as AdminRoomDetail;

        if (!ignore) {
          setRoom(json);
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

    fetchRoom();

    return () => {
      ignore = true;
    };
  }, [apiBaseUrl, params.roomId]);

  if (isLoading) {
    return (
      <div className="mx-auto max-w-5xl px-4 py-12">
        <p className="text-sm text-slate-500">スタジオ基本情報を読み込み中...</p>
      </div>
    );
  }

  if (errorMessage) {
    return (
      <div className="mx-auto max-w-5xl px-4 py-12">
        <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
          {errorMessage}
        </div>

        <div className="mt-6">
          <Link
            href="/admin/rooms"
            className="inline-flex rounded-xl border border-stone-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
          >
            スタジオ一覧へ戻る
          </Link>
        </div>
      </div>
    );
  }

  if (!room) {
    return null;
  }

  const imageSrc = room.imageName
    ? `/storage/${room.imageName}`
    : "/images/noImage.png";

  const rows = [
    { label: "ID", value: String(room.id) },
    { label: "スタジオ名", value: room.name },
    { label: "説明", value: room.description },
    { label: "基本料金", value: formatYen(room.price) },
    { label: "最大定員", value: `${room.capacity}人` },
    { label: "郵便番号", value: room.postalCode },
    { label: "住所", value: room.address },
    { label: "スタジオ提供者", value: room.hostName },
  ];

  return (
    <div className="mx-auto max-w-5xl px-4 py-6 sm:py-10">
      <nav className="mb-5 text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <Link href="/admin/rooms" className="hover:text-sky-700">
          スタジオ一覧
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">スタジオ基本情報</span>
      </nav>

      <section className="overflow-hidden rounded-[28px] border border-stone-200 bg-white shadow-sm">
        <div className="border-b border-stone-100 bg-gradient-to-br from-sky-50 via-white to-stone-50 px-5 py-7 text-center sm:px-8 sm:py-9">
          <p className="text-xs font-semibold tracking-[0.24em] text-sky-700 sm:text-sm">
            ADMIN ROOM DETAIL
          </p>
          <h1 className="mt-3 text-xl font-semibold text-slate-800 sm:text-2xl md:text-3xl">
            {room.name}
          </h1>
          <p className="mt-3 text-sm leading-6 text-slate-500">
            スタジオの基本情報を確認できます。
          </p>
        </div>

        <div className="p-5 sm:p-8">
          <div className="mb-6 flex justify-end">
            <Link
              href={`/admin/rooms/${room.id}/edit`}
              className="rounded-xl border border-stone-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
            >
              編集
            </Link>
          </div>

          <div className="mb-6 overflow-hidden rounded-2xl border border-stone-200 bg-stone-100">
            <img
              src={imageSrc}
              alt={room.name || "スタジオ画像"}
              className="h-72 w-full object-cover"
            />
          </div>

          <div className="divide-y divide-stone-200 rounded-2xl border border-stone-200 bg-white">
            {rows.map((row) => (
              <div
                key={row.label}
                className="grid gap-2 px-4 py-4 md:grid-cols-[180px_1fr] md:px-5"
              >
                <div className="text-sm font-semibold text-slate-700">
                  {row.label}
                </div>
                <div className="whitespace-pre-wrap break-words text-sm leading-6 text-slate-700">
                  {row.value || "未登録"}
                </div>
              </div>
            ))}
          </div>

          <div className="mt-8 flex justify-center">
            <Link
              href="/admin/rooms"
              className="inline-flex justify-center rounded-xl border border-stone-300 px-6 py-3 text-sm font-medium text-slate-700 transition hover:border-sky-300 hover:text-sky-700"
            >
              スタジオ一覧へ戻る
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}