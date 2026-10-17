"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight, CheckCircle2, Clock, Inbox, Sparkles, UserCheck } from "lucide-react";
import { ticketsApi } from "@/lib/api/tickets";
import { useAuth } from "@/lib/auth/store";
import { fmtDateTime } from "@/lib/format";
import { PRIORITY_STYLE, STATUS_STYLE, statusLabel } from "@/lib/ticket-meta";
import { cn } from "@/lib/utils";

export default function SupportHome() {
  const user = useAuth((s) => s.user);
  const all = useQuery({ queryKey: ["tickets", "all"], queryFn: ticketsApi.all });
  const assigned = useQuery({ queryKey: ["tickets", "assigned"], queryFn: ticketsApi.assigned });

  if (!user) return null;

  const tickets = all.data ?? [];
  const open = tickets.filter((t) => t.status === "Open").length;
  const inProgress = tickets.filter((t) => t.status === "In Progress").length;
  const resolved = tickets.filter((t) => t.status === "Resolved" || t.status === "Closed").length;
  const mine = assigned.data?.length ?? 0;
  const latest = [...tickets].sort((a, b) => b.id - a.id).slice(0, 6);

  const hour = new Date().getHours();
  const greet = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
  const loading = all.isLoading || assigned.isLoading;

  const stats = [
    { label: "Open", value: open, icon: Inbox, tone: "bg-amber-50 text-amber-700" },
    { label: "In progress", value: inProgress, icon: Clock, tone: "bg-sky-50 text-sky-700" },
    { label: "Resolved", value: resolved, icon: CheckCircle2, tone: "bg-emerald-50 text-emerald-700" },
    { label: "Assigned to me", value: mine, icon: UserCheck, tone: "bg-teal-50 text-teal-700" },
  ];

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
            <Sparkles size={14} /> Support Dashboard
          </span>
          <h1 className="mt-4 text-3xl font-bold sm:text-4xl">
            {greet}, {user.name.split(" ")[0]} 👋
          </h1>
          <p className="mt-2 max-w-xl text-slate-300">Here is where your support queue stands right now.</p>
          <Link
            href="/support/tickets"
            className="group mt-6 inline-flex items-center gap-2 rounded-xl bg-teal-500 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-400"
          >
            Open tickets <ArrowRight size={16} className="transition group-hover:translate-x-1" />
          </Link>
        </div>
      </section>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
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
            {loading ? (
              <div className="mt-2 h-8 w-14 animate-pulse rounded-lg bg-slate-100" />
            ) : (
              <p className="mt-1 text-3xl font-bold tracking-tight text-[#0A2540]">{value}</p>
            )}
          </div>
        ))}
      </div>

      <section className="animate-fade-up rounded-2xl border border-slate-200 bg-white" style={{ animationDelay: "0.3s" }}>
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-5">
          <h3 className="font-bold text-[#0A2540]">Latest tickets</h3>
          <Link href="/support/tickets" className="text-sm font-semibold text-teal-700 hover:text-teal-800">
            View all
          </Link>
        </div>
        {latest.length === 0 ? (
          <p className="px-6 py-12 text-center text-sm text-slate-400">No tickets yet.</p>
        ) : (
          <ul className="divide-y divide-slate-100">
            {latest.map((t) => (
              <li key={t.id} className="flex items-center justify-between gap-4 px-6 py-3.5">
                <div className="min-w-0">
                  <p className="truncate font-semibold text-[#0A2540]">{t.subject}</p>
                  <p className="text-xs text-slate-400">
                    {t.ticketNumber} · {t.userName ?? "—"} · {t.createdAt ? fmtDateTime(t.createdAt) : "—"}
                  </p>
                </div>
                <div className="flex shrink-0 items-center gap-2">
                  <span className={cn("rounded-full px-2.5 py-1 text-xs font-semibold", PRIORITY_STYLE[t.priority] ?? "bg-slate-100")}>
                    {t.priority}
                  </span>
                  <span className={cn("rounded-full px-2.5 py-1 text-xs font-semibold", STATUS_STYLE[t.status] ?? "bg-slate-100")}>
                    {statusLabel(t.status)}
                  </span>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}