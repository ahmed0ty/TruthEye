"use client";

import { useEffect, useState, type CSSProperties, type ReactNode } from "react";

const STEPS = ["Initializing secure session", "Securing the connection", "Preparing the sign-in gateway"];

// Opening timeline (ms)
const OPEN_DELAY = 450; // content fades + the seam lights up, then the doors start moving
const OPEN_DUR = 1900; // how long the doors take to part
const OPEN_MS = OPEN_DELAY + OPEN_DUR + 150;
const OPEN_MS_CALM = 900;

const SEEN_KEY = "te_intro_seen";

// false = the intro plays on every visit. Set to true to show it once per browser session.
const SHOW_ONCE_PER_SESSION = false;

// true  = respect the OS "reduce motion" setting (the gate simply fades away)
// false = always play the full sliding-doors animation
const RESPECT_REDUCED_MOTION = false;

type Phase = "loading" | "opening" | "done";

function IntroContent({ step, open }: { step: number; open: boolean }) {
  return (
    <div
      className="absolute inset-0 grid place-items-center px-6"
      style={{
        opacity: open ? 0 : 1,
        transform: open ? "scale(.94)" : "none",
        transition: "opacity .55s ease-in, transform .55s ease-in",
      }}
    >
      <div className="pointer-events-none absolute h-80 w-80 rounded-full bg-teal-500/20 blur-3xl" style={{ animation: "ig-glow 3s ease-in-out infinite" }} />

      <div className="relative flex flex-col items-center">
        <svg aria-hidden viewBox="0 0 200 200" className="h-56 w-56 sm:h-64 sm:w-64">
          {/* dial ticks */}
          <g style={{ animation: "ig-fade-op 1s .2s both" }}>
            {Array.from({ length: 60 }, (_, i) => (
              <line
                key={i}
                x1="100"
                y1="4"
                x2="100"
                y2={i % 5 === 0 ? 13 : 9}
                transform={`rotate(${i * 6} 100 100)`}
                stroke={i % 5 === 0 ? "#5EEAD4" : "#ffffff"}
                strokeOpacity={i % 5 === 0 ? 0.55 : 0.18}
                strokeWidth="1"
              />
            ))}
          </g>

          {/* rings */}
          <circle cx="100" cy="100" r="84" pathLength={1} className="ig-stroke" stroke="#ffffff" strokeOpacity=".18" strokeWidth="1" />
          <circle cx="100" cy="100" r="68" pathLength={1} className="ig-stroke" stroke="#ffffff" strokeOpacity=".28" strokeWidth="1" style={{ animationDelay: ".2s" }} />
          <circle cx="100" cy="100" r="52" pathLength={1} className="ig-stroke" stroke="#5EEAD4" strokeOpacity=".55" strokeWidth="1" style={{ animationDelay: ".4s" }} />

          {/* scanning arc */}
          <circle
            cx="100"
            cy="100"
            r="76"
            pathLength={1}
            fill="none"
            stroke="#5EEAD4"
            strokeWidth="2"
            strokeLinecap="round"
            strokeDasharray="0.14 0.86"
            strokeOpacity=".9"
            className="ig-sweep"
          />

          {/* shield */}
          <path
            d="M100 62 L134 75 V104 C134 126 119 140 100 148 C81 140 66 126 66 104 V75 Z"
            fill="rgba(20,184,166,.16)"
            className="ig-fillin"
          />
          <path
            d="M100 62 L134 75 V104 C134 126 119 140 100 148 C81 140 66 126 66 104 V75 Z"
            pathLength={1}
            className="ig-stroke"
            stroke="#5EEAD4"
            strokeWidth="3"
            strokeLinejoin="round"
            style={{ animationDelay: ".9s", animationDuration: "1s" }}
          />
          <path
            d="M86 104 L96 114 L116 92"
            pathLength={1}
            className="ig-stroke"
            stroke="#ffffff"
            strokeWidth="4"
            strokeLinecap="round"
            strokeLinejoin="round"
            style={{ animationDelay: "1.8s", animationDuration: ".5s" }}
          />
        </svg>

        <h1 className="mt-6 text-3xl font-bold tracking-tight text-white">
          Truth<span className="text-teal-300">Eye</span>
        </h1>
        <p className="mt-1 text-sm text-slate-300">Secure Exam Proctoring Platform</p>

        <div className="mt-8 h-1 w-56 overflow-hidden rounded-full bg-white/10">
          <div className="ig-bar h-full rounded-full bg-gradient-to-r from-teal-500 to-teal-300" />
        </div>

        <p key={step} className="ig-step mt-4 h-5 text-xs font-medium tracking-wide text-teal-200/90">
          {STEPS[step]}
        </p>

        <div className="mt-3 flex gap-1.5">
          {STEPS.map((_, i) => (
            <span
              key={i}
              className={`h-1.5 w-1.5 rounded-full transition-colors duration-300 ${i <= step ? "bg-teal-300" : "bg-white/20"}`}
            />
          ))}
        </div>
      </div>
    </div>
  );
}

