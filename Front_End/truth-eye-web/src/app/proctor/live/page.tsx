"use client";

import { useEffect, useMemo, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Activity, Clock, Radio, ShieldAlert, Trash2, Users, WifiOff } from "lucide-react";
import { EmptyState } from "@/components/ui/empty-state";
import { PageHeader } from "@/components/ui/page-header";
import { ErrorState } from "@/components/ui/states";
import { proctorApi } from "@/lib/api/proctor";
import { fmtDateTime, parseDate } from "@/lib/format";
import { metaOf, riskOf, RISK_STYLE, SEVERITY_STYLE } from "@/lib/proctoring/events";
import { useLiveMonitor, type HubStatus } from "@/lib/proctoring/use-live-monitor";
import { cn } from "@/lib/utils";
import type { ActiveSession } from "@/types/proctor";

const nameOf = (s?: ActiveSession, id?: number) => s?.studentName?.trim() || `Student #${s?.studentId ?? id ?? "?"}`;

function elapsed(startTime: string, now: number) {
  const start = parseDate(startTime);
  if (!start) return "—";
  const total = Math.max(0, Math.floor((now - +start) / 1000));
  const h = Math.floor(total / 3600);
  const m = Math.floor((total % 3600) / 60);
  const p = (n: number) => String(n).padStart(2, "0");
  return h ? `${h}:${p(m)}:${p(total % 60)}` : `${p(m)}:${p(total % 60)}`;
}

function StatusPill({ status }: { status: HubStatus }) {
  const map = {
    live: { text: "Live", cls: "bg-emerald-50 text-emerald-700", dot: "bg-emerald-500 animate-pulse" },
    connecting: { text: "Connecting...", cls: "bg-amber-50 text-amber-700", dot: "bg-amber-500 animate-pulse" },
    offline: { text: "Offline", cls: "bg-red-50 text-red-700", dot: "bg-red-500" },
  }[status];
  return (
    <span className={cn("inline-flex items-center gap-2 rounded-full px-3 py-1.5 text-xs font-semibold", map.cls)}>
      <span className={cn("h-2 w-2 rounded-full", map.dot)} /> {map.text}
    </span>
  );
}

