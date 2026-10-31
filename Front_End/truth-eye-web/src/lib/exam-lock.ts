"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { api } from "@/lib/api/client";
import { ensureFreshToken } from "@/lib/auth/refresh";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5210";

export type LeaveReason =
  | "tab-hidden"
  | "window-blur"
  | "fullscreen-exit"
  | "split-screen"
  | "page-leave"
  | "session-ended";

export const LEAVE_TEXT: Record<LeaveReason, string> = {
  "tab-hidden": "You switched to another tab or minimized the browser.",
  "window-blur": "You left the exam window.",
  "fullscreen-exit": "You exited full screen.",
  "split-screen": "The screen was split or the window was resized.",
  "page-leave": "You left or reloaded the exam page.",
  "session-ended": "This exam session has already ended.",
};

/** What the student had on the screen (and had not saved yet) at the moment they left */
export interface LeaveAnswer {
  questionId: number;
  selectedOptionId?: number | null;
  answerText?: string | null;
}

export interface ExamLeaveResult {
  sessionId: number;
  examTitle: string;
  totalScore: number;
  totalMarks: number;
  percentage: number;
  isPassed: boolean;
  correctAnswers: number;
  totalQuestions: number;
}

interface Terminated {
  reason: LeaveReason;
  result: ExamLeaveResult | null;
  failed?: boolean; // the server couldn't be reached
}

const markerKey = (sessionId: number) => `te-exam-left-${sessionId}`;

function leaveFullscreen() {
  try {
    if (document.fullscreenElement) void document.exitFullscreen().catch(() => {});
  } catch {
    /* ignore */
  }
}

/** True when the window doesn't fill the screen (a snapped / split window) */
function looksSplit(): boolean {
  const sw = window.screen.width;
  const sh = window.screen.height;
  const w = Math.max(window.outerWidth || 0, window.innerWidth);
  const h = Math.max(window.outerHeight || 0, window.innerHeight);
  if (document.fullscreenElement) return w < sw * 0.98 || h < sh * 0.98;
  return w < sw * 0.9;
}

/**
 * Locks the student into the exam. Once started, leaving it in any way (another tab, another window,
 * leaving full screen, a split screen, a reload, a link inside the app) ends the exam:
 * what was answered so far is graded and the student can't open the exam again.
 */
