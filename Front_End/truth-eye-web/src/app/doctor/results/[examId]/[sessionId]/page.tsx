"use client";

import { use } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Bot, CheckCircle2, XCircle } from "lucide-react";
import { ErrorState } from "@/components/ui/states";
import { doctorApi } from "@/lib/api/doctor";
import { fmtDateTime } from "@/lib/format";
import { cn } from "@/lib/utils";
import { CheatTimeline } from "@/components/proctoring/cheat-timeline";

export default function ReviewPage({ params }: { params: Promise<{ examId: string; sessionId: string }> }) {
  const { examId, sessionId } = use(params);
  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["doctor", "details", sessionId],
    queryFn: () => doctorApi.sessionDetails(Number(sessionId)),
  });

  const back = (
    <Link href={`/doctor/results/${examId}`} className="inline-flex items-center gap-2 text-sm font-semibold text-slate-500 transition hover:text-[#0A2540]">
      <ArrowLeft size={16} /> Back to students
    </Link>
  );

  if (isLoading) return <div className="h-96 animate-pulse rounded-3xl bg-slate-100" />;
  if (isError || !data)
    return (
      <div className="space-y-6">
        {back}
        <ErrorState title="Couldn’t load this attempt" message={(error as Error)?.message} onRetry={() => refetch()} />
      </div>
    );

  return (
    <div className="space-y-6">
      {back}

      <section className="animate-fade-up flex flex-wrap items-center justify-between gap-4 rounded-3xl bg-[#0A2540] p-6 text-white">
        <div>
          <h1 className="text-2xl font-bold">{data.studentName}</h1>
          <p className="mt-1 text-sm text-slate-300">Submitted {fmtDateTime(data.submittedAt)}</p>
        </div>
        <div className="flex items-center gap-4">
          <div className="text-end">
            <p className="text-3xl font-bold text-teal-300">{data.percentage.toFixed(0)}%</p>
            <p className="text-xs text-slate-300">{data.totalScore} marks</p>
          </div>
          <span className={cn("rounded-full px-3 py-1 text-xs font-semibold", data.isPassed ? "bg-emerald-500/20 text-emerald-300" : "bg-red-500/20 text-red-300")}>
            {data.isPassed ? "Passed" : "Failed"}
          </span>
        </div>
      </section>

      <CheatTimeline sessionId={Number(sessionId)} />
      <ol className="space-y-3">
        {data.answers.map((q, i) => {
          const mcq = (q.options?.length ?? 0) > 0;
          return (
            <li key={q.questionId} className="animate-fade-up rounded-2xl border border-slate-200 bg-white p-5" style={{ animationDelay: `${Math.min(i, 8) * 0.05}s` }}>
              <div className="mb-2 flex flex-wrap items-center gap-2">
                <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-[#0A2540] text-xs font-bold text-teal-300">{i + 1}</span>
                <span className={cn("flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-semibold", q.isCorrect ? "bg-emerald-50 text-emerald-700" : "bg-red-50 text-red-700")}>
                  {q.isCorrect ? <CheckCircle2 size={13} /> : <XCircle size={13} />}
                  {q.isCorrect ? "Correct" : "Incorrect"}
                </span>
                <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-semibold text-slate-600">
                  {q.marksObtained.toFixed(1)} / {q.marks}
                </span>
              </div>

              <p className="font-semibold text-[#0A2540]">{q.questionText}</p>

              {mcq ? (
                <ul className="mt-3 grid gap-2 sm:grid-cols-2">
                  {q.options!.map((o) => {
                    const picked = o.id === q.studentSelectedOptionId;
                    return (
                      <li
                        key={o.id}
                        className={cn(
                          "rounded-xl border px-3 py-2 text-sm",
                          o.isCorrect ? "border-teal-300 bg-teal-50 font-semibold text-teal-800" : picked ? "border-red-300 bg-red-50 text-red-700" : "border-slate-200 text-slate-600"
                        )}
                      >
                        {o.optionText}
                        {picked && <span className="ms-2 text-xs">(student’s choice)</span>}
                      </li>
                    );
                  })}
                </ul>
              ) : (
                <div className="mt-3 grid gap-3 sm:grid-cols-2">
                  <div className="rounded-xl bg-slate-50 p-3 text-sm text-slate-700">
                    <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-400">Student answer</p>
                    {q.studentAnswer || <span className="text-slate-400">No answer</span>}
                  </div>
                  <div className="rounded-xl bg-teal-50 p-3 text-sm text-teal-900">
                    <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-teal-600">Model answer</p>
                    {q.modelAnswer}
                  </div>
                  {q.aiSimilarityScore != null && (
                    <p className="flex items-center gap-1.5 text-xs text-slate-500 sm:col-span-2">
                      <Bot size={14} /> AI similarity: <b>{Math.round(q.aiSimilarityScore * 100)}%</b>
                    </p>
                  )}
                </div>
              )}
            </li>
          );
        })}
      </ol>
    </div>
  );
}