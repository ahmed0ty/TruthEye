"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight, Radio, ShieldAlert, Sparkles, Users } from "lucide-react";
import { ChartCard, ChartEmpty, HBarChart } from "@/components/charts/charts";
import { proctorApi } from "@/lib/api/proctor";
import { useAuth } from "@/lib/auth/store";
import { fmtDateTime } from "@/lib/format";
import { metaOf, SEVERITY_STYLE } from "@/lib/proctoring/events";
import { cn } from "@/lib/utils";

// Minimum confidence for an event to count as "flagged" (phone detections usually score between 0.5 and 0.8)
const FLAGGED_MIN = 0.5;

export default function ProctorHome() {
  const user = useAuth((s) => s.user);
  const active = useQuery({ queryKey: ["proctor", "active"], queryFn: proctorApi.active, refetchInterval: 15000 });
  const flagged = useQuery({
    queryKey: ["proctor", "flagged", FLAGGED_MIN],
    queryFn: () => proctorApi.flagged(FLAGGED_MIN),
    refetchInterval: 30000,
  });

  if (!user) return null;

  const sessions = active.data ?? [];
  const exams = new Set(sessions.map((s) => s.examId)).size;
  const logs = [...(flagged.data ?? [])].sort((a, b) => +new Date(b.timestamp) - +new Date(a.timestamp));

  const hour = new Date().getHours();
  const greet = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";

  const stats = [
    { label: "Students testing now", value: sessions.length, icon: Users, tone: "bg-teal-50 text-teal-700" },
    { label: "Exams in progress", value: exams, icon: Radio, tone: "bg-sky-50 text-sky-700" },
    { label: "Flagged events", value: logs.length, icon: ShieldAlert, tone: "bg-red-50 text-red-700" },
  ];

  const counts = new Map<string, number>();
  logs.forEach((l) => {
    const label = metaOf(l.eventType).label;
    counts.set(label, (counts.get(label) ?? 0) + 1);
  });
  const chartData = [...counts.entries()]
    .map(([label, value]) => ({ label, value }))
    .sort((a, b) => b.value - a.value);

  return (
    <div className="space-y-8">
      <section className="animate-fade-up relative overflow-hidden rounded-3xl bg-[#0A2540] p-8 text-white sm:p-10">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 opacity-[0.07]"
          style={{
            backgroundImage: "linear-gradient(#fff 1px, transparent 1px), linear-gradient(90deg, #fff 1px, transparent 1px)",
            backgroundSize: "44px 44px",
          }}
        />
        <div className="pointer-events-none absolute -top-20 -end-10 h-72 w-72 rounded-full bg-teal-500/25 blur-3xl" />
        <div className="relative">
          <span className="inline-flex items-center gap-1.5 rounded-full border border-teal-400/30 bg-teal-400/10 px-3 py-1 text-xs font-semibold text-teal-200">
            <Sparkles size={14} /> Proctor Dashboard
          </span>
          <h1 className="mt-4 text-3xl font-bold sm:text-4xl">
            {greet}, {user.name.split(" ")[0]} 👋
          </h1>
          <p className="mt-2 max-w-xl text-slate-300">Watch live exams and review suspicious activity as it happens.</p>
          <Link
            href="/proctor/live"
            className="group mt-6 inline-flex items-center gap-2 rounded-xl bg-teal-500 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-400"
          >
            Open live monitoring <ArrowRight size={16} className="transition group-hover:translate-x-1" />
          </Link>
        </div>
      </section>

      <div className="grid gap-4 sm:grid-cols-3">
        {stats.map(({ label, value, icon: Icon, tone }, i) => (
          <div
            key={label}
            className="animate-fade-up rounded-2xl border border-slate-200 bg-white p-5 transition duration-300 hover:-translate-y-1 hover:shadow-lg hover:shadow-slate-200/70"
            style={{ animationDelay: `${i * 0.07}s` }}
          >
            <div className={cn("mb-4 flex h-11 w-11 items-center justify-center rounded-xl", tone)}>
              <Icon size={21} />
            </div>
            <p className="text-sm font-medium text-slate-500">{label}</p>
            {active.isLoading || flagged.isLoading ? (
              <div className="mt-2 h-8 w-14 animate-pulse rounded-lg bg-slate-100" />
            ) : (
              <p className="mt-1 text-3xl font-bold tracking-tight text-[#0A2540]">{value}</p>
            )}
          </div>
        ))}
      </div>

      <ChartCard title="Events by type">
        {chartData.length ? <HBarChart data={chartData} /> : <ChartEmpty text="No events yet" />}
      </ChartCard>

      <section className="animate-fade-up rounded-2xl border border-slate-200 bg-white" style={{ animationDelay: "0.3s" }}>
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-5">
          <h3 className="font-bold text-[#0A2540]">Latest flagged events</h3>
          <Link href="/proctor/alerts" className="text-sm font-semibold text-teal-700 hover:text-teal-800">View all</Link>
        </div>
        {logs.length === 0 ? (
          <p className="px-6 py-12 text-center text-sm text-slate-400">No flagged events yet.</p>
        ) : (
          <ul className="divide-y divide-slate-100">
            {logs.slice(0, 6).map((l) => {
              const m = metaOf(l.eventType);
              return (
                <li key={l.id} className="flex items-center justify-between gap-4 px-6 py-3.5">
                  <div className="flex items-center gap-3">
                    <span className={cn("rounded-full px-2.5 py-1 text-xs font-semibold", SEVERITY_STYLE[m.severity])}>{m.label}</span>
                    <span className="text-sm text-slate-500">Session #{l.examSessionId}</span>
                  </div>
                  <span className="text-xs text-slate-400">{fmtDateTime(l.timestamp)}</span>
                </li>
              );
            })}
          </ul>
        )}
      </section>
    </div>
  );
}