export function IntroGate({ children }: { children: ReactNode }) {
  const [phase, setPhase] = useState<Phase>("loading");
  const [open, setOpen] = useState(false); // flips to true two frames AFTER "opening" starts
  const [step, setStep] = useState(0);
  const [calm, setCalm] = useState(false);

  const loadMs = calm ? 1800 : 2800;
  const openMs = calm ? OPEN_MS_CALM : OPEN_MS;

  // Loading sequence
  useEffect(() => {
    let seen = false;
    if (SHOW_ONCE_PER_SESSION) {
      try {
        seen = sessionStorage.getItem(SEEN_KEY) === "1";
      } catch {}
    }
    if (seen) {
      setPhase("done");
      return;
    }

    const reduce = RESPECT_REDUCED_MOTION && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    setCalm(reduce);
    const total = reduce ? 1800 : 2800;

    const t1 = setTimeout(() => setStep(1), total * 0.32);
    const t2 = setTimeout(() => setStep(2), total * 0.64);
    const t3 = setTimeout(() => setPhase((p) => (p === "loading" ? "opening" : p)), total);
    return () => [t1, t2, t3].forEach(clearTimeout);
  }, []);

  // Let the browser paint the CLOSED gate first, then flip `open` so the CSS transitions really run
  useEffect(() => {
    if (phase !== "opening") return;
    let r2 = 0;
    const r1 = requestAnimationFrame(() => {
      r2 = requestAnimationFrame(() => setOpen(true));
    });
    return () => {
      cancelAnimationFrame(r1);
      cancelAnimationFrame(r2);
    };
  }, [phase]);

  // Hide the page scrollbars while the gate is on screen, restore them when it's gone
useEffect(() => {
  if (phase === "done") return;
  const html = document.documentElement;
  const body = document.body;
  const prevHtml = html.style.overflow;
  const prevBody = body.style.overflow;
  html.style.overflow = "hidden";
  body.style.overflow = "hidden";
  return () => {
    html.style.overflow = prevHtml;
    body.style.overflow = prevBody;
  };
}, [phase]);

  // Remove the overlay once the animation has fully finished
  useEffect(() => {
    if (phase !== "opening") return;
    if (SHOW_ONCE_PER_SESSION) {
      try {
        sessionStorage.setItem(SEEN_KEY, "1");
      } catch {}
    }
    const t = setTimeout(() => setPhase("done"), openMs);
    return () => clearTimeout(t);
  }, [phase, openMs]);

  // Pressing a key skips the intro (desktop)
  useEffect(() => {
    if (phase !== "loading") return;
    const onKey = () => setPhase("opening");
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [phase]);

  const loading = phase === "loading";
  const opening = phase === "opening";

  // The page behind starts slightly zoomed and dim, then settles as the doors part.
  // The end state equals "no style", so removing it at the end causes no jump.
  const pageStyle: CSSProperties | undefined =
    phase === "done" || calm
      ? undefined
      : {
          transform: open ? "none" : "scale(1.05)",
          filter: open ? "none" : "brightness(.65)",
          transition: `transform ${OPEN_DUR}ms cubic-bezier(.2,.7,.2,1) ${OPEN_DELAY}ms, filter ${OPEN_DUR}ms cubic-bezier(.2,.7,.2,1) ${OPEN_DELAY}ms`,
        };

  const doorBase: CSSProperties = {
    transition: calm ? "none" : `transform ${OPEN_DUR}ms cubic-bezier(.65,0,.2,1) ${OPEN_DELAY}ms`,
  };
  const leftDoor: CSSProperties = {
    ...doorBase,
    transform: open && !calm ? "translate3d(-100%,0,0)" : "translate3d(0,0,0)",
  };
  const rightDoor: CSSProperties = {
    ...doorBase,
    transform: open && !calm ? "translate3d(100%,0,0)" : "translate3d(0,0,0)",
  };
  const edgeStyle: CSSProperties = {
    opacity: open && !calm ? 1 : 0,
    transition: "opacity .4s ease-out .15s",
  };

  return (
    <>
      {/* The sign-in page is mounted from the start, hidden behind the gate and not focusable until it opens */}
      <div style={{ overflow: phase !== "done" ? "clip" : undefined }}>
        <div style={pageStyle} inert={loading} aria-hidden={loading ? true : undefined}>
          {children}
        </div>
      </div>

      {phase !== "done" && (
        <div
          dir="ltr"
          role="status"
          aria-live="polite"
          aria-label="Preparing the sign-in gateway"
          className={`fixed inset-0 z-[100] overflow-hidden ${opening ? "pointer-events-none" : ""}`}
          style={calm ? { opacity: open ? 0 : 1, transition: "opacity .8s ease" } : undefined}
        >
          <style>{`
            @keyframes ig-draw { to { stroke-dashoffset: 0; } }
            @keyframes ig-spin { to { transform: rotate(360deg); } }
            @keyframes ig-fade { from { opacity: 0; transform: translateY(6px); } to { opacity: 1; transform: none; } }
            @keyframes ig-fade-op { from { opacity: 0; } to { opacity: 1; } }
            @keyframes ig-bar { from { transform: scaleX(0); } to { transform: scaleX(1); } }
            @keyframes ig-glow { 0%, 100% { opacity: .55; } 50% { opacity: 1; } }
            @keyframes ig-seam {
              0%   { opacity: 0; transform: translateX(-50%) scaleY(.15); }
              30%  { opacity: 1; transform: translateX(-50%) scaleY(1); }
              60%  { opacity: 1; transform: translateX(-50%) scaleY(1); }
              100% { opacity: 0; transform: translateX(-50%) scaleY(1); }
            }

            /* grid texture, drawn on the full-width inner layer so both halves line up */
            .ig-inner::before {
              content: ""; position: absolute; inset: 0;
              background-image:
                linear-gradient(rgba(255,255,255,.05) 1px, transparent 1px),
                linear-gradient(90deg, rgba(255,255,255,.05) 1px, transparent 1px);
              background-size: 44px 44px;
              -webkit-mask-image: radial-gradient(ellipse at center, #000 30%, transparent 85%);
              mask-image: radial-gradient(ellipse at center, #000 30%, transparent 85%);
            }

            .ig-seam {
              position: absolute; top: 0; bottom: 0; left: 50%; width: 2px;
              transform: translateX(-50%);
              background: linear-gradient(to bottom, transparent, #ffffff, transparent);
              box-shadow: 0 0 24px 6px rgba(94,234,212,.6);
              opacity: 0;
            }
            .ig-seam-on { animation: ig-seam 1.5s ease-out forwards; }

            .ig-stroke {
              fill: none; stroke-dasharray: 1; stroke-dashoffset: 1;
              animation: ig-draw 1.2s cubic-bezier(.3,.7,.2,1) forwards;
            }
            .ig-sweep {
              transform-origin: 100px 100px;
              animation: ig-spin 2.6s linear infinite;
            }
            .ig-fillin { opacity: 0; animation: ig-fade-op .6s 1.9s ease-out forwards; }
            .ig-bar { transform-origin: 0% 50%; animation: ig-bar ${loadMs}ms linear forwards; }
            .ig-step { animation: ig-fade .4s ease-out both; }

            /* calm mode: nothing spins or slides, the gate simply fades away */
            .ig-calm .ig-sweep { animation: none; opacity: 0; }
            .ig-calm .ig-stroke { animation-duration: .5s; animation-delay: 0s !important; }
            .ig-calm .ig-fillin { animation-delay: .5s; }
          `}</style>

          <div className={`absolute inset-0 ${calm ? "ig-calm" : ""}`}>
            {/* LEFT door: exactly half the screen, shows the left half of the full-size content */}
            <div className="absolute bottom-0 left-0 top-0 w-1/2 overflow-hidden bg-[#0A2540] will-change-transform" style={leftDoor}>
              <div className="ig-inner absolute left-0 top-0 h-full w-[200%]">
                <IntroContent step={step} open={open} />
              </div>
              <div
                className="absolute bottom-0 right-0 top-0 w-[3px]"
                style={{
                  ...edgeStyle,
                  background: "linear-gradient(to bottom, transparent, #5EEAD4 20%, #5EEAD4 80%, transparent)",
                  boxShadow: "0 0 16px 3px rgba(94,234,212,.55)",
                }}
              />
            </div>

            {/* RIGHT door */}
            <div aria-hidden className="absolute bottom-0 right-0 top-0 w-1/2 overflow-hidden bg-[#0A2540] will-change-transform" style={rightDoor}>
              <div className="ig-inner absolute right-0 top-0 h-full w-[200%]">
                <IntroContent step={step} open={open} />
              </div>
              <div
                className="absolute bottom-0 left-0 top-0 w-[3px]"
                style={{
                  ...edgeStyle,
                  background: "linear-gradient(to bottom, transparent, #5EEAD4 20%, #5EEAD4 80%, transparent)",
                  boxShadow: "0 0 16px 3px rgba(94,234,212,.55)",
                }}
              />
            </div>

            {/* light on the seam that builds up just before the split */}
            {!calm && <div className={`ig-seam ${open ? "ig-seam-on" : ""}`} />}
          </div>

          {loading && (
            <button
              onClick={() => setPhase("opening")}
              className="absolute bottom-8 left-1/2 z-10 -translate-x-1/2 rounded-lg px-4 py-2 text-xs font-medium text-slate-400 transition hover:bg-white/5 hover:text-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-300"
            >
              Skip
            </button>
          )}
        </div>
      )}
    </>
  );
}