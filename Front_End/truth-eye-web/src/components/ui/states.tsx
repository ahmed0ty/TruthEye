"use client";

import { RefreshCw, TriangleAlert } from "lucide-react";

export function TableSkeleton({ rows = 5 }: { rows?: number }) {
  return (
    <div className="space-y-3 p-6">
      {Array.from({ length: rows }, (_, i) => (
        <div key={i} className="h-12 animate-pulse rounded-xl bg-slate-100" />
      ))}
    </div>
  );
}

export function CardGridSkeleton({ count = 3, height = "h-48" }: { count?: number; height?: string }) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
      {Array.from({ length: count }, (_, i) => (
        <div key={i} className={`${height} animate-pulse rounded-2xl bg-slate-100`} />
      ))}
    </div>
  );
}

export function ErrorState({
  message,
  onRetry,
  title = "Something went wrong",
}: {
  message?: string;
  onRetry?: () => void;
  title?: string;
}) {
  return (
    <div className="flex flex-col items-center rounded-2xl border border-dashed border-red-200 bg-white px-6 py-14 text-center">
      <div className="mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-red-50 text-red-500">
        <TriangleAlert size={26} />
      </div>
      <p className="font-bold text-[#0A2540]">{title}</p>
      {message && <p className="mt-1 max-w-sm text-sm text-slate-500">{message}</p>}
      {onRetry && (
        <button
          onClick={onRetry}
          className="mt-5 inline-flex items-center gap-2 rounded-xl bg-teal-600 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-700 active:scale-[0.98]"
        >
          <RefreshCw size={16} /> Try again
        </button>
      )}
    </div>
  );
}