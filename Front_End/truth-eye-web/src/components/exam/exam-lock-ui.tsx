"use client";

import { useState } from "react";
import Link from "next/link";
import { Loader2, Lock, ShieldCheck } from "lucide-react";
import { LEAVE_TEXT, type ExamLeaveResult, type LeaveReason } from "@/lib/exam-lock";

/** Shown before the exam. The click on the button enters full screen and starts the lock. */
export function ExamStartGate({
  examTitle,
  onStart,
}: {
  examTitle?: string;
  onStart: () => Promise<{ ok: boolean; error?: string }>;
}) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  const go = async () => {
    setBusy(true);
    setError("");
    const r = await onStart();
    if (!r.ok) setError(r.error ?? "Couldn't start the exam.");
    setBusy(false);
  };

  return (
    <div className="mx-auto max-w-xl rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
      <div className="mb-5 flex h-14 w-14 items-center justify-center rounded-2xl bg-[#0A2540] text-teal-300">
        <ShieldCheck size={28} />
      </div>
      <h1 className="text-2xl font-bold text-[#0A2540]">{examTitle ?? "Before you start"}</h1>
      <p className="mt-2 text-sm text-slate-500">Read this carefully. It can't be undone once the exam starts.</p>

      <ul className="mt-5 space-y-2.5 text-sm text-slate-700">
        <li>• The exam opens in <b>full screen</b>. Stay in it until you submit.</li>
        <li>
          • <b>Switching tabs, opening another window, pressing Esc, minimizing, splitting the screen or reloading the page
          ends the exam immediately.</b>
        </li>
        <li>• Your saved answers (and the one on the screen) are graded, and you <b>can’t come back</b> to the exam.</li>
        <li>• Allow the camera <b>before</b> you press Start, and close other apps that may pop up.</li>
      </ul>

      {error && <p className="mt-4 rounded-xl bg-red-50 px-4 py-3 text-sm font-medium text-red-600">{error}</p>}

      <button
        onClick={go}
        disabled={busy}
        className="mt-6 flex w-full items-center justify-center gap-2 rounded-xl bg-teal-700 py-3.5 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:opacity-60"
      >
        {busy && <Loader2 size={18} className="animate-spin" />}
        I understand, start the exam
      </button>
    </div>
  );
}

/** Shown after the student left. The exam is over and can't be opened again. */
/** Shown after the student left. The exam is over and can't be opened again. */
export function ExamLeftScreen({
  reason,
  result,
  failed,
  onRetry,
}: {
  reason: LeaveReason;
  result: ExamLeaveResult | null;
  failed?: boolean;
  onRetry?: () => void;
}) {
  return (
    <div className="mx-auto max-w-lg rounded-2xl border border-red-100 bg-white p-8 text-center shadow-sm">
      <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-red-50 text-red-600">
        <Lock size={26} />
      </div>
      <h1 className="text-2xl font-bold text-[#0A2540]">Your exam has ended</h1>
      <p className="mt-2 text-sm text-slate-600">{LEAVE_TEXT[reason]}</p>
      <p className="mt-1 text-sm text-slate-500">
        The exam was submitted automatically with the answers you had so far. You can’t open it again.
      </p>

      <div className="mt-6 rounded-xl bg-slate-50 p-5">
        {result ? (
          <>
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Your result</p>
            <p className="mt-1 text-3xl font-bold text-[#0A2540]">
              {Number(result.totalScore.toFixed(1))} / {result.totalMarks}
            </p>
            <p className="text-sm text-slate-500">{result.percentage.toFixed(0)}%</p>
          </>
        ) : failed ? (
          <div className="space-y-3">
            <p className="text-sm text-slate-600">We couldn’t reach the server to save your result yet.</p>
            {onRetry && (
              <button
                onClick={onRetry}
                className="rounded-xl border border-slate-200 bg-white px-5 py-2 text-sm font-semibold text-slate-700 transition hover:bg-slate-100"
              >
                Try again
              </button>
            )}
          </div>
        ) : (
          <p className="flex items-center justify-center gap-2 text-sm text-slate-500">
            <Loader2 size={16} className="animate-spin" /> Saving your result...
          </p>
        )}
      </div>

      <div className="mt-6 flex flex-wrap items-center justify-center gap-3">
        {result && (
          <Link
            href={`/student/results/${result.sessionId}`}
            className="inline-flex rounded-xl bg-teal-700 px-6 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-800"
          >
            View full result
          </Link>
        )}
        <Link
          href="/student/exams"
          className="inline-flex rounded-xl border border-slate-200 px-6 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50"
        >
          Back to exams
        </Link>
      </div>
    </div>
  );
}