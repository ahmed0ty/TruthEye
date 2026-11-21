"use client";

import { use } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft } from "lucide-react";
import { BarChart, Stat } from "@/components/analytics/bars";
import { PageHeader } from "@/components/ui/page-header";
import { ErrorState } from "@/components/ui/states";
import { analyticsApi } from "@/lib/api/analytics";

const DIFF: Record<string, string> = {
  Easy: "bg-emerald-50 text-emerald-700",
  Medium: "bg-amber-50 text-amber-700",
  Hard: "bg-red-50 text-red-700",
};

export default function ExamAnalyticsPage({ params }: { params: Promise<{ examId: string }> }) {
  const { examId } = use(params);
  const id = Number(examId);
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ["analytics", "exam", id],
    queryFn: () => analyticsApi.exam(id),
  });

  if (isLoading) return <div className="h-96 animate-pulse rounded-3xl bg-slate-100" />;
  if (error || !data)
    return <ErrorState title="Couldn’t load analytics" message={(error as Error)?.message} onRetry={() => refetch()} />;

  return (
    <div className="space-y-6">
      <Link href={`/doctor/results/${id}`} className="inline-flex items-center gap-2 text-sm font-semibold text-slate-500 hover:text-[#0A2540]">
        <ArrowLeft size={16} /> Back to results
      </Link>
      <PageHeader title={`Analytics: ${data.title}`} subtitle="Finished attempts only (completed, timed out and terminated)." />

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <Stat label="Students" value={data.students} />
        <Stat label="Average" value={`${data.avgPercentage}%`} />
        <Stat label="Pass rate" value={`${data.passRate}%`} tone="text-emerald-600" />
        <Stat label="Highest / Lowest" value={`${data.highest}% / ${data.lowest}%`} />
        <Stat label="Flagged" value={data.flagged} tone="text-red-600" />
      </div>

      <section className="rounded-2xl border border-slate-200 bg-white p-5">
        <h3 className="mb-4 font-bold text-slate-800">Score distribution (%)</h3>
        <BarChart data={data.buckets.map((b) => ({ label: b.label, value: b.count }))} />
      </section>

      <section className="rounded-2xl border border-slate-200 bg-white p-5">
        <h3 className="mb-1 font-bold text-slate-800">Questions, hardest first</h3>
        <p className="mb-4 text-xs text-slate-400">Score rate = marks students earned out of the marks available (skipped counts as zero).</p>
        {data.questions.length === 0 && <p className="text-sm text-slate-500">No questions.</p>}
        <ul className="space-y-3">
          {data.questions.map((q) => (
            <li key={q.questionId} className="space-y-1.5">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <p className="min-w-0 flex-1 truncate text-sm font-semibold text-slate-700">{q.text}</p>
                <span className={`rounded-full px-2.5 py-0.5 text-xs font-bold ${DIFF[q.difficulty]}`}>{q.difficulty}</span>
              </div>
              <div className="h-2.5 overflow-hidden rounded-full bg-slate-100">
                <div className="h-full rounded-full bg-teal-500" style={{ width: `${q.scoreRate}%` }} />
              </div>
              <p className="text-xs text-slate-400">
                {q.scoreRate}% · avg {q.avgMarks} / {q.maxMarks} · skipped {q.skipped}
              </p>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}