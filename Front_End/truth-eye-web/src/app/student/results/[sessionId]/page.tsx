"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, CheckCircle2, Clock, ListChecks, ShieldAlert, Target, XCircle } from "lucide-react";
import { ErrorState } from "@/components/ui/states";
import { studentApi } from "@/lib/api/student";
import { fmtDateTime, fmtSeconds, parseTimeSpan } from "@/lib/format";
import { cn } from "@/lib/utils";

function ScoreRing({ value, passed }: { value: number; passed: boolean }) {
  const R = 54;
  const C = 2 * Math.PI * R;
  const [p, setP] = useState(0);
  useEffect(() => {
    const t = setTimeout(() => setP(Math.max(0, Math.min(value, 100))), 150);
    return () => clearTimeout(t);
  }, [value]);

  return (
    <div className="relative h-44 w-44">
      <svg viewBox="0 0 128 128" className="h-full w-full -rotate-90">
        <circle cx="64" cy="64" r={R} fill="none" strokeWidth="10" className="stroke-white/10" />
        <circle
          cx="64" cy="64" r={R} fill="none" strokeWidth="10" strokeLinecap="round"
          className={passed ? "stroke-emerald-400" : "stroke-red-400"}
          style={{ strokeDasharray: C, strokeDashoffset: C * (1 - p / 100), transition: "stroke-dashoffset 1.2s cubic-bezier(0.22,1,0.36,1)" }}
        />
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center">
        <span className="text-4xl font-extrabold">{value.toFixed(0)}%</span>
        <span className="text-xs text-slate-400">score</span>
      </div>
    </div>
  );
}

export default function ResultPage({ params }: { params: Promise<{ sessionId: string }> }) {
  const { sessionId } = use(params);
  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["student", "result", sessionId],
    queryFn: () => studentApi.result(Number(sessionId)),
  });

  const back = (
    <Link href="/student/history" className="inline-flex items-center gap-2 text-sm font-bold text-slate-500 transition hover:text-slate-900">
      <ArrowLeft size={16} /> Back to history
    </Link>
  );

  if (isLoading) return <div className="h-96 animate-pulse rounded-3xl bg-slate-100" />;
  if (isError || !data)
    return (
      <div className="space-y-6">
        {back}
        <ErrorState title="Result unavailable" message={(error as Error)?.message} onRetry={() => refetch()} />
      </div>
    );

  const stats = [
    { label: "Marks", value: `${data.totalScore} / ${data.totalMarks}`, icon: Target },
    { label: "Correct answers", value: `${data.correctAnswers} / ${data.totalQuestions}`, icon: ListChecks },
    { label: "Time spent", value: fmtSeconds(parseTimeSpan(data.duration)), icon: Clock },
    { label: "Proctoring alerts", value: String(data.aiAlertsCount), icon: ShieldAlert },
  ];

  return (
    <div className="space-y-6">
      {back}

      <section className="animate-fade-up relative overflow-hidden rounded-3xl bg-slate-950 p-8 text-white sm:p-10">
        <div className="bg-grid absolute inset-0" />
        <div className={cn("animate-blob absolute -top-20 -end-10 h-72 w-72 rounded-full blur-3xl", data.isPassed ? "bg-emerald-600/30" : "bg-red-600/30")} />
        <div className="relative flex flex-col items-center gap-8 sm:flex-row">
          <ScoreRing value={data.percentage} passed={data.isPassed} />
          <div className="text-center sm:text-start">
            <span className={cn("inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-bold", data.isPassed ? "bg-emerald-500/20 text-emerald-300" : "bg-red-500/20 text-red-300")}>
              {data.isPassed ? <CheckCircle2 size={14} /> : <XCircle size={14} />}
              {data.isPassed ? "Passed" : "Not passed"}
            </span>
            <h1 className="mt-3 text-3xl font-extrabold">{data.examTitle}</h1>
            <p className="mt-1 text-slate-400">Submitted {fmtDateTime(data.submittedAt)}</p>
          </div>
        </div>
      </section>

      <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-4">
        {stats.map(({ label, value, icon: Icon }, i) => (
          <div key={label} className="animate-fade-up rounded-2xl border border-slate-200 bg-white p-5" style={{ animationDelay: `${0.1 + i * 0.07}s` }}>
            <div className="mb-3 flex h-10 w-10 items-center justify-center rounded-xl bg-teal-50 text-teal-700">
              <Icon size={20} />
            </div>
            <p className="text-sm font-semibold text-slate-500">{label}</p>
            <p className="mt-0.5 text-2xl font-extrabold text-slate-900">{value}</p>
          </div>
        ))}
      </div>
    </div>
  );
}