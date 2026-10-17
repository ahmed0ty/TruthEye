"use client";

import { useQuery } from "@tanstack/react-query";
import { RefreshCw, ShieldAlert, ShieldCheck } from "lucide-react";
import { EvidenceThumb } from "@/components/proctoring/evidence-thumb";
import { proctorApi } from "@/lib/api/proctor";
import { fmtDateTime } from "@/lib/format";
import { metaOf, riskOf, RISK_STYLE, SEVERITY_STYLE } from "@/lib/proctoring/events";
import { cn } from "@/lib/utils";

export function CheatTimeline({ sessionId }: { sessionId: number }) {
  const q = useQuery({ queryKey: ["proctor", "logs", sessionId], queryFn: () => proctorApi.sessionLogs(sessionId) });

  const logs = [...(q.data ?? [])].sort((a, b) => +new Date(a.timestamp) - +new Date(b.timestamp));
  const risk = riskOf(logs);
  const style = RISK_STYLE[risk.level];

  const counts = new Map<string, number>();
  logs.forEach((l) => counts.set(l.eventType, (counts.get(l.eventType) ?? 0) + 1));

  return (
    <section className="animate-fade-up rounded-2xl border border-slate-200 bg-white p-5">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <h3 className="flex items-center gap-2 font-bold text-[#0A2540]">
          {logs.length ? <ShieldAlert size={18} className="text-amber-500" /> : <ShieldCheck size={18} className="text-emerald-600" />}
          Proctoring timeline
        </h3>
        <span className={cn("rounded-full px-3 py-1 text-xs font-semibold", style.badge)}>
          {style.label} · {logs.length} {logs.length === 1 ? "event" : "events"}
        </span>
      </div>

      {q.isLoading ? (
        <div className="h-20 animate-pulse rounded-xl bg-slate-100" />
      ) : q.isError ? (
        <div className="flex flex-col items-center gap-3 py-4 text-center">
          <p className="text-sm text-red-500">{(q.error as Error).message}</p>
          <button
            onClick={() => q.refetch()}
            className="inline-flex items-center gap-1.5 rounded-lg bg-teal-600 px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-teal-700"
          >
            <RefreshCw size={13} /> Try again
          </button>
        </div>
      ) : logs.length === 0 ? (
        <p className="py-6 text-center text-sm text-slate-400">No suspicious activity was recorded for this attempt.</p>
      ) : (
        <>
          <div className="mb-5 flex flex-wrap gap-2">
            {[...counts.entries()].map(([type, n]) => {
              const m = metaOf(type);
              return (
                <span key={type} className={cn("rounded-full px-2.5 py-1 text-xs font-semibold", SEVERITY_STYLE[m.severity])}>
                  {m.label} × {n}
                </span>
              );
            })}
          </div>

          <ol className="space-y-4 border-s-2 border-slate-200 ps-5">
            {logs.map((l) => {
              const m = metaOf(l.eventType);
              return (
                <li key={l.id} className="relative flex items-center justify-between gap-4">
                  <span
                    className={cn(
                      "absolute -start-[1.7rem] h-3 w-3 rounded-full ring-4 ring-white",
                      m.severity === "high" ? "bg-red-500" : m.severity === "medium" ? "bg-amber-500" : "bg-slate-400"
                    )}
                  />
                  <div className="min-w-0">
                    <span className={cn("rounded-full px-2.5 py-1 text-xs font-semibold", SEVERITY_STYLE[m.severity])}>{m.label}</span>
                    <p className="mt-1 text-xs text-slate-400">
                      {fmtDateTime(l.timestamp)} · confidence {Math.round(l.confidence * 100)}%
                    </p>
                  </div>
                  <EvidenceThumb name={l.screenshotPath} />
                </li>
              );
            })}
          </ol>
        </>
      )}
    </section>
  );
}