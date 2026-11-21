"use client";

import { ArrowRight } from "lucide-react";

function parse(json?: string | null): Record<string, unknown> | null {
  if (!json) return null;
  try {
    const v = JSON.parse(json);
    return v && typeof v === "object" && !Array.isArray(v) ? (v as Record<string, unknown>) : null;
  } catch {
    return null;
  }
}

const fmt = (v: unknown) =>
  v === null || v === undefined || v === "" ? "—" : typeof v === "boolean" ? (v ? "Yes" : "No") : String(v);

const label = (k: string) => k.replace(/([A-Z])/g, " $1").replace(/^./, (c) => c.toUpperCase());

export function ChangeDetails({ oldValues, newValues }: { oldValues?: string | null; newValues?: string | null }) {
  const before = parse(oldValues);
  const after = parse(newValues);

  if (!before && !after) return <p className="text-sm text-slate-500">No details were recorded.</p>;

  const keys = [...new Set([...Object.keys(before ?? {}), ...Object.keys(after ?? {})])];
  const rows = keys.map((k) => ({ k, a: before?.[k], b: after?.[k] }));
  const changedOnly = before && after ? rows.filter((r) => fmt(r.a) !== fmt(r.b)) : rows;

  if (changedOnly.length === 0) return <p className="text-sm text-slate-500">Nothing changed.</p>;

  return (
    <div className="space-y-2">
      {changedOnly.map((r) => (
        <div key={r.k} className="flex flex-wrap items-center gap-2 rounded-lg bg-slate-50 px-3 py-2 text-sm">
          <span className="w-40 shrink-0 font-semibold text-slate-600">{label(r.k)}</span>
          {before && (
            <span className={`rounded px-2 py-0.5 ${after ? "bg-red-50 text-red-700 line-through" : "bg-slate-100 text-slate-700"}`}>
              {fmt(r.a)}
            </span>
          )}
          {before && after && <ArrowRight size={14} className="text-slate-400" />}
          {after && <span className="rounded bg-emerald-50 px-2 py-0.5 text-emerald-700">{fmt(r.b)}</span>}
        </div>
      ))}
    </div>
  );
}