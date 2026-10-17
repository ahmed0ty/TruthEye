"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, History } from "lucide-react";
import { EmptyState } from "@/components/ui/empty-state";
import { PageHeader } from "@/components/ui/page-header";
import { ErrorState } from "@/components/ui/states";
import { studentApi } from "@/lib/api/student";
import { fmtDateTime } from "@/lib/format";
import { cn } from "@/lib/utils";

const STATUS_STYLE: Record<string, string> = {
  Completed: "bg-emerald-50 text-emerald-600",
  TimedOut: "bg-red-50 text-red-600",
  InProgress: "bg-amber-50 text-amber-600",
};
const STATUS_LABEL: Record<string, string> = {
  Completed: "Completed",
  TimedOut: "Timed out",
  InProgress: "In progress",
};

export default function HistoryPage() {
  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["student", "history"],
    queryFn: studentApi.history,
  });

  const rows = [...(data ?? [])].sort((a, b) => +new Date(b.startTime) - +new Date(a.startTime));

  return (
    <div className="space-y-6">
      <PageHeader title="My History" subtitle="All the exams you’ve taken and their results." />

      {isLoading ? (
        <div className="space-y-3">{[0, 1, 2, 3].map((i) => <div key={i} className="h-16 animate-pulse rounded-2xl bg-slate-100" />)}</div>
      ) : isError ? (
        <ErrorState title="Couldn’t load history" message={(error as Error).message} onRetry={() => refetch()} />
      ) : rows.length === 0 ? (
        <EmptyState icon={History} title="No exams yet" text="Once you take an exam, it will show up here." />
      ) : (
        <div className="animate-fade-up overflow-hidden rounded-2xl border border-slate-200 bg-white">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-400">
                <tr>
                  <th className="px-6 py-3 text-start font-semibold">Exam</th>
                  <th className="px-6 py-3 text-start font-semibold">Date</th>
                  <th className="px-6 py-3 text-start font-semibold">Status</th>
                  <th className="px-6 py-3 text-start font-semibold">Score</th>
                  <th className="px-6 py-3" />
                </tr>
              </thead>
              <tbody>
                {rows.map((s) => {
                  const done = s.status !== "InProgress";
                  return (
                    <tr key={s.id} className="border-t border-slate-100 transition hover:bg-slate-50/70">
                      <td className="px-6 py-4 font-bold text-slate-800">{s.examTitle ?? `Exam #${s.examId}`}</td>
                      <td className="px-6 py-4 text-slate-500">{fmtDateTime(s.startTime)}</td>
                      <td className="px-6 py-4">
                        <span className={cn("rounded-full px-2.5 py-1 text-xs font-bold", STATUS_STYLE[s.status])}>{STATUS_LABEL[s.status] ?? s.status}</span>
                      </td>
                      <td className="px-6 py-4">
                        {done ? (
                          <div className="flex items-center gap-2">
                            <span className="font-extrabold text-slate-900">{s.percentage.toFixed(0)}%</span>
                            <span className={cn("rounded-full px-2 py-0.5 text-[11px] font-bold", s.isPassed ? "bg-emerald-50 text-emerald-600" : "bg-red-50 text-red-600")}>
                              {s.isPassed ? "Passed" : "Failed"}
                            </span>
                          </div>
                        ) : "—"}
                      </td>
                      <td className="px-6 py-4 text-end">
                        <Link
                          href={done ? `/student/results/${s.id}` : `/exam/${s.id}`}
                          className="inline-flex items-center gap-1 text-sm font-bold text-teal-700 hover:text-teal-800"
                        >
                          {done ? "Details" : "Continue"} <ChevronRight size={16} />
                        </Link>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}