function SessionCard({ s, now, flash }: { s: ActiveSession; now: number; flash: boolean }) {
  const logs = useQuery({
    queryKey: ["proctor", "logs", s.id],
    queryFn: () => proctorApi.sessionLogs(s.id),
    refetchInterval: 30000,
  });
  const list = logs.data ?? [];
  const risk = riskOf(list);
  const style = RISK_STYLE[risk.level];
  const last = [...list].sort((a, b) => +new Date(b.timestamp) - +new Date(a.timestamp))[0];

  return (
    <article
      className={cn(
        "rounded-2xl border-2 bg-white p-5 transition duration-300",
        style.ring,
        flash && "scale-[1.02] shadow-xl shadow-red-200/60"
      )}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-[#0A2540] text-sm font-bold text-teal-300">
            {nameOf(s).replace("Student #", "#").split(" ").map((p) => p[0]).slice(0, 2).join("")}
          </div>
          <div className="min-w-0">
            <p className="truncate font-bold text-[#0A2540]">{nameOf(s)}</p>
            <p className="truncate text-xs text-slate-500">{s.examTitle ?? `Exam #${s.examId}`}</p>
          </div>
        </div>
        <span className={cn("shrink-0 rounded-full px-2.5 py-1 text-[11px] font-semibold", style.badge)}>{style.label}</span>
      </div>

      <dl className="mt-4 grid grid-cols-2 gap-3 text-sm">
        <div className="rounded-xl bg-slate-50 p-3">
          <dt className="flex items-center gap-1.5 text-xs text-slate-400"><Clock size={13} /> Elapsed</dt>
          <dd className="mt-0.5 font-mono font-bold tabular-nums text-[#0A2540]">{elapsed(s.startTime, now)}</dd>
        </div>
        <div className="rounded-xl bg-slate-50 p-3">
          <dt className="flex items-center gap-1.5 text-xs text-slate-400"><ShieldAlert size={13} /> Events</dt>
          <dd className="mt-0.5 font-bold text-[#0A2540]">{logs.isLoading ? "…" : list.length}</dd>
        </div>
      </dl>

      <p className="mt-3 truncate text-xs text-slate-400">
        {last ? `Last: ${metaOf(last.eventType).label} · ${fmtDateTime(last.timestamp)}` : "No suspicious activity"}
      </p>
    </article>
  );
}

export default function LivePage() {
  const qc = useQueryClient();
  const [examFilter, setExamFilter] = useState<number | "all">("all");
  const [now, setNow] = useState(() => Date.now());
  const [flashId, setFlashId] = useState<number | null>(null);

  const active = useQuery({ queryKey: ["proctor", "active"], queryFn: proctorApi.active, refetchInterval: 15000 });
  const sessions = useMemo(() => active.data ?? [], [active.data]);
  const examIds = useMemo(() => [...new Set(sessions.map((s) => s.examId))], [sessions]);
  const live = useLiveMonitor(examIds);

  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);

  // A new live alert: refresh that student's counters and flash the card
  const latest = live.alerts[0];
  useEffect(() => {
    if (!latest) return;
    qc.invalidateQueries({ queryKey: ["proctor", "logs", latest.sessionId] });
    setFlashId(latest.sessionId);
    const t = setTimeout(() => setFlashId(null), 2500);
    return () => clearTimeout(t);
  }, [latest, qc]);

  const bySession = useMemo(() => new Map(sessions.map((s) => [s.id, s])), [sessions]);
  const visible = sessions
    .filter((s) => examFilter === "all" || s.examId === examFilter)
    .sort((a, b) => +new Date(b.startTime) - +new Date(a.startTime));

  const examOptions = examIds.map((id) => ({ id, title: sessions.find((s) => s.examId === id)?.examTitle ?? `Exam #${id}` }));

  return (
    <div className="space-y-6">
      <PageHeader
        title="Live Monitoring"
        subtitle="Students currently taking exams. New events appear instantly."
        action={<StatusPill status={live.status} />}
      />

      {live.status === "offline" && (
        <div className="flex items-start gap-3 rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          <WifiOff size={18} className="mt-0.5 shrink-0" />
          <p>Live connection is down. Cards still refresh every 30 seconds, but instant alerts are paused. Check that the backend is running.</p>
        </div>
      )}

      <div className="grid gap-6 xl:grid-cols-[1fr_380px]">
        <section className="space-y-4">
          <div className="flex flex-wrap items-center gap-2">
            <button
              onClick={() => setExamFilter("all")}
              className={cn(
                "rounded-full px-4 py-2 text-sm font-semibold transition",
                examFilter === "all" ? "bg-[#0A2540] text-white" : "bg-white text-slate-600 ring-1 ring-slate-200 hover:bg-slate-50"
              )}
            >
              All exams
            </button>
            {examOptions.map((e) => (
              <button
                key={e.id}
                onClick={() => setExamFilter(e.id)}
                className={cn(
                  "rounded-full px-4 py-2 text-sm font-semibold transition",
                  examFilter === e.id ? "bg-[#0A2540] text-white" : "bg-white text-slate-600 ring-1 ring-slate-200 hover:bg-slate-50"
                )}
              >
                {e.title}
              </button>
            ))}
            <span className="ms-auto flex items-center gap-1.5 text-sm text-slate-500">
              <Users size={16} /> {visible.length} testing
            </span>
          </div>

          {active.isLoading ? (
            <div className="grid gap-4 sm:grid-cols-2">{[0, 1, 2, 3].map((i) => <div key={i} className="h-48 animate-pulse rounded-2xl bg-slate-100" />)}</div>
          ) : active.isError ? (
            <ErrorState title="Couldn’t load sessions" message={(active.error as Error).message} onRetry={() => active.refetch()} />
          ) : visible.length === 0 ? (
            <EmptyState icon={Radio} title="No one is taking an exam right now" text="Students appear here as soon as they start an exam." />
          ) : (
            <div className="grid gap-4 sm:grid-cols-2">
              {visible.map((s) => <SessionCard key={s.id} s={s} now={now} flash={flashId === s.id} />)}
            </div>
          )}
        </section>

        <aside className="rounded-2xl border border-slate-200 bg-white xl:sticky xl:top-24 xl:self-start">
          <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
            <h3 className="flex items-center gap-2 font-bold text-[#0A2540]">
              <Activity size={18} className="text-teal-600" /> Live feed
            </h3>
            {live.alerts.length > 0 && (
              <button onClick={live.clear} className="flex items-center gap-1 text-xs font-semibold text-slate-400 transition hover:text-red-600">
                <Trash2 size={14} /> Clear
              </button>
            )}
          </div>

          {live.alerts.length === 0 ? (
            <p className="px-5 py-12 text-center text-sm text-slate-400">Waiting for events...</p>
          ) : (
            <ul className="max-h-[70vh] divide-y divide-slate-100 overflow-y-auto">
              {live.alerts.map((a) => {
                const m = metaOf(a.eventType);
                return (
                  <li key={a.key} className="animate-fade-in px-5 py-3.5">
                    <div className="flex items-center justify-between gap-2">
                      <span className={cn("rounded-full px-2.5 py-1 text-xs font-semibold", SEVERITY_STYLE[m.severity])}>{m.label}</span>
                      <span className="text-xs text-slate-400">{fmtDateTime(a.timestamp)}</span>
                    </div>
                    <p className="mt-1.5 text-sm font-semibold text-[#0A2540]">{nameOf(bySession.get(a.sessionId), a.sessionId)}</p>
                    <p className="text-xs text-slate-400">Session #{a.sessionId} · confidence {Math.round(a.confidence * 100)}%</p>
                  </li>
                );
              })}
            </ul>
          )}
        </aside>
      </div>
    </div>
  );
}