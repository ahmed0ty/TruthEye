"use client";

import { useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { BarChart, Stat } from "@/components/analytics/bars";
import { PageHeader } from "@/components/ui/page-header";
import { ErrorState } from "@/components/ui/states";
import { analyticsApi } from "@/lib/api/analytics";

const iso = (d: Date) => d.toISOString().slice(0, 10);

export default function FacultyAnalyticsPage() {
  const [to, setTo] = useState(iso(new Date()));
  const [from, setFrom] = useState(iso(new Date(Date.now() - 30 * 864e5)));

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ["analytics", "faculty", from, to],
    queryFn: () => analyticsApi.faculty({ from, to }),
    placeholderData: keepPreviousData,
  });

  return (
    <div className="space-y-6">
      <PageHeader title="Analytics" subtitle="Exam sessions in your faculty. Times are in UTC." />

      <div className="flex flex-wrap items-center gap-3 text-sm text-slate-600">
        <label className="flex items-center gap-2">From
          <input type="date" value={from} max={to} onChange={(e) => e.target.value && setFrom(e.target.value)}
            className="rounded-xl border border-slate-200 px-3 py-2 outline-none focus:border-teal-600" />
        </label>
        <label className="flex items-center gap-2">To
          <input type="date" value={to} min={from} onChange={(e) => e.target.value && setTo(e.target.value)}
            className="rounded-xl border border-slate-200 px-3 py-2 outline-none focus:border-teal-600" />
        </label>
      </div>

      {isLoading ? (
        <div className="h-96 animate-pulse rounded-3xl bg-slate-100" />
      ) : error || !data ? (
        <ErrorState title="Couldn’t load analytics" message={(error as Error)?.message} onRetry={() => refetch()} />
      ) : (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <Stat label="Sessions" value={data.totalSessions} />
            <Stat label="Average score" value={`${data.avgPercentage}%`} />
            <Stat label="Pass rate" value={`${data.passRate}%`} tone="text-emerald-600" />
            <Stat label="Flagged sessions" value={`${data.flaggedSessions} (${data.flaggedRate}%)`} tone="text-red-600" />
          </div>

          <div className="grid gap-5 lg:grid-cols-2">
            <section className="rounded-2xl border border-slate-200 bg-white p-5">
              <h3 className="mb-4 font-bold text-slate-800">Peak hours</h3>
              <BarChart tone="bg-sky-500" data={data.hours.map((h) => ({ label: String(h.hour), value: h.count }))} />
            </section>
            <section className="rounded-2xl border border-slate-200 bg-white p-5">
              <h3 className="mb-4 font-bold text-slate-800">Sessions per day</h3>
              {data.daily.length === 0 ? (
                <p className="py-10 text-center text-sm text-slate-400">No sessions in this period.</p>
              ) : (
                <BarChart data={data.daily.map((d) => ({ label: d.date.slice(5), value: d.sessions }))} />
              )}
            </section>
          </div>

          <section className="overflow-x-auto rounded-2xl border border-slate-200 bg-white">
            <table className="w-full min-w-[560px] text-sm">
              <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-400">
                <tr>
                  {["Course", "Exams", "Sessions", "Average", "Pass rate", "Flagged"].map((h) => (
                    <th key={h} className="px-5 py-3 text-start font-semibold">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {data.courses.length === 0 && (
                  <tr><td colSpan={6} className="px-5 py-10 text-center text-slate-400">No data.</td></tr>
                )}
                {data.courses.map((c) => (
                  <tr key={c.courseId} className="border-t border-slate-100">
                    <td className="px-5 py-3 font-semibold text-slate-800">{c.name}</td>
                    <td className="px-5 py-3">{c.exams}</td>
                    <td className="px-5 py-3">{c.sessions}</td>
                    <td className="px-5 py-3">{c.avgPercentage}%</td>
                    <td className="px-5 py-3">{c.passRate}%</td>
                    <td className={`px-5 py-3 font-semibold ${c.flagged ? "text-red-600" : "text-slate-400"}`}>{c.flagged}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>
        </>
      )}
    </div>
  );
}