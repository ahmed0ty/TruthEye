"use client";

import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  BookOpen, Building2, GraduationCap, LifeBuoy, ScanEye, ShieldCheck, Sparkles,
  Stethoscope, Users, type LucideIcon,
} from "lucide-react";
import { AdminCharts } from "@/components/dashboard/admin-charts";
import { adminApi } from "@/lib/api/admin";
import { useAuth } from "@/lib/auth/store";
import { cn } from "@/lib/utils";
import type { PersonRow } from "@/types/api";

type Tone = "navy" | "teal" | "sky" | "emerald" | "amber";

// Soft tinted tiles that stay inside the navy / teal brand palette
const TONES: Record<Tone, { tile: string; glow: string }> = {
  navy: { tile: "bg-[#0A2540] text-teal-300", glow: "bg-[#0A2540]" },
  teal: { tile: "bg-teal-50 text-teal-700 ring-1 ring-teal-100", glow: "bg-teal-500" },
  sky: { tile: "bg-sky-50 text-sky-700 ring-1 ring-sky-100", glow: "bg-sky-500" },
  emerald: { tile: "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-100", glow: "bg-emerald-500" },
  amber: { tile: "bg-amber-50 text-amber-700 ring-1 ring-amber-100", glow: "bg-amber-500" },
};

interface StatDef {
  key: string;
  label: string;
  icon: LucideIcon;
  tone: Tone;
  fn: () => Promise<unknown[]>;
}

const ADMIN_STATS: StatDef[] = [
  { key: "students", label: "Students", icon: Users, tone: "navy", fn: adminApi.students },
  { key: "doctors", label: "Doctors", icon: Stethoscope, tone: "teal", fn: adminApi.doctors },
  { key: "proctors", label: "Proctors", icon: ScanEye, tone: "sky", fn: adminApi.proctors },
  { key: "departments", label: "Departments", icon: Building2, tone: "emerald", fn: adminApi.departments },
  { key: "courses", label: "Courses", icon: BookOpen, tone: "amber", fn: adminApi.courses },
];

const SUPER_STATS: StatDef[] = [
  { key: "admins", label: "Faculty Admins", icon: ShieldCheck, tone: "navy", fn: adminApi.admins },
  { key: "supports", label: "Support Team", icon: LifeBuoy, tone: "teal", fn: adminApi.supports },
  { key: "universities", label: "Universities", icon: Building2, tone: "sky", fn: adminApi.universities },
  { key: "faculties", label: "Faculties", icon: GraduationCap, tone: "emerald", fn: adminApi.faculties },
];

function CountUp({ value }: { value: number }) {
  const [n, setN] = useState(0);
  useEffect(() => {
    let raf = 0;
    const start = performance.now();
    const tick = (t: number) => {
      const p = Math.min((t - start) / 900, 1);
      setN(Math.round(value * (1 - Math.pow(1 - p, 3))));
      if (p < 1) raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [value]);
  return <>{n.toLocaleString("en-US")}</>;
}

function StatCard({ def, index }: { def: StatDef; index: number }) {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["admin-stat", def.key],
    queryFn: def.fn,
  });
  const Icon = def.icon;
  const tone = TONES[def.tone];

  return (
    <div
      className="animate-fade-up group relative overflow-hidden rounded-2xl border border-slate-200 bg-white p-6 shadow-sm transition duration-300 hover:-translate-y-0.5 hover:border-teal-200 hover:shadow-lg hover:shadow-slate-200/70"
      style={{ animationDelay: `${index * 0.08}s` }}
    >
      <div
        className={cn(
          "pointer-events-none absolute -end-10 -top-10 h-28 w-28 rounded-full opacity-[0.06] transition duration-300 group-hover:scale-125",
          tone.glow
        )}
      />
      <div className={cn("mb-5 flex h-12 w-12 items-center justify-center rounded-xl", tone.tile)}>
        <Icon size={22} />
      </div>
      <p className="text-sm font-medium text-slate-500">{def.label}</p>
      {isLoading ? (
        <div className="mt-2 h-9 w-20 animate-pulse rounded-lg bg-slate-100" />
      ) : isError ? (
        <p className="mt-1 text-3xl font-bold text-slate-300">—</p>
      ) : (
        <p className="mt-1 text-4xl font-bold tracking-tight text-[#0A2540]">
          <CountUp value={Array.isArray(data) ? data.length : 0} />
        </p>
      )}
    </div>
  );
}

