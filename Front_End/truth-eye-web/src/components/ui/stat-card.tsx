"use client";

import { useEffect, useRef, useState, type PointerEvent, type ReactNode } from "react";
import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";

/** Counts from the previous value up to `value` (respects reduced motion) */
export function CountUp({
  value,
  decimals = 0,
  suffix = "",
  duration = 900,
}: {
  value: number;
  decimals?: number;
  suffix?: string;
  duration?: number;
}) {
  const [display, setDisplay] = useState(0);
  const from = useRef(0);

  useEffect(() => {
    const reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const total = reduced ? 0 : duration;
    const startVal = from.current;
    const start = performance.now();
    let raf = 0;

    const tick = (now: number) => {
      const t = total === 0 ? 1 : Math.min(1, (now - start) / total);
      const eased = 1 - Math.pow(1 - t, 3); // ease-out
      const v = startVal + (value - startVal) * eased;
      from.current = v;
      setDisplay(v);
      if (t < 1) raf = requestAnimationFrame(tick);
    };

    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [value, duration]);

  return (
    <span className="tabular-nums">
      {display.toFixed(decimals)}
      {suffix}
    </span>
  );
}

/** A card with a soft teal light that follows the pointer */
export function SpotlightCard({ children, className }: { children: ReactNode; className?: string }) {
  const ref = useRef<HTMLDivElement>(null);

  const onMove = (e: PointerEvent<HTMLDivElement>) => {
    const el = ref.current;
    if (!el) return;
    const r = el.getBoundingClientRect();
    el.style.setProperty("--mx", `${e.clientX - r.left}px`);
    el.style.setProperty("--my", `${e.clientY - r.top}px`);
  };

  return (
    <div
      ref={ref}
      onPointerMove={onMove}
      className={cn(
        "group relative overflow-hidden rounded-2xl border border-slate-200 bg-white transition duration-300 hover:-translate-y-0.5 hover:shadow-lg hover:shadow-teal-900/5 motion-reduce:transition-none motion-reduce:hover:translate-y-0",
        className
      )}
    >
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-0 transition-opacity duration-300 group-hover:opacity-100"
        style={{
          background:
            "radial-gradient(260px circle at var(--mx, 50%) var(--my, 50%), rgba(20,184,166,.13), transparent 70%)",
        }}
      />
      <div className="relative">{children}</div>
    </div>
  );
}

export function StatCard({
  label,
  value,
  decimals = 0,
  suffix = "",
  icon: Icon,
  tone,
  loading,
  delay = 0,
}: {
  label: string;
  value: number;
  decimals?: number;
  suffix?: string;
  icon: LucideIcon;
  tone: string;
  loading?: boolean;
  delay?: number;
}) {
  return (
    <div className="animate-fade-up" style={{ animationDelay: `${delay}s` }}>
      <SpotlightCard className="p-5">
        <div className={cn("mb-3 flex h-11 w-11 items-center justify-center rounded-xl", tone)}>
          <Icon size={21} />
        </div>
        <p className="text-sm font-medium text-slate-500">{label}</p>
        {loading ? (
          <div className="mt-2 h-8 w-14 animate-pulse rounded-lg bg-slate-100" />
        ) : (
          <p className="mt-1 text-3xl font-bold text-[#0A2540]">
            <CountUp value={value} decimals={decimals} suffix={suffix} />
          </p>
        )}
      </SpotlightCard>
    </div>
  );
}