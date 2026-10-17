"use client";

import { use, useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMutation, useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  AlertTriangle, ArrowRight, Camera, CheckCircle2, Clock, FileQuestion, Loader2,
  Maximize, MonitorOff, ScanEye, ShieldAlert, WifiOff,
} from "lucide-react";
import { ErrorState } from "@/components/ui/states";
import { examTakeApi } from "@/lib/api/exam-take";
import { useProctoring } from "@/lib/proctoring/use-proctoring";
import { cn } from "@/lib/utils";
import type { SavedAnswer } from "@/types/exam-take";

const input =
  "w-full rounded-xl border border-slate-300 bg-white px-4 py-3 text-sm outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-4 focus:ring-teal-100";

function fmt(total: number) {
  const h = Math.floor(total / 3600);
  const m = Math.floor((total % 3600) / 60);
  const s = total % 60;
  const p = (n: number) => String(n).padStart(2, "0");
  return h ? `${h}:${p(m)}:${p(s)}` : `${p(m)}:${p(s)}`;
}

function useCountdown(deadline: number | null) {
  const [left, setLeft] = useState<number | null>(null);
  useEffect(() => {
    if (deadline == null) return;
    const tick = () => setLeft(Math.max(0, Math.ceil((deadline - Date.now()) / 1000)));
    tick();
    const id = setInterval(tick, 500);
    return () => clearInterval(id);
  }, [deadline]);
  return left;
}

