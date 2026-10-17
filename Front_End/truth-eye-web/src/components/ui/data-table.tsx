"use client";

import { useMemo, useState } from "react";
import { ArrowDown, ArrowUp, ArrowUpDown, ChevronLeft, ChevronRight, Inbox, Search } from "lucide-react";
import { ErrorState, TableSkeleton } from "./states";
import { cn } from "@/lib/utils";

type Sortable = string | number | null | undefined;

export interface Column<T> {
  header: string;
  cell: (row: T) => React.ReactNode;
  className?: string;
  /** Provide this to make the column sortable */
  sortValue?: (row: T) => Sortable;
}

const SIZES = [10, 25, 50];

function compare(a: Sortable, b: Sortable) {
  if (a == null && b == null) return 0;
  if (a == null) return 1;
  if (b == null) return -1;
  if (typeof a === "number" && typeof b === "number") return a - b;
  return String(a).localeCompare(String(b), undefined, { numeric: true, sensitivity: "base" });
}

export function DataTable<T extends { id: number }>({
  rows,
  columns,
  loading,
  error,
  onRetry,
  searchText,
  actions,
  toolbar,
  placeholder = "Search...",
  pageSize = 10,
}: {
  rows: T[];
  columns: Column<T>[];
  loading?: boolean;
  error?: string;
  onRetry?: () => void;
  searchText: (row: T) => string;
  actions?: (row: T) => React.ReactNode;
  toolbar?: React.ReactNode;
  placeholder?: string;
  pageSize?: number;
}) {
  const [q, setQ] = useState("");
  const [sort, setSort] = useState<{ key: string; dir: "asc" | "desc" } | null>(null);
  const [page, setPage] = useState(1);
  const [size, setSize] = useState(pageSize);

  const filtered = useMemo(() => {
    const s = q.trim().toLowerCase();
    return s ? rows.filter((r) => searchText(r).toLowerCase().includes(s)) : rows;
  }, [rows, q, searchText]);

  const sorted = useMemo(() => {
    const col = sort && columns.find((c) => c.header === sort.key);
    if (!col?.sortValue) return filtered;
    const dir = sort!.dir === "asc" ? 1 : -1;
    return [...filtered].sort((a, b) => dir * compare(col.sortValue!(a), col.sortValue!(b)));
  }, [filtered, sort, columns]);

  const pages = Math.max(1, Math.ceil(sorted.length / size));
  const current = Math.min(page, pages);
  const start = (current - 1) * size;
  const visible = sorted.slice(start, start + size);

  const toggleSort = (key: string) => {
    setPage(1);
    setSort((s) => (s?.key !== key ? { key, dir: "asc" } : s.dir === "asc" ? { key, dir: "desc" } : null));
  };

  return (
    <div className="animate-fade-up overflow-hidden rounded-2xl border border-slate-200 bg-white">
      <div className="flex flex-wrap items-center gap-3 border-b border-slate-100 p-4">
        <div className="relative w-full max-w-sm">
          <Search size={18} className="pointer-events-none absolute inset-y-0 start-3.5 my-auto text-slate-400" />
          <input
            value={q}
            onChange={(e) => {
              setQ(e.target.value);
              setPage(1);
            }}
            placeholder={placeholder}
            className="w-full rounded-xl border border-slate-200 bg-slate-50 py-2.5 ps-10 pe-4 text-sm outline-none transition focus:border-teal-600 focus:bg-white focus:ring-4 focus:ring-teal-100"
          />
        </div>
        {toolbar}
        <span className="ms-auto shrink-0 rounded-full bg-teal-50 px-3 py-1 text-xs font-semibold text-teal-700">
          {sorted.length} {sorted.length === 1 ? "record" : "records"}
        </span>
      </div>

      {loading ? (
        <TableSkeleton />
      ) : error ? (
        <div className="p-6">
          <ErrorState message={error} onRetry={onRetry} title="Couldn’t load the data" />
        </div>
      ) : sorted.length === 0 ? (
        <div className="flex flex-col items-center px-6 py-16 text-center">
                    <div className="relative mb-4 flex h-24 w-24 items-center justify-center">
            <span aria-hidden className="absolute inset-0 rounded-full border-2 border-dashed border-teal-200 motion-safe:animate-[spin_24s_linear_infinite]" />
            <span aria-hidden className="absolute inset-3 rounded-full bg-teal-50" />
            <Inbox size={30} className="relative text-teal-600" />
          </div>
          <p className="font-bold text-slate-700">{q || toolbar ? "No results found" : "Nothing here yet"}</p>
          <p className="mt-1 text-sm text-slate-400">
            {q ? "Try a different search term or filter." : "Add the first record to get started."}
          </p>
        </div>
      ) : (
        <>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[640px] text-sm">
              <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-400">
                <tr>
                  {columns.map((c) => {
                    const active = sort?.key === c.header;
                    return (
                      <th key={c.header} className="px-5 py-3 text-start font-semibold">
                        {c.sortValue ? (
                          <button
                            onClick={() => toggleSort(c.header)}
                            className={cn("inline-flex items-center gap-1 uppercase tracking-wide transition hover:text-slate-700", active && "text-teal-700")}
                          >
                            {c.header}
                            {active ? sort!.dir === "asc" ? <ArrowUp size={13} /> : <ArrowDown size={13} /> : <ArrowUpDown size={13} className="opacity-40" />}
                          </button>
                        ) : (
                          c.header
                        )}
                      </th>
                    );
                  })}
                  {actions && <th className="px-5 py-3" />}
                </tr>
              </thead>
              <tbody>
                              {visible.map((r, i) => (
                  <tr
                    key={r.id}
                    className="animate-row-in border-t border-slate-100 transition hover:bg-slate-50/70"
                    style={{ animationDelay: `${Math.min(i, 12) * 28}ms` }}
                  >
                    {columns.map((c) => (
                      <td key={c.header} className={`px-5 py-3.5 ${c.className ?? ""}`}>{c.cell(r)}</td>
                    ))}
                    {actions && <td className="px-5 py-3.5 text-end">{actions(r)}</td>}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {sorted.length > SIZES[0] && (
            <div className="flex flex-wrap items-center justify-between gap-3 border-t border-slate-100 px-5 py-3 text-sm text-slate-500">
              <div className="flex items-center gap-2">
                <span>Rows per page</span>
                <select
                  value={size}
                  onChange={(e) => {
                    setSize(Number(e.target.value));
                    setPage(1);
                  }}
                  className="rounded-lg border border-slate-200 bg-white px-2 py-1 text-sm outline-none focus:border-teal-600"
                >
                  {SIZES.map((n) => <option key={n} value={n}>{n}</option>)}
                </select>
              </div>
              <div className="flex items-center gap-3">
                <span>
                  {start + 1}–{Math.min(start + size, sorted.length)} of {sorted.length}
                </span>
                <div className="flex gap-1">
                  <button
                    onClick={() => setPage(current - 1)}
                    disabled={current <= 1}
                    className="rounded-lg border border-slate-200 p-1.5 transition hover:bg-slate-50 disabled:opacity-40"
                    aria-label="Previous page"
                  >
                    <ChevronLeft size={16} />
                  </button>
                  <button
                    onClick={() => setPage(current + 1)}
                    disabled={current >= pages}
                    className="rounded-lg border border-slate-200 p-1.5 transition hover:bg-slate-50 disabled:opacity-40"
                    aria-label="Next page"
                  >
                    <ChevronRight size={16} />
                  </button>
                </div>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}

export function PersonCell({ first, last, email }: { first: string; last: string; email?: string }) {
  return (
    <div className="flex items-center gap-3">
      <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-[#0A2540] text-xs font-bold text-teal-300">
        {first?.[0]}{last?.[0]}
      </div>
      <div>
        <p className="font-bold text-slate-800">{first} {last}</p>
        {email && <p className="text-xs text-slate-400" dir="ltr">{email}</p>}
      </div>
    </div>
  );
}

export function StatusBadge({ active }: { active: boolean }) {
  return (
    <span className={`rounded-full px-2.5 py-1 text-xs font-bold ${active ? "bg-emerald-50 text-emerald-600" : "bg-slate-100 text-slate-500"}`}>
      {active ? "Active" : "Inactive"}
    </span>
  );
}