export function useExamLock(opts: {
  sessionId: number;
  /** return the answer currently on the screen (if any) so it is counted too */
  getCurrentAnswer?: () => LeaveAnswer | null;
  onTerminated?: (reason: LeaveReason, result: ExamLeaveResult | null) => void;
  isSuspended?: () => boolean;
}) {
  const [armed, setArmed] = useState(false);
  const [terminated, setTerminated] = useState<Terminated | null>(null);

  const armedRef = useRef(false);
  const doneRef = useRef(false);
  const needFullscreen = useRef(false);
  const tokenRef = useRef<string | null>(null);
  const reasonRef = useRef<LeaveReason | null>(null);
  const optsRef = useRef(opts);

  useEffect(() => {
    optsRef.current = opts;
  });

  // Tells the server. A keepalive request still finishes while the page is closing.
  const report = useCallback(async (reason: LeaveReason) => {
    const { sessionId, getCurrentAnswer, onTerminated } = optsRef.current;
    reasonRef.current = reason;

    try {
      sessionStorage.setItem(markerKey(sessionId), reason); // so a failed request is retried on the next visit
    } catch {
      /* ignore */
    }

    let answer: LeaveAnswer | null = null;
    try {
      answer = getCurrentAnswer?.() ?? null;
    } catch {
      /* ignore */
    }

    try {
      const token = tokenRef.current ?? (await ensureFreshToken());
      const res = await fetch(`${BASE_URL}/api/ExamGuard/${sessionId}/terminate`, {
        method: "POST",
        keepalive: true,
        headers: {
          "Content-Type": "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({
          reason,
          currentAnswer: answer
            ? {
                questionId: answer.questionId,
                selectedOptionId: answer.selectedOptionId ?? null,
                answerText: answer.answerText ?? null,
              }
            : null,
        }),
      });
      const body = await res.json().catch(() => null);
      if (res.ok && body?.success !== false) {
        try {
          sessionStorage.removeItem(markerKey(sessionId));
        } catch {
          /* ignore */
        }
        const result = (body?.data ?? null) as ExamLeaveResult | null;
        setTerminated({ reason, result });
        onTerminated?.(reason, result);
        return;
      }
    } catch {
      /* offline: the marker stays and it is retried */
    }

    setTerminated({ reason, result: null, failed: true });
    onTerminated?.(reason, null);
  }, []);

  const terminate = useCallback(
    (reason: LeaveReason) => {
      if (!armedRef.current || doneRef.current || optsRef.current.isSuspended?.()) return;
      doneRef.current = true;
      setTerminated({ reason, result: null }); // lock the screen immediately
      leaveFullscreen();
      void report(reason);
    },
    [report]
  );

  /** Call this from the click on the "Start" button (full screen needs a click) */
  const start = useCallback(async (): Promise<{ ok: boolean; error?: string }> => {
    if (armedRef.current) return { ok: true };

    let fullscreen = false;
    const el = document.documentElement;
    if (typeof el.requestFullscreen === "function") {
      try {
        await el.requestFullscreen();
        fullscreen = true;
      } catch {
        return { ok: false, error: "Couldn't enter full screen. Allow it and try again." };
      }
    }

    await new Promise((r) => setTimeout(r, 500)); // let the window settle

    if (fullscreen && !document.fullscreenElement) {
      return { ok: false, error: "Full screen was closed. Try again." };
    }
    if (looksSplit()) {
      leaveFullscreen();
      return {
        ok: false,
        error: "The window doesn't fill the screen. Close any split or snapped windows and try again.",
      };
    }

    tokenRef.current = (await ensureFreshToken()) ?? null;
    needFullscreen.current = fullscreen;
    armedRef.current = true;
    setArmed(true);
    return { ok: true };
  }, []);

  /** Call this right after a normal submit, before leaving the page */
  const release = useCallback(() => {
    doneRef.current = true;
    armedRef.current = false;
    setArmed(false);
    try {
      sessionStorage.removeItem(markerKey(optsRef.current.sessionId));
    } catch {
      /* ignore */
    }
    leaveFullscreen();
  }, []);

  // The student already left earlier but the server never heard about it: report it again
  useEffect(() => {
    let reason: string | null = null;
    try {
      reason = sessionStorage.getItem(markerKey(opts.sessionId));
    } catch {
      /* ignore */
    }
    if (reason && !doneRef.current) {
      doneRef.current = true;
      setTerminated({ reason: reason as LeaveReason, result: null });
      void report(reason as LeaveReason);
    }
  }, [opts.sessionId, report]);

  // The watch itself
  useEffect(() => {
    if (!armed) return;
    const sessionId = optsRef.current.sessionId;

    const onVisibility = () => {
      if (document.hidden) terminate("tab-hidden");
    };
    const onBlur = () => terminate("window-blur");
    const onFullscreen = () => {
      if (needFullscreen.current && !document.fullscreenElement) terminate("fullscreen-exit");
    };
    let resizeTimer: ReturnType<typeof setTimeout> | undefined;
    const onResize = () => {
      clearTimeout(resizeTimer);
      resizeTimer = setTimeout(() => {
        if (looksSplit()) terminate("split-screen");
      }, 350);
    };
    const onLeave = () => terminate("page-leave");

    document.addEventListener("visibilitychange", onVisibility);
    window.addEventListener("blur", onBlur);
    document.addEventListener("fullscreenchange", onFullscreen);
    window.addEventListener("resize", onResize);
    window.addEventListener("pagehide", onLeave);

    // tells the server "still here" and keeps the token fresh for the moment the page closes
    const beat = async () => {
      try {
        tokenRef.current = (await ensureFreshToken()) ?? tokenRef.current;
        const r = await api<{ status: string }>(`/api/ExamGuard/${sessionId}/heartbeat`, { method: "POST" });
        if (r.status !== "InProgress") terminate("session-ended");
      } catch {
        /* offline for a moment: the server decides later */
      }
    };
    void beat();
    const timer = setInterval(beat, 5000);

    return () => {
      clearInterval(timer);
      clearTimeout(resizeTimer);
      document.removeEventListener("visibilitychange", onVisibility);
      window.removeEventListener("blur", onBlur);
      document.removeEventListener("fullscreenchange", onFullscreen);
      window.removeEventListener("resize", onResize);
      window.removeEventListener("pagehide", onLeave);

      // the page was closed from inside the app (a link, the back button) without finishing
      terminate("page-leave");
    };
  }, [armed, terminate]);

  /** Tries again to tell the server (after a failed attempt) */
  const retry = useCallback(() => {
    const reason = reasonRef.current;
    if (!reason) return;
    setTerminated({ reason, result: null });
    void report(reason);
  }, [report]);

  return { armed, terminated, start, release, retry };
}