export default function ExamPage({ params }: { params: Promise<{ sessionId: string }> }) {
  const { sessionId: raw } = use(params);
  const sessionId = Number(raw);
  const router = useRouter();

  const take = useQuery({
    queryKey: ["exam-take", sessionId],
    queryFn: () => examTakeApi.take(sessionId),
    staleTime: Infinity,
    gcTime: 0,
    retry: false,
    refetchOnReconnect: false,
  });

  const [phase, setPhase] = useState<"gate" | "running">("gate");
  const [gateError, setGateError] = useState("");
  const [starting, setStarting] = useState(false);
  const [stream, setStream] = useState<MediaStream | null>(null);
  const [index, setIndex] = useState(0);
  const [locked, setLocked] = useState<Record<number, SavedAnswer>>({});
  const [draft, setDraft] = useState<{ text: string; optionId: number | null }>({ text: "", optionId: null });
  const videoRef = useRef<HTMLVideoElement>(null);
  const initialised = useRef(false);
  const expiredHandled = useRef(false);

  const data = take.data;
  const questions = data?.questions ?? [];

  // Resume: jump to the first unanswered question
  useEffect(() => {
    if (!data || initialised.current) return;
    initialised.current = true;
    const map: Record<number, SavedAnswer> = {};
    data.answers.forEach((a) => (map[a.questionId] = a));
    setLocked(map);
    const firstOpen = data.questions.findIndex((q) => !map[q.id]);
    setIndex(firstOpen === -1 ? data.questions.length : firstOpen);
  }, [data]);

  // Deadline comes from the server's remaining time at load, not from the student's clock
  const deadline = useMemo(
    () => (data ? take.dataUpdatedAt + data.remainingSeconds * 1000 : null),
    [data, take.dataUpdatedAt]
  );
  const left = useCountdown(deadline);

  const cleanup = () => {
    stream?.getTracks().forEach((t) => t.stop());
    if (document.fullscreenElement) document.exitFullscreen().catch(() => {});
  };

  // Defined BEFORE useProctoring so proctoring can be switched off while submitting
  const submit = useMutation({
    mutationFn: () => examTakeApi.submit(sessionId),
    onSuccess: () => {
      toast.success("Exam submitted");
      router.replace(`/student/results/${sessionId}`);
      // Proctoring is already inactive (isPending/isSuccess), so leaving fullscreen is no longer recorded
      setTimeout(cleanup, 300);
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const { violations, fullscreenLost, face, offline } = useProctoring({
    sessionId,
    active: phase === "running" && !submit.isPending && !submit.isSuccess,
    faceEnabled: !!stream && !submit.isPending && !submit.isSuccess,
    videoRef,
  });

  // ---- Draft autosave (survives refresh; cleared once the answer is saved) ----
  const currentId = questions[index]?.id;
  const draftKey = (qid: number) => `te_draft:${sessionId}:${qid}`;

  useEffect(() => {
    if (currentId == null) return;
    try {
      const raw = sessionStorage.getItem(draftKey(currentId));
      setDraft(raw ? JSON.parse(raw) : { text: "", optionId: null });
    } catch {
      setDraft({ text: "", optionId: null });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentId]);

  const updateDraft = (patch: Partial<{ text: string; optionId: number | null }>) => {
    const next = { ...draft, ...patch };
    setDraft(next);
    if (currentId != null) {
      try {
        sessionStorage.setItem(draftKey(currentId), JSON.stringify(next));
      } catch {}
    }
  };

  // ---- Time warnings ----
  const warned = useRef({ ten: false, one: false });
  useEffect(() => {
    if (phase !== "running" || left == null || left === 0) return;
    if (left <= 60 && !warned.current.one) {
      warned.current.one = true;
      warned.current.ten = true;
      toast.error("Less than 1 minute left!", { duration: 8000 });
    } else if (left <= 600 && left > 60 && !warned.current.ten) {
      warned.current.ten = true;
      toast.warning(`${Math.ceil(left / 60)} minutes left`, { duration: 6000 });
    }
  }, [left, phase]);

  // Attach camera stream to the preview
  useEffect(() => {
    if (videoRef.current && stream) videoRef.current.srcObject = stream;
  }, [stream, phase]);

  const saveNext = useMutation({
    mutationFn: async () => {
      const q = questions[index];
      const answer = { questionId: q.id, answerText: draft.text, selectedOptionId: draft.optionId };
      const res = await examTakeApi.next(sessionId, q.id, answer);
      return { q, answer, res };
    },
    onSuccess: ({ q, answer, res }) => {
      try {
        sessionStorage.removeItem(draftKey(q.id));
      } catch {}
      setLocked((l) => ({ ...l, [q.id]: answer }));
      setDraft({ text: "", optionId: null });
      if (res.hasNext) setIndex((i) => i + 1);
      else {
        setIndex(questions.length);
        submit.mutate();
      }
    },
    onError: (e: Error) => toast.error(e.message),
  });

  // Auto-submit when time is up
  useEffect(() => {
    if (phase !== "running" || left !== 0 || expiredHandled.current) return;
    expiredHandled.current = true;
    toast.error("Time is up. Submitting your exam...");
    submit.mutate();
  }, [left, phase, submit]);

  // Warn before leaving mid-exam (not while/after submitting)
  useEffect(() => {
    if (phase !== "running" || submit.isPending || submit.isSuccess) return;
    const warn = (e: BeforeUnloadEvent) => e.preventDefault();
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [phase, submit.isPending, submit.isSuccess]);

  const begin = async () => {
    setGateError("");
    setStarting(true);
    let s: MediaStream | null = null;
    try {
      s = await navigator.mediaDevices.getUserMedia({ video: { width: 640, height: 480 }, audio: false });
      await document.documentElement.requestFullscreen();
      setStream(s);
      setPhase("running");
    } catch (e) {
      s?.getTracks().forEach((t) => t.stop());
      const name = (e as DOMException)?.name;
      setGateError(
        name === "NotAllowedError"
          ? "Camera or fullscreen permission was denied. Allow both and try again."
          : name === "NotFoundError"
          ? "No camera was found on this device."
          : "Couldn't start the secure exam mode. Try again."
      );
    } finally {
      setStarting(false);
    }
  };

  /* ---------- Loading / error ---------- */
  if (take.isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-[#F6F8FA]">
        <Loader2 className="animate-spin text-teal-700" size={32} />
      </div>
    );
  }
  if (take.isError || !data) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-[#F6F8FA] p-6">
        <div className="w-full max-w-lg space-y-5">
          <ErrorState title="Can’t open this exam" message={(take.error as Error)?.message} onRetry={() => take.refetch()} />
          <Link href="/student/exams" className="block text-center text-sm font-semibold text-teal-700 hover:text-teal-800">
            Back to exams
          </Link>
        </div>
      </div>
    );
  }

  /* ---------- Gate ---------- */
  if (phase === "gate") {
    const rules = [
      "Your camera must stay on for the whole exam.",
      "The exam runs in fullscreen. Leaving it is recorded.",
      "Switching tabs or windows is recorded.",
      "Copy, paste and right-click are disabled.",
      "Only one face should be visible in the camera.",
      "Each answer is final once you move to the next question.",
      "A camera snapshot is saved as evidence when a violation is detected.",
    ];
    return (
      <main className="flex min-h-screen items-center justify-center bg-[#F6F8FA] p-4">
        <div className="animate-fade-up w-full max-w-2xl overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-xl shadow-slate-200/60">
          <div className="relative bg-[#0A2540] p-8 text-white">
            <div className="pointer-events-none absolute -top-16 -end-10 h-56 w-56 rounded-full bg-teal-500/25 blur-3xl" />
            <div className="relative">
              <span className="inline-flex items-center gap-1.5 rounded-full border border-teal-400/30 bg-teal-400/10 px-3 py-1 text-xs font-semibold text-teal-200">
                <ScanEye size={14} /> Proctored exam
              </span>
              <h1 className="mt-4 text-2xl font-bold">{data.examTitle}</h1>
              <div className="mt-3 flex flex-wrap gap-4 text-sm text-slate-300">
                <span className="flex items-center gap-1.5"><FileQuestion size={16} /> {questions.length} questions</span>
                <span className="flex items-center gap-1.5"><Clock size={16} /> {fmt(data.remainingSeconds)} remaining</span>
              </div>
            </div>
          </div>

          <div className="space-y-6 p-8">
            {data.instructions && (
              <div className="rounded-2xl bg-slate-50 p-4 text-sm text-slate-600">
                <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-400">Instructions</p>
                {data.instructions}
              </div>
            )}

            <ul className="grid gap-2.5 sm:grid-cols-2">
              {rules.map((r) => (
                <li key={r} className="flex items-start gap-2 text-sm text-slate-600">
                  <CheckCircle2 size={17} className="mt-0.5 shrink-0 text-teal-600" /> {r}
                </li>
              ))}
            </ul>

            <div className="flex items-start gap-3 rounded-2xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
              <AlertTriangle size={18} className="mt-0.5 shrink-0" />
              <p>The timer already started when you pressed “Start exam”. Begin now to avoid losing time.</p>
            </div>

            {gateError && <p className="rounded-xl bg-red-50 px-4 py-3 text-sm font-medium text-red-600">{gateError}</p>}

            <button
              onClick={begin}
              disabled={starting}
              className="flex w-full items-center justify-center gap-2 rounded-xl bg-teal-600 py-3.5 font-semibold text-white shadow-lg shadow-teal-900/10 transition hover:bg-teal-700 active:scale-[0.98] disabled:opacity-60"
            >
              {starting ? <Loader2 size={18} className="animate-spin" /> : <Camera size={18} />}
              Enable camera & begin
            </button>
          </div>
        </div>
      </main>
    );
  }

  /* ---------- Running ---------- */
  const total = questions.length;
  const finished = index >= total;
  const q = questions[index];
  const isMcq = !!q && (q.options?.length ?? 0) > 0;
  const isLast = index === total - 1;
  const canSave = isMcq ? draft.optionId != null : draft.text.trim().length > 0;
  const answered = Object.keys(locked).length;
  const lowTime = left != null && left <= 300;
  const busy = saveNext.isPending || submit.isPending;

  const faceBadge = {
    off: null,
    loading: { text: "Starting camera check...", cls: "bg-slate-800 text-slate-200" },
    ok: { text: "Face detected", cls: "bg-emerald-600 text-white" },
    none: { text: "No face detected", cls: "bg-red-600 text-white" },
    multiple: { text: "Multiple faces", cls: "bg-red-600 text-white" },
    unavailable: { text: "Face check unavailable", cls: "bg-amber-500 text-white" },
  }[face];

  return (
    <div className="min-h-screen select-none bg-[#F6F8FA]">
      <header className="sticky top-0 z-20 border-b border-slate-200 bg-white/90 backdrop-blur">
        <div className="mx-auto flex h-16 max-w-4xl items-center justify-between gap-4 px-4">
          <div className="min-w-0">
            <p className="truncate font-bold text-[#0A2540]">{data.examTitle}</p>
            <p className="text-xs text-slate-500">{answered} of {total} answered</p>
          </div>
          <div className="flex items-center gap-3">
            <span
              className={cn(
                "flex items-center gap-1.5 rounded-full px-3 py-1.5 text-xs font-semibold",
                violations ? "bg-red-50 text-red-600" : "bg-slate-100 text-slate-500"
              )}
              title="Recorded violations"
            >
              <ShieldAlert size={14} /> {violations}
            </span>
            <span
              className={cn(
                "flex items-center gap-2 rounded-xl px-4 py-2 font-mono text-lg font-bold tabular-nums",
                lowTime ? "animate-pulse bg-red-600 text-white" : "bg-[#0A2540] text-teal-300"
              )}
            >
              <Clock size={18} /> {left == null ? "--:--" : fmt(left)}
            </span>
          </div>
        </div>
        <div className="h-1 bg-slate-100">
          <div className="h-full bg-teal-500 transition-all duration-500" style={{ width: `${total ? (answered / total) * 100 : 0}%` }} />
        </div>
      </header>

      <main className="mx-auto max-w-4xl space-y-5 p-4 pb-32 sm:p-8">
        {offline && (
          <div className="flex items-start gap-3 rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            <WifiOff size={18} className="mt-0.5 shrink-0" />
            <p>You&apos;re offline. Your answers can&apos;t be saved until the connection returns. What you typed is kept on this device, and the timer keeps running.</p>
          </div>
        )}

        {/* read-only progress dots */}
        <div className="flex flex-wrap gap-1.5">
          {questions.map((x, i) => (
            <span
              key={x.id}
              className={cn(
                "flex h-8 w-8 items-center justify-center rounded-lg text-xs font-bold",
                locked[x.id] ? "bg-teal-600 text-white" : i === index ? "bg-[#0A2540] text-teal-300" : "bg-white text-slate-400 ring-1 ring-slate-200"
              )}
            >
              {i + 1}
            </span>
          ))}
        </div>

        {finished ? (
          <div className="animate-fade-up rounded-3xl border border-slate-200 bg-white p-10 text-center">
            <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-teal-50 text-teal-600">
              <CheckCircle2 size={32} />
            </div>
            <h2 className="text-xl font-bold text-[#0A2540]">All questions answered</h2>
            <p className="mt-1 text-sm text-slate-500">Submit your exam to see your result.</p>
            <button
              onClick={() => submit.mutate()}
              disabled={busy}
              className="mx-auto mt-6 flex items-center gap-2 rounded-xl bg-teal-600 px-8 py-3 font-semibold text-white transition hover:bg-teal-700 disabled:opacity-60"
            >
              {submit.isPending && <Loader2 size={18} className="animate-spin" />}
              {submit.isPending ? "Grading..." : "Submit exam"}
            </button>
          </div>
        ) : (
          <section key={q.id} className="animate-fade-up rounded-3xl border border-slate-200 bg-white p-6 sm:p-8">
            <div className="mb-4 flex items-center justify-between">
              <span className="rounded-full bg-[#0A2540] px-3 py-1 text-xs font-bold text-teal-300">
                Question {index + 1} / {total}
              </span>
              <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">
                {q.marks} {q.marks === 1 ? "mark" : "marks"}
              </span>
            </div>

            <h2 className="whitespace-pre-wrap text-lg font-semibold leading-relaxed text-[#0A2540]">{q.questionText}</h2>

            <div className="mt-6">
              {isMcq ? (
                <ul className="space-y-3">
                  {q.options!.map((o, i) => {
                    const on = draft.optionId === o.id;
                    return (
                      <li key={o.id}>
                        <button
                          type="button"
                          onClick={() => updateDraft({ optionId: o.id })}
                          className={cn(
                            "flex w-full items-center gap-3 rounded-2xl border-2 px-4 py-3.5 text-start text-sm font-medium transition",
                            on ? "border-teal-600 bg-teal-50 text-teal-900" : "border-slate-200 text-slate-700 hover:border-teal-300 hover:bg-slate-50"
                          )}
                        >
                          <span className={cn("flex h-7 w-7 shrink-0 items-center justify-center rounded-lg text-xs font-bold", on ? "bg-teal-600 text-white" : "bg-slate-100 text-slate-500")}>
                            {String.fromCharCode(65 + i)}
                          </span>
                          {o.optionText}
                        </button>
                      </li>
                    );
                  })}
                </ul>
              ) : (
                <textarea
                  rows={8}
                  value={draft.text}
                  onChange={(e) => updateDraft({ text: e.target.value })}
                  className={cn(input, "select-text resize-y")}
                  placeholder="Type your answer here..."
                />
              )}
            </div>

            <div className="mt-8 flex items-center justify-between gap-4">
              <p className="text-xs text-slate-400">Answers can’t be changed after you continue.</p>
              <button
                onClick={() => saveNext.mutate()}
                disabled={busy || offline}
                className={cn(
                  "flex items-center gap-2 rounded-xl px-6 py-3 text-sm font-semibold transition active:scale-[0.98] disabled:opacity-60",
                  canSave ? "bg-teal-600 text-white shadow-lg shadow-teal-900/10 hover:bg-teal-700" : "bg-slate-100 text-slate-500 hover:bg-slate-200"
                )}
              >
                {busy ? <Loader2 size={16} className="animate-spin" /> : <ArrowRight size={16} />}
                {submit.isPending
                  ? "Grading..."
                  : canSave
                  ? isLast ? "Save & submit" : "Save & next"
                  : isLast ? "Skip & submit" : "Skip"}
              </button>
            </div>
          </section>
        )}
      </main>

      {/* camera preview */}
      <div className="fixed bottom-4 end-4 z-30 w-28 overflow-hidden rounded-2xl border-2 border-white bg-slate-900 shadow-xl sm:w-44">
        <video ref={videoRef} autoPlay muted playsInline className="aspect-[4/3] w-full -scale-x-100 object-cover" />
        {faceBadge && <div className={cn("px-2 py-1 text-center text-[11px] font-semibold", faceBadge.cls)}>{faceBadge.text}</div>}
      </div>

      {/* fullscreen lock */}
      {fullscreenLost && !submit.isPending && !submit.isSuccess && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-[#0A2540]/95 p-6 backdrop-blur">
          <div className="animate-pop max-w-md text-center text-white">
            <div className="mx-auto mb-5 flex h-16 w-16 items-center justify-center rounded-2xl bg-red-500/20 text-red-300">
              <MonitorOff size={32} />
            </div>
            <h2 className="text-2xl font-bold">You left fullscreen</h2>
            <p className="mt-2 text-slate-300">This was recorded. The timer is still running. Return to fullscreen to continue.</p>
            <button
              onClick={() => document.documentElement.requestFullscreen().catch(() => {})}
              className="mx-auto mt-6 flex items-center gap-2 rounded-xl bg-teal-500 px-6 py-3 font-semibold text-white transition hover:bg-teal-400"
            >
              <Maximize size={18} /> Return to fullscreen
            </button>
          </div>
        </div>
      )}
    </div>
  );
}