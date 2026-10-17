"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { BarChart3, ChevronRight } from "lucide-react";
import { EmptyState } from "@/components/ui/empty-state";
import { PageHeader } from "@/components/ui/page-header";
import { ErrorState } from "@/components/ui/states";
import { doctorApi } from "@/lib/api/doctor";
import { fmtDateTime } from "@/lib/format";

export default function ResultsIndex() {
  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["doctor", "exams"],
    queryFn: doctorApi.exams,
  });
  const rows = [...(data ?? [])].sort((a, b) => +new Date(b.startTime) - +new Date(a.startTime));

  return (
    <div className="space-y-6">
      <PageHeader title="Results" subtitle="Pick an exam to see how your students performed." />

      {isLoading ? (
        <div className="space-y-3">{[0, 1, 2].map((i) => <div key={i} className="h-20 animate-pulse rounded-2xl bg-slate-100" />)}</div>
      ) : isError ? (
        <ErrorState title="Couldn’t load exams" message={(error as Error).message} onRetry={() => refetch()} />
      ) : rows.length === 0 ? (
        <EmptyState icon={BarChart3} title="No exams yet" text="Create an exam first, then its results will show up here." />
      ) : (
        <ul className="space-y-3">
          {rows.map((e, i) => (
            <li key={e.id} className="animate-fade-up" style={{ animationDelay: `${Math.min(i, 8) * 0.05}s` }}>
              <Link
                href={`/doctor/results/${e.id}`}
                className="flex items-center justify-between gap-4 rounded-2xl border border-slate-200 bg-white p-5 transition hover:-translate-y-0.5 hover:border-teal-300 hover:shadow-lg hover:shadow-slate-200/70"
              >
                <div className="min-w-0">
                  <p className="truncate font-bold text-[#0A2540]">{e.title}</p>
                  <p className="text-xs text-slate-500">{e.courseName ?? "—"} · {fmtDateTime(e.startTime)}</p>
                </div>
                <ChevronRight className="shrink-0 text-slate-400" size={20} />
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}