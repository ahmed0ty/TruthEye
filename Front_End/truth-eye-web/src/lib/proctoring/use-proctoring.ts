"use client";

import { useCallback, useEffect, useRef, useState, type RefObject } from "react";
import { toast } from "sonner";
import { examTakeApi } from "@/lib/api/exam-take";
import { uploadEvidence } from "@/lib/api/evidence";
import { settingsApi } from "@/lib/api/settings";

const FACE_MODEL =
  "https://storage.googleapis.com/mediapipe-models/face_detector/blaze_face_short_range/float16/1/blaze_face_short_range.tflite";

// Phone detection. To use your own trained model later, change PHONE_MODEL only.
const PHONE_MODEL = "/mediapipe/efficientdet_lite0.tflite";
// Category names the model may use: "cell phone" (COCO model) or "phone" (your own model)
const PHONE_LABELS = ["cell phone", "phone"];
const PHONE_SCORE = 0.5; // lower = catches more, but more false alarms

// Defaults used until the server settings arrive (or if they can't be loaded)
const DEFAULT_NO_FACE_SECONDS = 3;
const DEFAULT_MULTIPLE_FACES_SECONDS = 2;

export type ProctorEvent =
  | "TabSwitch"
  | "WindowBlur"
  | "FullscreenExit"
  | "CopyPaste"
  | "ContextMenu"
  | "NoFace"
  | "MultipleFaces"
  | "ConnectionLost"
  | "PhoneDetected";

const LABEL: Record<ProctorEvent, string> = {
  TabSwitch: "You left the exam tab. This was recorded.",
  WindowBlur: "The exam window lost focus. This was recorded.",
  FullscreenExit: "You exited fullscreen. This was recorded.",
  CopyPaste: "Copy / paste is disabled during the exam.",
  ContextMenu: "Right-click is disabled during the exam.",
  NoFace: "We can't see your face. Stay in front of the camera.",
  MultipleFaces: "More than one face was detected.",
  ConnectionLost: "Your internet connection dropped. This was recorded.",
  PhoneDetected: "A phone was detected. This was recorded.",
};

// Events that save a camera snapshot as evidence
const WITH_EVIDENCE: ProctorEvent[] = ["NoFace", "MultipleFaces", "TabSwitch", "FullscreenExit", "PhoneDetected"];
const MAX_EVIDENCE = 25;

// Camera-based events repeat at most once per 15 seconds; the rest once per 3 seconds
const SLOW_EVENTS: ProctorEvent[] = ["NoFace", "MultipleFaces", "PhoneDetected"];

export type FaceStatus = "off" | "loading" | "ok" | "none" | "multiple" | "unavailable";

interface Options {
  sessionId: number;
  active: boolean;
  faceEnabled: boolean;
  videoRef: RefObject<HTMLVideoElement | null>;
}

function captureFrame(video: HTMLVideoElement | null): Promise<Blob | null> {
  if (!video || video.readyState < 2 || !video.videoWidth) return Promise.resolve(null);
  const w = 480;
  const h = Math.round((video.videoHeight / video.videoWidth) * w);
  const canvas = document.createElement("canvas");
  canvas.width = w;
  canvas.height = h;
  canvas.getContext("2d")!.drawImage(video, 0, 0, w, h);
  return new Promise((resolve) => canvas.toBlob((b) => resolve(b), "image/jpeg", 0.6));
}

