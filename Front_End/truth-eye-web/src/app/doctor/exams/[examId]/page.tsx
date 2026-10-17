"use client";

import { use, useState } from "react";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { AlertTriangle, ArrowLeft, CheckCircle2, FileQuestion, ListChecks, Loader2, PenLine, Plus, Trash2, X } from "lucide-react";
import { Confirm } from "@/components/ui/confirm";
import { EmptyState } from "@/components/ui/empty-state";
import { Modal } from "@/components/ui/modal";
import { ErrorState } from "@/components/ui/states";
import { doctorApi, type QuestionPayload } from "@/lib/api/doctor";
import { useDelete } from "@/hooks/use-crud";
import { cn } from "@/lib/utils";
import { QUESTION_TYPES, isMcq, type Question } from "@/types/doctor";

const input =
  "w-full rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-sm outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-4 focus:ring-teal-100";
const label = "mb-1.5 block text-sm font-semibold text-slate-700";

type Kind = "mcq" | "written";
interface Opt { optionText: string; isCorrect: boolean }

function QuestionForm({ examId, onDone }: { examId: number; onDone: () => void }) {
  const qc = useQueryClient();
  const [kind, setKind] = useState<Kind>("mcq");
  const [text, setText] = useState("");
  const [marks, setMarks] = useState(1);
  const [options, setOptions] = useState<Opt[]>([
    { optionText: "", isCorrect: true },
    { optionText: "", isCorrect: false },
  ]);
  const [model, setModel] = useState("");
  const [threshold, setThreshold] = useState(0);
  const [error, setError] = useState("");

  const save = useMutation({
    mutationFn: (p: QuestionPayload) => doctorApi.addQuestion(p),
    onSuccess: () => {
      toast.success("Question added");
      qc.invalidateQueries({ queryKey: ["doctor", "questions", examId] });
      qc.invalidateQueries({ queryKey: ["doctor", "exams"] });
      onDone();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const setOpt = (i: number, patch: Partial<Opt>) =>
    setOptions((o) => o.map((x, idx) => (idx === i ? { ...x, ...patch } : x)));
  const markCorrect = (i: number) => setOptions((o) => o.map((x, idx) => ({ ...x, isCorrect: idx === i })));
  const removeOpt = (i: number) =>
    setOptions((o) => {
      const next = o.filter((_, idx) => idx !== i);
      if (!next.some((x) => x.isCorrect) && next[0]) next[0].isCorrect = true;
      return next;
    });

  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    if (text.trim().length < 3) return setError("Write the question text.");
    if (!(marks >= 1)) return setError("Marks must be at least 1.");

    if (kind === "mcq") {
      const clean = options.map((o) => ({ ...o, optionText: o.optionText.trim() }));
      if (clean.length < 2) return setError("Add at least 2 options.");
      if (clean.some((o) => !o.optionText)) return setError("Fill in every option.");
      if (clean.filter((o) => o.isCorrect).length !== 1) return setError("Mark exactly one correct option.");
      save.mutate({ examId, questionText: text.trim(), questionType: QUESTION_TYPES.mcq, marks, options: clean });
    } else {
      if (model.trim().length < 3) return setError("Write the model answer used for AI grading.");
      save.mutate({
        examId,
        questionText: text.trim(),
        questionType: QUESTION_TYPES.written,
        marks,
        modelAnswer: model.trim(),
        aiThreshold: threshold,
        evaluationModel: "Default",
      });
    }
  };

  return (
    <form onSubmit={submit} className="space-y-5">
      <div className="grid grid-cols-2 gap-2 rounded-2xl bg-slate-100 p-1.5">
        {([
          ["mcq", "Multiple choice", ListChecks],
          ["written", "Written (AI graded)", PenLine],
        ] as const).map(([k, title, Icon]) => (
          <button
            key={k}
            type="button"
            onClick={() => setKind(k)}
            className={cn(
              "flex items-center justify-center gap-2 rounded-xl py-2.5 text-sm font-semibold transition",
              kind === k ? "bg-white text-teal-700 shadow-sm" : "text-slate-500 hover:text-slate-700"
            )}
          >
            <Icon size={16} /> {title}
          </button>
        ))}
      </div>

      <div>
        <label className={label}>Question</label>
        <textarea rows={3} value={text} onChange={(e) => setText(e.target.value)} className={cn(input, "resize-none")} placeholder="Type the question..." />
      </div>

      <div className="w-32">
        <label className={label}>Marks</label>
        <input type="number" min={1} value={marks} onChange={(e) => setMarks(Number(e.target.value))} className={input} />
      </div>

      {kind === "mcq" ? (
        <div>
          <label className={label}>Options <span className="font-normal text-slate-400">(select the correct one)</span></label>
          <div className="space-y-2">
            {options.map((o, i) => (
              <div key={i} className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => markCorrect(i)}
                  aria-label="Mark as correct"
                  className={cn(
                    "flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border transition",
                    o.isCorrect ? "border-teal-600 bg-teal-600 text-white" : "border-slate-300 text-slate-300 hover:border-teal-400 hover:text-teal-500"
                  )}
                >
                  <CheckCircle2 size={18} />
                </button>
                <input value={o.optionText} onChange={(e) => setOpt(i, { optionText: e.target.value })} className={input} placeholder={`Option ${i + 1}`} />
                {options.length > 2 && (
                  <button type="button" onClick={() => removeOpt(i)} className="rounded-lg p-2 text-slate-400 hover:bg-red-50 hover:text-red-600" aria-label="Remove option">
                    <X size={17} />
                  </button>
                )}
              </div>
            ))}
          </div>
          {options.length < 6 && (
            <button
              type="button"
              onClick={() => setOptions((o) => [...o, { optionText: "", isCorrect: false }])}
              className="mt-3 inline-flex items-center gap-1.5 text-sm font-semibold text-teal-700 hover:text-teal-800"
            >
              <Plus size={16} /> Add option
            </button>
          )}
        </div>
      ) : (
        <div className="space-y-4">
          <div>
            <label className={label}>Model answer</label>
            <textarea rows={4} value={model} onChange={(e) => setModel(e.target.value)} className={cn(input, "resize-none")} placeholder="The ideal answer. The AI compares students’ answers to this." />
          </div>
          <div>
            <label className={label}>
              AI similarity threshold:{" "}
              <span className="text-teal-700">
                {threshold === 0 ? "Default (from settings)" : `${Math.round(threshold * 100)}%`}
              </span>
              {threshold !== 0 && (
                <button
                  type="button"
                  onClick={() => setThreshold(0)}
                  className="ms-3 text-xs font-semibold text-teal-700 hover:text-teal-800"
                >
                  Reset to default
                </button>
              )}
            </label>
            <input
              type="range"
              min={0.3}
              max={0.95}
              step={0.05}
              value={threshold === 0 ? 0.7 : threshold}
              onChange={(e) => setThreshold(Number(e.target.value))}
              className="w-full accent-teal-600"
            />
            <p className="mt-1 text-xs text-slate-400">Higher means the student’s answer must be closer to the model answer.</p>
          </div>
        </div>
      )}

      {error && <p className="rounded-xl bg-red-50 px-4 py-3 text-sm font-medium text-red-600">{error}</p>}

      <div className="flex justify-end pt-1">
        <button
          disabled={save.isPending}
          className="flex items-center gap-2 rounded-xl bg-teal-600 px-6 py-2.5 text-sm font-semibold text-white shadow-lg shadow-teal-900/10 transition hover:bg-teal-700 active:scale-[0.98] disabled:opacity-60"
        >
          {save.isPending && <Loader2 size={16} className="animate-spin" />}
          Add question
        </button>
      </div>
    </form>
  );
}

export default function ExamQuestionsPage({ params }: { params: Promise<{ examId: string }> }) {
  const { examId: raw } = use(params);
  const examId = Number(raw);
  const [adding, setAdding] = useState(false);

  const exam = useQuery({ queryKey: ["doctor", "exam", examId], queryFn: () => doctorApi.exam(examId) });
  const questions = useQuery({ queryKey: ["doctor", "questions", examId], queryFn: () => doctorApi.questions(examId) });
  const del = useDelete<Question>(["doctor", "questions", examId], doctorApi.deleteQuestion, "Question deleted");

  const list = questions.data ?? [];
  const sum = list.reduce((a, q) => a + q.marks, 0);
  const total = exam.data?.totalMarks;
  const mismatch = total != null && list.length > 0 && sum !== total;

  return (
    <div className="space-y-6">
      <Link href="/doctor/exams" className="inline-flex items-center gap-2 text-sm font-semibold text-slate-500 transition hover:text-[#0A2540]">
        <ArrowLeft size={16} /> Back to exams
      </Link>

      <div className="animate-fade-up flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-[#0A2540]">{exam.data?.title ?? "Exam questions"}</h1>
          <p className="mt-1 text-sm text-slate-500">
            {list.length} questions · {sum} marks{total != null ? ` of ${total}` : ""}
          </p>
        </div>
        <button
          onClick={() => setAdding(true)}
          className="inline-flex items-center gap-2 rounded-xl bg-teal-600 px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-teal-900/10 transition hover:bg-teal-700 active:scale-[0.98]"
        >
          <Plus size={18} /> Add question
        </button>
      </div>

      {mismatch && (
        <div className="flex items-start gap-3 rounded-2xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
          <AlertTriangle size={18} className="mt-0.5 shrink-0" />
          <p>
            Question marks add up to <b>{sum}</b>, but the exam’s total marks is <b>{total}</b>. Percentages are calculated from the exam total, so adjust one of them.
          </p>
        </div>
      )}

      {questions.isLoading ? (
        <div className="space-y-3">{[0, 1, 2].map((i) => <div key={i} className="h-28 animate-pulse rounded-2xl bg-slate-100" />)}</div>
      ) : questions.isError ? (
        <ErrorState title="Couldn’t load questions" message={(questions.error as Error).message} onRetry={() => questions.refetch()} />
      ) : list.length === 0 ? (
        <EmptyState icon={FileQuestion} title="No questions yet" text="Add multiple-choice or written questions. Students can’t take an empty exam." />
      ) : (
        <ol className="space-y-3">
          {list.map((q, i) => (
            <li key={q.id} className="animate-fade-up rounded-2xl border border-slate-200 bg-white p-5" style={{ animationDelay: `${Math.min(i, 8) * 0.05}s` }}>
              <div className="flex items-start gap-4">
                <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-[#0A2540] text-sm font-bold text-teal-300">{i + 1}</span>
                <div className="min-w-0 flex-1">
                  <div className="mb-2 flex flex-wrap items-center gap-2">
                    <span className={cn("rounded-full px-2.5 py-0.5 text-xs font-semibold", isMcq(q) ? "bg-sky-50 text-sky-700" : "bg-amber-50 text-amber-700")}>
                      {isMcq(q) ? "Multiple choice" : "Written · AI"}
                    </span>
                    <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-semibold text-slate-600">{q.marks} {q.marks === 1 ? "mark" : "marks"}</span>
                  </div>
                  <p className="font-semibold text-[#0A2540]">{q.questionText}</p>

                  {isMcq(q) ? (
                    <ul className="mt-3 grid gap-2 sm:grid-cols-2">
                      {(q.options ?? []).map((o, idx) => (
                        <li
                          key={o.id ?? idx}
                          className={cn(
                            "flex items-center gap-2 rounded-xl border px-3 py-2 text-sm",
                            o.isCorrect ? "border-teal-300 bg-teal-50 font-semibold text-teal-800" : "border-slate-200 text-slate-600"
                          )}
                        >
                          {o.isCorrect && <CheckCircle2 size={15} className="shrink-0" />}
                          {o.optionText}
                        </li>
                      ))}
                    </ul>
                  ) : (
                    <div className="mt-3 rounded-xl bg-slate-50 p-3 text-sm text-slate-600">
                      <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-400">Model answer</p>
                      {q.modelAnswer}
                      {q.aiThreshold != null && <p className="mt-2 text-xs text-slate-400">AI threshold: {Math.round(q.aiThreshold * 100)}%</p>}
                    </div>
                  )}
                </div>
                <button onClick={() => del.setTarget(q)} className="rounded-lg p-2 text-slate-400 transition hover:bg-red-50 hover:text-red-600" aria-label="Delete question">
                  <Trash2 size={18} />
                </button>
              </div>
            </li>
          ))}
        </ol>
      )}

      <Modal open={adding} title="New question" onClose={() => setAdding(false)} wide>
        <QuestionForm examId={examId} onDone={() => setAdding(false)} />
      </Modal>

      <Confirm
        open={!!del.target}
        title="Delete question"
        text="This question will be removed from the exam."
        loading={del.mutation.isPending}
        onCancel={() => del.setTarget(null)}
        onConfirm={() => del.target && del.mutation.mutate(del.target.id)}
      />
    </div>
  );
}