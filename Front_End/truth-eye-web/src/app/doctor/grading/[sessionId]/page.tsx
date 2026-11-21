"use client";

import { useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { PageHeader } from "@/components/ui/page-header";
import { gradingApi } from "@/lib/api/grading";
import type { GradingAnswer } from "@/types/grading";

function AnswerRow({ a, sessionId }: { a: GradingAnswer; sessionId: number }) {
  const qc = useQueryClient();
  const [marks, setMarks] = useState(String(a.marksObtained));
  const [reason, setReason] = useState("");

  const save = useMutation({
    mutationFn: () => gradingApi.adjust(a.answerId, { marks: Number(marks), reason }),
    onSuccess: (d) => {
      toast.success("Grade updated");
      qc.setQueryData(["grading", sessionId], d);
      qc.invalidateQueries({ queryKey: ["appeals"] });
      qc.invalidateQueries({ queryKey: ["reviews"] });
      setReason("");
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const changed = Number(marks) !== a.marksObtained;
  const valid = marks !== "" && Number(marks) >= 0 && Number(marks) <= a.maxMarks;

  return (
    <div className="space-y-3 rounded-2xl border border-slate-200 bg-white p-4">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <p className="font-semibold text-slate-800">{a.questionText}</p>
        <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-bold text-slate-600">
          {a.marksObtained} / {a.maxMarks}
          {a.adjusted && <span className="ms-1 text-purple-600">(edited)</span>}
        </span>
      </div>

      <div className="grid gap-3 text-sm sm:grid-cols-2">
        <div className="rounded-lg bg-slate-50 p-3">
          <p className="mb-1 text-xs font-bold uppercase text-slate-400">Student answer</p>
          <p className="whitespace-pre-wrap text-slate-700">{a.studentAnswer || a.selectedOption || "—"}</p>
        </div>
        {a.modelAnswer && (
          <div className="rounded-lg bg-emerald-50 p-3">
            <p className="mb-1 text-xs font-bold uppercase text-emerald-600">Model answer</p>
            <p className="whitespace-pre-wrap text-slate-700">{a.modelAnswer}</p>
          </div>
        )}
      </div>

      {a.aiFeedback && (
        <p className="text-xs text-slate-500">
          AI{a.aiSimilarity != null && <> ({(a.aiSimilarity * 100).toFixed(0)}% similar)</>}: {a.aiFeedback}
        </p>
      )}
      {a.lastReason && <p className="text-xs text-purple-600">Last change: {a.lastReason}</p>}

      <div className="flex flex-wrap items-center gap-2">
        <input
          type="number" min={0} max={a.maxMarks} step="0.25" value={marks}
          onChange={(e) => setMarks(e.target.value)}
          className="w-24 rounded-xl border border-slate-200 px-3 py-2 text-sm outline-none focus:border-teal-600"
        />
        <input
          value={reason} onChange={(e) => setReason(e.target.value)} maxLength={500}
          placeholder="Reason for the change"
          className="min-w-[200px] flex-1 rounded-xl border border-slate-200 px-3 py-2 text-sm outline-none focus:border-teal-600"
        />
        <button
          type="button"
          disabled={!changed || !valid || reason.trim().length < 3 || save.isPending}
          onClick={() => save.mutate()}
          className="rounded-xl bg-teal-700 px-4 py-2 text-sm font-bold text-white hover:bg-teal-800 disabled:opacity-50"
        >
          Save
        </button>
      </div>
    </div>
  );
}

export default function GradingPage() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const id = Number(sessionId);
  const { data, isLoading, error } = useQuery({ queryKey: ["grading", id], queryFn: () => gradingApi.session(id) });

  if (isLoading) return <p className="py-10 text-center text-sm text-slate-500">Loading...</p>;
  if (error || !data) return <p className="py-10 text-center text-sm text-red-600">{(error as Error)?.message ?? "Not found"}</p>;

  return (
    <div className="space-y-6">
      <Link href="/doctor/appeals" className="text-sm font-semibold text-teal-700 hover:text-teal-800">← Back</Link>
      <PageHeader
        title={`Grading: ${data.studentName}`}
        subtitle={`${data.examTitle} · ${data.totalScore} / ${data.totalMarks} (${data.percentage.toFixed(1)}%) · ${data.isPassed ? "Passed" : "Failed"} (pass mark ${data.passingScore}%)`}
      />
      <div className="space-y-4">
        {data.answers.length === 0 && <p className="text-sm text-slate-500">This session has no answers.</p>}
        {data.answers.map((a) => <AnswerRow key={a.answerId} a={a} sessionId={id} />)}
      </div>
    </div>
  );
}