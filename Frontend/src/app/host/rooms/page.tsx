"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";

type HostRoom = {
  id: number;
  name: string;
  postalCode: string;
  address: string;
};

type PagedResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export default function HostRoomsPage() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const apiBaseUrl = useMemo(() => {
    return process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7226";
  }, []);

  const currentKeyword = searchParams.get("keyword") ?? "";
  const currentPage = Number(searchParams.get("page") ?? "1");

  const [keyword, setKeyword] = useState(currentKeyword);
  const [rooms, setRooms] = useState<HostRoom[]>([]);
  const [pageInfo, setPageInfo] = useState({
    page: currentPage,
    pageSize: 10,
    totalCount: 0,
    totalPages: 1,
  });
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    setKeyword(currentKeyword);
  }, [currentKeyword]);

  useEffect(() => {
    let ignore = false;

    const fetchRooms = async () => {
      setIsLoading(true);
      setErrorMessage("");

      try {
        const params = new URLSearchParams();
        params.set("page", String(currentPage > 0 ? currentPage : 1));
        params.set("pageSize", "10");

        if (currentKeyword.trim()) {
          params.set("keyword", currentKeyword.trim());
        }

        const response = await fetch(`${apiBaseUrl}/api/host/rooms?${params.toString()}`, {
          method: "GET",
          credentials: "include",
          cache: "no-store",
        });

        if (response.status === 401) {
          window.location.href = "/auth/login";
          return;
        }

        if (response.status === 403) {
          setErrorMessage("ホストユーザーのみアクセスできます。");
          return;
        }

        if (!response.ok) {
          setErrorMessage("スタジオ一覧の取得に失敗しました。");
          return;
        }

        const data = (await response.json()) as PagedResponse<HostRoom>;

        if (ignore) return;

        setRooms(data.items ?? []);
        setPageInfo({
          page: data.page,
          pageSize: data.pageSize,
          totalCount: data.totalCount,
          totalPages: data.totalPages,
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
    

    fetchRooms();

    return () => {
      ignore = true;
    };
  }, [apiBaseUrl, currentKeyword, currentPage]);

  const [userName, setUserName] = useState("");

useEffect(() => {
  let ignore = false;

  const fetchCurrentUser = async () => {
    try {
      const response = await fetch(`${apiBaseUrl}/api/auth/me`, {
        method: "GET",
        credentials: "include",
        cache: "no-store",
      });

      if (!response.ok) {
        return;
      }

      const data = await response.json();

      if (!ignore) {
        setUserName(data.user?.name ?? "");
      }
    } catch (error) {
      console.error("ユーザー情報取得エラー:", error);
    }
  };

  fetchCurrentUser();

  return () => {
    ignore = true;
  };
}, [apiBaseUrl]);
useEffect(() => {
  setKeyword(currentKeyword);
}, [currentKeyword]);

useEffect(() => {
  let ignore = false;

  const fetchRooms = async () => {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const params = new URLSearchParams();
      params.set("page", String(currentPage > 0 ? currentPage : 1));
      params.set("pageSize", "10");

      if (currentKeyword.trim()) {
        params.set("keyword", currentKeyword.trim());
      }

      const response = await fetch(`${apiBaseUrl}/api/host/rooms?${params.toString()}`, {
        method: "GET",
        credentials: "include",
        cache: "no-store",
      });

      if (response.status === 401) {
        window.location.href = "/auth/login";
        return;
      }

      if (response.status === 403) {
        setErrorMessage("ホストユーザーのみアクセスできます。");
        return;
      }

      if (!response.ok) {
        setErrorMessage("スタジオ一覧の取得に失敗しました。");
        return;
      }

      const data = (await response.json()) as PagedResponse<HostRoom>;

      if (ignore) return;

      setRooms(data.items ?? []);
      setPageInfo({
        page: data.page,
        pageSize: data.pageSize,
        totalCount: data.totalCount,
        totalPages: data.totalPages,
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

  fetchRooms();

  return () => {
    ignore = true;
  };
}, [apiBaseUrl, currentKeyword, currentPage]);

  const handleSearch = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    const params = new URLSearchParams();
    if (keyword.trim()) {
      params.set("keyword", keyword.trim());
    }
    params.set("page", "1");

    router.push(`/host/rooms?${params.toString()}`);
  };

  const movePage = (page: number) => {
    const params = new URLSearchParams();

    if (currentKeyword.trim()) {
      params.set("keyword", currentKeyword.trim());
    }

    params.set("page", String(page));
    router.push(`/host/rooms?${params.toString()}`);
  };

  return (
    <div className="mx-auto max-w-6xl px-4 py-10">
      <nav className="mb-6 text-sm text-slate-500">
        <Link href="/" className="hover:text-sky-700">
          ホーム
        </Link>
        <span className="mx-2">&gt;</span>
        <span className="text-slate-700">スタジオ一覧（ホスト）</span>
      </nav>

      <section className="rounded-[28px] border border-stone-200 bg-white p-6 shadow-sm md:p-8">
        <div className="mb-6 text-center">
          <p className="text-sm font-medium tracking-[0.2em] text-sky-700">
            HOST ROOMS
          </p>
<h1 className="mt-2 text-2xl font-semibold text-slate-800">
  {userName ? `${userName}様のスタジオ一覧` : "スタジオ一覧"}
</h1>
          <p className="mt-2 text-sm text-slate-500">
            登録済みスタジオの確認、詳細表示、各種設定画面へ移動できます。
          </p>
        </div>

        <div className="mb-6 flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
          <form onSubmit={handleSearch} className="flex w-full gap-2 md:max-w-md">
            <input
              type="text"
              value={keyword}
              onChange={(e) => setKeyword(e.target.value)}
              placeholder="スタジオ名"
              className="w-full rounded-xl border border-stone-300 px-4 py-3 text-sm outline-none transition focus:border-sky-500"
            />
            <button
              type="submit"
              className="rounded-xl bg-sky-700 px-5 py-3 text-sm font-medium text-white transition hover:opacity-90"
            >
              検索
            </button>
          </form>

          <div className="text-sm text-slate-500">
            全 {pageInfo.totalCount} 件
          </div>
        </div>

        {errorMessage && (
          <div className="mb-5 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
            {errorMessage}
          </div>
        )}

        {isLoading ? (
          <p className="text-sm text-slate-500">スタジオ一覧を読み込み中...</p>
        ) : rooms.length === 0 ? (
          <div className="rounded-2xl border border-dashed border-stone-300 bg-stone-50 px-4 py-10 text-center">
            <p className="text-sm text-slate-500">
              該当するスタジオがありません。
            </p>
          </div>
        ) : (
          <>
 <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
  {rooms.map((room) => (
    <article
      key={room.id}
      className="rounded-2xl border border-stone-200 bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md"
    >
      <div className="mb-4 flex items-start justify-between gap-3">
        <div>
          <p className="text-xs font-medium tracking-[0.18em] text-sky-700">
            ROOM #{room.id}
          </p>
          <h2 className="mt-1 text-lg font-semibold text-slate-800">
            {room.name}
          </h2>
        </div>

        <span className="rounded-full bg-sky-50 px-3 py-1 text-xs font-medium text-sky-700">
          ホスト管理
        </span>
      </div>

      <div className="space-y-3 text-sm">
        <div>
          <p className="text-xs font-medium text-slate-400">郵便番号</p>
          <p className="mt-1 text-slate-700">{room.postalCode}</p>
        </div>

        <div>
          <p className="text-xs font-medium text-slate-400">住所</p>
          <p className="mt-1 leading-relaxed text-slate-700">
            {room.address}
          </p>
        </div>
      </div>

      <div className="mt-5 grid grid-cols-1 gap-2 sm:grid-cols-2">
        <Link
          href={`/host/rooms/${room.id}/closures`}
          className="rounded-xl border border-sky-200 px-3 py-2 text-center text-xs font-medium text-sky-700 transition hover:bg-sky-50"
        >
          休館日設定
        </Link>

        <Link
          href={`/host/rooms/${room.id}/business-hours`}
          className="rounded-xl border border-sky-200 px-3 py-2 text-center text-xs font-medium text-sky-700 transition hover:bg-sky-50"
        >
          営業時間設定
        </Link>

        <Link
          href={`/host/rooms/${room.id}/price-rules`}
          className="rounded-xl border border-sky-200 px-3 py-2 text-center text-xs font-medium text-sky-700 transition hover:bg-sky-50"
        >
          料金ルール設定
        </Link>

        <Link
          href={`/host/rooms/${room.id}`}
          className="rounded-xl bg-slate-800 px-3 py-2 text-center text-xs font-medium text-white transition hover:opacity-90"
        >
          詳細
        </Link>
      </div>
    </article>
  ))}
</div>
            {pageInfo.totalPages > 1 && (
              <div className="mt-8 flex items-center justify-center gap-2">
                <button
                  type="button"
                  disabled={pageInfo.page <= 1}
                  onClick={() => movePage(pageInfo.page - 1)}
                  className="rounded-lg border border-stone-300 px-4 py-2 text-sm text-slate-700 disabled:cursor-not-allowed disabled:opacity-40"
                >
                  前
                </button>

                {Array.from({ length: pageInfo.totalPages }, (_, index) => {
                  const page = index + 1;
                  const isActive = page === pageInfo.page;

                  return (
                    <button
                      key={page}
                      type="button"
                      onClick={() => movePage(page)}
                      className={
                        isActive
                          ? "rounded-lg bg-sky-700 px-4 py-2 text-sm font-medium text-white"
                          : "rounded-lg border border-stone-300 px-4 py-2 text-sm text-slate-700 hover:border-sky-300 hover:text-sky-700"
                      }
                    >
                      {page}
                    </button>
                  );
                })}

                <button
                  type="button"
                  disabled={pageInfo.page >= pageInfo.totalPages}
                  onClick={() => movePage(pageInfo.page + 1)}
                  className="rounded-lg border border-stone-300 px-4 py-2 text-sm text-slate-700 disabled:cursor-not-allowed disabled:opacity-40"
                >
                  次
                </button>
              </div>
            )}
          </>
        )}
      </section>
    </div>
  );
}