function RecentTable({ isSuper }: { isSuper: boolean }) {
  const { data, isLoading } = useQuery({
    queryKey: ["admin-recent", isSuper ? "admins" : "students"],
    queryFn: isSuper ? adminApi.admins : adminApi.students,
  });

  const rows = [...(data ?? [])]
    .sort((a: PersonRow, b: PersonRow) => +new Date(b.createdAt) - +new Date(a.createdAt))
    .slice(0, 6);

  return (
    <section
      className="animate-fade-up overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm"
      style={{ animationDelay: "0.45s" }}
    >
      <div className="flex items-center justify-between border-b border-slate-100 px-6 py-5">
        <h3 className="font-bold text-[#0A2540]">
          {isSuper ? "Recent faculty admins" : "Recently added students"}
        </h3>
        <span className="rounded-full bg-teal-50 px-3 py-1 text-xs font-semibold text-teal-700 ring-1 ring-teal-100">
          {rows.length} {rows.length === 1 ? "record" : "records"}
        </span>
      </div>

      {isLoading ? (
        <div className="space-y-3 p-6">
          {[0, 1, 2].map((i) => (
            <div key={i} className="h-12 animate-pulse rounded-xl bg-slate-100" />
          ))}
        </div>
      ) : rows.length === 0 ? (
        <div className="px-6 py-14 text-center text-sm text-slate-400">No data to show yet</div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-slate-50/80 text-xs uppercase tracking-wide text-slate-500">
                <th className="px-6 py-3 text-start font-semibold">Name</th>
                <th className="px-6 py-3 text-start font-semibold">Email</th>
                <th className="px-6 py-3 text-start font-semibold">{isSuper ? "Faculty" : "Department"}</th>
                <th className="px-6 py-3 text-start font-semibold">Status</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.id} className="border-t border-slate-100 transition hover:bg-slate-50/70">
                  <td className="px-6 py-3.5">
                    <div className="flex items-center gap-3">
                      <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-[#0A2540] text-xs font-bold text-teal-300">
                        {r.firstName?.[0]}
                        {r.lastName?.[0]}
                      </div>
                      <span className="font-semibold text-slate-800">
                        {r.firstName} {r.lastName}
                      </span>
                    </div>
                  </td>
                  <td className="px-6 py-3.5 text-slate-500" dir="ltr">
                    {r.email}
                  </td>
                  <td className="px-6 py-3.5 text-slate-600">
                    {(isSuper ? r.facultyName : r.departmentName) ?? "—"}
                  </td>
                  <td className="px-6 py-3.5">
                    <span
                      className={cn(
                        "inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold",
                        r.isActive ? "bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-500"
                      )}
                    >
                      <span
                        className={cn(
                          "h-1.5 w-1.5 rounded-full",
                          r.isActive ? "bg-emerald-500" : "bg-slate-400"
                        )}
                      />
                      {r.isActive ? "Active" : "Disabled"}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

export default function AdminHome() {
  const user = useAuth((s) => s.user);
  if (!user) return null;

  const isSuper = user.role === "SuperAdmin";
  const stats = isSuper ? SUPER_STATS : ADMIN_STATS;
  const hour = new Date().getHours();
  const greet = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";

  return (
    <div className="space-y-8">
      {/* Banner */}
      <section className="animate-fade-up relative overflow-hidden rounded-3xl bg-[#0A2540] p-8 text-white sm:p-10">
        <div
          aria-hidden
          className="absolute inset-0 opacity-[0.07]"
          style={{
            backgroundImage:
              "linear-gradient(#fff 1px, transparent 1px), linear-gradient(90deg, #fff 1px, transparent 1px)",
            backgroundSize: "44px 44px",
          }}
        />
        <div className="animate-blob pointer-events-none absolute -top-20 -end-10 h-72 w-72 rounded-full bg-teal-500/25 blur-3xl" />
        <div
          className="animate-blob pointer-events-none absolute -bottom-24 start-1/3 h-72 w-72 rounded-full bg-sky-500/15 blur-3xl"
          style={{ animationDelay: "-5s" }}
        />

        <div className="relative flex items-center justify-between gap-6">
          <div>
            <span className="inline-flex items-center gap-1.5 rounded-full bg-teal-400/15 px-3 py-1 text-xs font-semibold text-teal-200 ring-1 ring-teal-300/30">
              <Sparkles size={14} />
              {isSuper ? "System Admin Dashboard" : "Faculty Admin Dashboard"}
            </span>
            <h1 className="mt-4 text-3xl font-bold sm:text-4xl">
              {greet}, {user.name.split(" ")[0]} 👋
            </h1>
            <p className="mt-2 max-w-xl text-slate-300">
              A quick overview of the platform. Track totals and the latest additions from here.
            </p>
          </div>
          <ShieldCheck className="animate-float hidden text-teal-300/50 md:block" size={110} strokeWidth={1.2} />
        </div>
      </section>

      {/* Stats */}
      <div className={cn("grid gap-5 sm:grid-cols-2", isSuper ? "xl:grid-cols-4" : "xl:grid-cols-5")}>
        {stats.map((s, i) => (
          <StatCard key={s.key} def={s} index={i} />
        ))}
      </div>

      {!isSuper && <AdminCharts />}

      <RecentTable isSuper={isSuper} />
    </div>
  );
}