export function useProctoring({ sessionId, active, faceEnabled, videoRef }: Options) {
  const [violations, setViolations] = useState(0);
  const [fullscreenLost, setFullscreenLost] = useState(false);
  const [face, setFace] = useState<FaceStatus>("off");
  const [offline, setOffline] = useState(false);

  const last = useRef<Partial<Record<ProctorEvent, number>>>({});
  const evidenceCount = useRef(0);

  // How many seconds (checks, one per second) a missing / extra face must last before it is reported.
  // A ref, so the running detector always reads the latest values without being restarted.
  const limits = useRef({ none: DEFAULT_NO_FACE_SECONDS, multi: DEFAULT_MULTIPLE_FACES_SECONDS });

  const report = useCallback(
    (type: ProctorEvent, confidence = 1) => {
      const now = Date.now();
      const gap = SLOW_EVENTS.includes(type) ? 15_000 : 3_000;
      if (now - (last.current[type] ?? 0) < gap) return;
      last.current[type] = now;

      setViolations((v) => v + 1);
      toast.warning(LABEL[type]);

      const wantsImage = WITH_EVIDENCE.includes(type) && evidenceCount.current < MAX_EVIDENCE;

      (async () => {
        try {
          const blob = wantsImage ? await captureFrame(videoRef.current) : null;
          if (blob) {
            evidenceCount.current++;
            try {
              await uploadEvidence(sessionId, type, confidence, blob);
              return;
            } catch {
              /* fall back to a plain log below */
            }
          }
          await examTakeApi.logEvent(sessionId, type, confidence);
        } catch {
          /* never block the exam because logging failed */
        }
      })();
    },
    [sessionId, videoRef]
  );

  // Load the proctoring settings from the server (and refresh them every minute while the exam is open)
  useEffect(() => {
    if (!active) return;
    let cancelled = false;

    const load = async () => {
      try {
        const s = await settingsApi.proctoring();
        if (cancelled) return;
        limits.current = {
          none: Math.max(2, Math.round(s.noFaceSeconds || DEFAULT_NO_FACE_SECONDS)),
          multi: Math.max(2, Math.round(s.multipleFacesSeconds || DEFAULT_MULTIPLE_FACES_SECONDS)),
        };
      } catch {
        /* keep the defaults: the exam never depends on this request */
      }
    };

    load();
    const id = setInterval(load, 60_000);
    return () => {
      cancelled = true;
      clearInterval(id);
    };
  }, [active]);

  // Browser behaviour listeners
  useEffect(() => {
    if (!active) return;

    const onVisibility = () => document.hidden && report("TabSwitch");
    const onBlur = () => {
      setTimeout(() => {
        if (!document.hidden) report("WindowBlur");
      }, 200);
    };
    const onFullscreen = () => {
      if (document.fullscreenElement) setFullscreenLost(false);
      else {
        setFullscreenLost(true);
        report("FullscreenExit");
      }
    };
    const block = (type: ProctorEvent) => (e: Event) => {
      e.preventDefault();
      report(type);
    };
    const onCopy = block("CopyPaste");
    const onCtx = block("ContextMenu");

    document.addEventListener("visibilitychange", onVisibility);
    window.addEventListener("blur", onBlur);
    document.addEventListener("fullscreenchange", onFullscreen);
    document.addEventListener("copy", onCopy);
    document.addEventListener("cut", onCopy);
    document.addEventListener("paste", onCopy);
    document.addEventListener("contextmenu", onCtx);

    return () => {
      document.removeEventListener("visibilitychange", onVisibility);
      window.removeEventListener("blur", onBlur);
      document.removeEventListener("fullscreenchange", onFullscreen);
      document.removeEventListener("copy", onCopy);
      document.removeEventListener("cut", onCopy);
      document.removeEventListener("paste", onCopy);
      document.removeEventListener("contextmenu", onCtx);
    };
  }, [active, report]);

  // Internet connection: warn when it drops, record the event once it returns
  // (the log request can't reach the server while the student is offline)
  useEffect(() => {
    if (!active) return;
    setOffline(!navigator.onLine);

    const onOffline = () => {
      setOffline(true);
      toast.error("You're offline. Reconnect to keep saving your answers.", { duration: 8000 });
    };
    const onOnline = () => {
      setOffline(false);
      report("ConnectionLost", 0.5);
    };

    window.addEventListener("offline", onOffline);
    window.addEventListener("online", onOnline);
    return () => {
      window.removeEventListener("offline", onOffline);
      window.removeEventListener("online", onOnline);
    };
  }, [active, report]);

  // Face detection (runs fully in the browser)
  useEffect(() => {
    if (!active || !faceEnabled) {
      setFace("off");
      return;
    }
    let cancelled = false;
    let timer: ReturnType<typeof setInterval> | undefined;
    let detector: import("@mediapipe/tasks-vision").FaceDetector | null = null;
    let none = 0;
    let multi = 0;

    setFace("loading");

    (async () => {
      try {
        const { FilesetResolver, FaceDetector } = await import("@mediapipe/tasks-vision");
        const vision = await FilesetResolver.forVisionTasks("/mediapipe/wasm");
        const make = (delegate: "GPU" | "CPU") =>
          FaceDetector.createFromOptions(vision, {
            baseOptions: { modelAssetPath: FACE_MODEL, delegate },
            runningMode: "VIDEO",
          });
        try {
          detector = await make("GPU");
        } catch {
          detector = await make("CPU");
        }
        if (cancelled) {
          detector.close();
          return;
        }
        setFace("ok");

        timer = setInterval(() => {
          const video = videoRef.current;
          if (!detector || !video || video.readyState < 2) return;
          const n = detector.detectForVideo(video, performance.now()).detections.length;

          if (n === 0) {
            multi = 0;
            none++;
            if (none >= limits.current.none) {
              setFace("none");
              report("NoFace", 0.9);
            }
          } else if (n > 1) {
            none = 0;
            multi++;
            if (multi >= limits.current.multi) {
              setFace("multiple");
              report("MultipleFaces", 0.9);
            }
          } else {
            none = 0;
            multi = 0;
            setFace("ok");
          }
        }, 1000);
      } catch {
        if (!cancelled) setFace("unavailable");
      }
    })();

    return () => {
      cancelled = true;
      if (timer) clearInterval(timer);
      detector?.close();
    };
  }, [active, faceEnabled, report, videoRef]);

  // Phone detection (separate from the face check, so a failure here never affects it)
  useEffect(() => {
    if (!active || !faceEnabled) return;

    let cancelled = false;
    let timer: ReturnType<typeof setInterval> | undefined;
    let detector: import("@mediapipe/tasks-vision").ObjectDetector | null = null;
    let hits = 0;

    (async () => {
      try {
        const { FilesetResolver, ObjectDetector } = await import("@mediapipe/tasks-vision");
        const vision = await FilesetResolver.forVisionTasks("/mediapipe/wasm");
        detector = await ObjectDetector.createFromOptions(vision, {
          baseOptions: { modelAssetPath: PHONE_MODEL, delegate: "CPU" },
          runningMode: "VIDEO",
          scoreThreshold: PHONE_SCORE,
          maxResults: 5,
        });
        if (cancelled) {
          detector.close();
          return;
        }

        timer = setInterval(() => {
          const video = videoRef.current;
          if (!detector || !video || video.readyState < 2) return;

          const result = detector.detectForVideo(video, performance.now());
          let best = 0;
          for (const d of result.detections)
            for (const c of d.categories)
              if (PHONE_LABELS.includes(c.categoryName) && c.score > best) best = c.score;

          if (process.env.NODE_ENV === "development") console.debug("phone score:", best.toFixed(2));

          // The phone must be seen in two checks in a row (about 4 seconds) before it is reported
          if (best >= PHONE_SCORE) {
            hits++;
            if (hits >= 2) report("PhoneDetected", best);
          } else {
            hits = 0;
          }
        }, 2000);
      } catch (e) {
        // Missing model file or unsupported browser: the exam continues without phone detection
        console.warn("Phone detection unavailable:", e);
      }
    })();

    return () => {
      cancelled = true;
      if (timer) clearInterval(timer);
      detector?.close();
    };
  }, [active, faceEnabled, report, videoRef]);

  return { violations, fullscreenLost, face, offline };
}