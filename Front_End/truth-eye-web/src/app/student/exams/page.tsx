"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { CalendarClock, ClipboardList, Clock, Loader2, Play, RotateCcw, Target, Trophy } from "lucide-react";
import { EmptyState } from "@/components/ui/empty-state";
import { PageHeader } from "@/components/ui/page-header";
import { ErrorState } from "@/components/ui/states";
import { studentApi } from "@/lib/api/student";
import { fmtDateTime } from "@/lib/format";
import { cn } from "@/lib/utils";
import { getExamState, type Exam, type ExamSession } from "@/types/exam";

const FILTERS = ["all", "open", "upcoming", "closed"] as const;
type Filter = (typeof FILTERS)[number];

const STATE_STYLE = {
  open: "bg-emerald-50 text-emerald-600",
  upcoming: "bg-amber-50 text-amber-600",
  closed: "bg-slate-100 text-slate-500",
} as const;

function ExamCard({ exam, session, index }: { exam: Exam; session?: ExamSession; index: number }) {
  const router = useRouter();
  const qc = useQueryClient();
  const state = getExamState(exam);

  const start = useMutation({
    mutationFn: () => studentApi.start(exam.id),
    onSuccess: (s) => {
      qc.invalidateQueries({ queryKey: ["student"] });
      router.push(`/exam/${s.id}`);
    },
    onError: (e: Error) => toast.error(e.message),
  });

  let action: React.ReactNode;
  if (session?.status === "InProgress") {
    action = (
      <Link href={`/exam/${session.id}`} className="flex w-full items-center justify-center gap-2 rounded-xl bg-amber-500 py-3 text-sm font-bold text-white transition hover:bg-amber-600">
        <RotateCcw size={16} /> Continue exam
      </Link>
    );
  } else if (session) {
    action = (
      <Link href={`/student/results/${session.id}`} className="flex w-full items-center justify-center gap-2 rounded-xl border border-slate-200 py-3 text-sm font-bold text-slate-700 transition hover:bg-slate-50">
        <Trophy size={16} /> View result
      </Link>
    );
  } else if (state === "open") {
    action = (
      <button
        onClick={() => start.mutate()}
        disabled={start.isPending}
        className="flex w-full items-center justify-center gap-2 rounded-xl bg-teal-700 hover:bg-teal-800 py-3 text-sm font-bold text-white shadow-lg shadow-teal-700/30 transition hover:shadow-xl active:scale-[0.98] disabled:opacity-60"
      >
        {start.isPending ? <Loader2 size={16} className="animate-spin" /> : <Play size={16} />}
        Start exam
      </button>
    );
  } else {
    action = (
      <button disabled className="w-full cursor-not-allowed rounded-xl bg-slate-100 py-3 text-sm font-bold text-slate-400">
        {state === "upcoming" ? "Not open yet" : "Exam closed"}
      </button>
    );
  }

  return (
    <article
      className="animate-fade-up group flex flex-col rounded-2xl border border-slate-200 bg-white p-6 transition duration-300 hover:-translate-y-1 hover:shadow-xl hover:shadow-slate-200/70"
      style={{ animationDelay: `${Math.min(index, 8) * 0.06}s` }}
    >
      <div className="mb-4 flex items-start justify-between gap-3">
        <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-teal-600 text-white shadow-lg shadow-teal-600/25">
          <ClipboardList size={20} />
        </div>
        <span className={cn("rounded-full px-2.5 py-1 text-xs font-bold capitalize", STATE_STYLE[state])}>
          {state}
        </span>
      </div>

      <h3 className="text-lg font-extrabold leading-snug text-slate-900">{exam.title}</h3>
      {exam.courseName && <p className="mt-0.5 text-sm text-slate-500">{exam.courseName}</p>}

      <dl className="mt-5 grid grid-cols-2 gap-3 text-sm">
        <div className="flex items-center gap-2 text-slate-600"><Clock size={16} className="text-slate-400" />{exam.duration} min</div>
        {exam.totalMarks != null && <div className="flex items-center gap-2 text-slate-600"><Target size={16} className="text-slate-400" />{exam.totalMarks} marks</div>}
        {exam.passingScore != null && <div className="col-span-2 flex items-center gap-2 text-slate-600"><Trophy size={16} className="text-slate-400" />Pass at {exam.passingScore}%</div>}
      </dl>

      <div className="mt-4 space-y-1 rounded-xl bg-slate-50 p-3 text-xs text-slate-500">
        <p className="flex items-center gap-2"><CalendarClock size={14} /> Opens: <b className="text-slate-700">{fmtDateTime(exam.startTime)}</b></p>
        <p className="flex items-center gap-2"><CalendarClock size={14} /> Closes: <b className="text-slate-700">{fmtDateTime(exam.endTime)}</b></p>
      </div>

      <div className="mt-auto pt-5">{action}</div>
    </article>
  );
}

export default function StudentExamsPage() {
  const [filter, setFilter] = useState<Filter>("all");
  const exams = useQuery({ queryKey: ["student", "available"], queryFn: studentApi.availableExams });
  const history = useQuery({ queryKey: ["student", "history"], queryFn: studentApi.history });

  const sessionByExam = useMemo(() => {
    const map = new Map<number, ExamSession>();
    (history.data ?? []).forEach((s) => map.set(s.examId, s));
    return map;
  }, [history.data]);

  const list = (exams.data ?? [])
    .filter((e) => filter === "all" || getExamState(e) === filter)
    .sort((a, b) => +new Date(a.startTime) - +new Date(b.startTime));

  return (
    <div className="space-y-6">
      <PageHeader title="Available Exams" subtitle="Each exam allows a single attempt. Make sure you’re ready before you start." />

      <div className="animate-fade-in flex flex-wrap gap-2">
        {FILTERS.map((f) => (
          <button
            key={f}
            onClick={() => setFilter(f)}
            className={cn(
              "rounded-full px-4 py-2 text-sm font-bold capitalize transition",
              filter === f ? "bg-slate-900 text-white shadow-lg shadow-slate-900/20" : "bg-white text-slate-600 ring-1 ring-slate-200 hover:bg-slate-50"
            )}
          >
            {f}
          </button>
        ))}
      </div>

      {exams.isLoading ? (
        <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
          {[0, 1, 2].map((i) => <div key={i} className="h-72 animate-pulse rounded-2xl bg-slate-100" />)}
        </div>
      ) : exams.isError ? (
        <ErrorState title="Couldn’t load exams" message={(exams.error as Error).message} onRetry={() => exams.refetch()} />
      ) : list.length === 0 ? (
        <EmptyState icon={ClipboardList} title="No exams here" text="Nothing matches this filter right now." />
      ) : (
        <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
          {list.map((e, i) => <ExamCard key={e.id} exam={e} session={sessionByExam.get(e.id)} index={i} />)}
        </div>
      )}
    </div>
  );
}