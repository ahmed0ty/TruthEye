"use client";

import { useEffect, useState } from "react";

export interface Datum {
  label: string;
  value: number;
}

export function ChartCard({ title, children, className = "" }: { title: string; children: React.ReactNode; className?: string }) {
  return (
    <section className={`animate-fade-up rounded-2xl border border-slate-200 bg-white p-5 ${className}`}>
      <h3 className="mb-4 font-bold text-[#0A2540]">{title}</h3>
      {children}
    </section>
  );
}

export function ChartEmpty({ text = "No data yet" }: { text?: string }) {
  return <p className="flex h-40 items-center justify-center text-center text-sm text-slate-400">{text}</p>;
}

export function ColumnChart({ data, height = 190, suffix = "" }: { data: Datum[]; height?: number; suffix?: string }) {
  const max = Math.max(1, ...data.map((d) => d.value));
  return (
    <div className="flex gap-2.5" style={{ height }}>
      {data.map((d, i) => (
        <div key={d.label} className="flex min-w-0 flex-1 flex-col items-center gap-1.5">
          <span className="text-xs font-semibold text-slate-600">{d.value}{suffix}</span>
          <div className="relative w-full flex-1">
            <div
              className="grow-up absolute inset-x-0 bottom-0 rounded-t-lg bg-gradient-to-t from-teal-600 to-teal-400"
              style={{ height: `${(d.value / max) * 100}%`, minHeight: d.value ? 4 : 2, animationDelay: `${i * 0.06}s` }}
            />
          </div>
          <span className="w-full truncate text-center text-[11px] text-slate-400" title={d.label}>{d.label}</span>
        </div>
      ))}
    </div>
  );
}

export function HBarChart({ data, suffix = "", max: fixedMax }: { data: Datum[]; suffix?: string; max?: number }) {
  const max = fixedMax ?? Math.max(1, ...data.map((d) => d.value));
  return (
    <ul className="space-y-3">
      {data.map((d, i) => (
        <li key={d.label}>
          <div className="mb-1 flex items-center justify-between gap-3 text-xs">
            <span className="truncate font-medium text-slate-600" title={d.label}>{d.label}</span>
            <span className="shrink-0 font-semibold text-[#0A2540]">{d.value}{suffix}</span>
          </div>
          <div className="h-2.5 overflow-hidden rounded-full bg-slate-100">
            <div
              className="grow-x h-full rounded-full bg-gradient-to-r from-teal-600 to-teal-400"
              style={{ width: `${Math.min(100, (d.value / max) * 100)}%`, animationDelay: `${i * 0.06}s` }}
            />
          </div>
        </li>
      ))}
    </ul>
  );
}

export function Donut({ value, total, label, color = "#0d9488" }: { value: number; total: number; label: string; color?: string }) {
  const R = 52;
  const C = 2 * Math.PI * R;
  const pct = total ? value / total : 0;
  const [p, setP] = useState(0);
  useEffect(() => {
    const t = setTimeout(() => setP(pct), 100);
    return () => clearTimeout(t);
  }, [pct]);

  return (
    <div className="flex flex-col items-center gap-3">
      <div className="relative h-40 w-40">
        <svg viewBox="0 0 128 128" className="h-full w-full -rotate-90">
          <circle cx="64" cy="64" r={R} fill="none" strokeWidth="12" stroke="currentColor" className="text-slate-100" />
          <circle
            cx="64" cy="64" r={R} fill="none" strokeWidth="12" strokeLinecap="round" stroke={color}
            style={{ strokeDasharray: C, strokeDashoffset: C * (1 - p), transition: "stroke-dashoffset 1s cubic-bezier(0.22,1,0.36,1)" }}
          />
        </svg>
        <div className="absolute inset-0 flex flex-col items-center justify-center">
          <span className="text-3xl font-extrabold text-[#0A2540]">{total ? Math.round(pct * 100) : 0}%</span>
          <span className="text-xs text-slate-400">{label}</span>
        </div>
      </div>
      <p className="text-xs text-slate-500">{value} of {total}</p>
    </div>
  );
}