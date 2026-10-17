"use client";

import { useCallback, useEffect, useId, useRef, useState } from "react";

export type EyeMode = "idle" | "watching" | "private" | "error" | "success";

const CAPTION: Record<EyeMode, string> = {
  idle: "Watching over every exam",
  watching: "Take your time",
  private: "Your password stays private",
  error: "Couldn't verify, try again",
  success: "Identity verified",
};

const ALMOND = "M20 100 Q160 -10 300 100 Q160 210 20 100 Z";
const SHIELD = "M0 -43 L34 -30 V-1 C34 21 19 35 0 43 C-19 35 -34 21 -34 -1 V-30 Z";
const LID = "#0F2F52";
const MAX_X = 38; // how far the iris can travel sideways
const MAX_Y = 17; // ...and vertically

const clamp = (v: number, min: number, max: number) => Math.min(max, Math.max(min, v));
const TICKS = Array.from({ length: 24 }, (_, i) => i * 15);

export function EyeGuardian({ mode = "idle", className = "" }: { mode?: EyeMode; className?: string }) {
  const uid = useId().replace(/:/g, "");
  const svgRef = useRef<SVGSVGElement>(null);
  const irisRef = useRef<SVGGElement>(null);
  const mouse = useRef<{ x: number; y: number } | null>(null);
  const modeRef = useRef<EyeMode>(mode);
  const [blink, setBlink] = useState(false);

  const aim = useCallback((dx: number, dy: number) => {
    if (irisRef.current) irisRef.current.style.transform = `translate(${dx}px, ${dy}px)`;
  }, []);

  // Decide where the iris should look, based on the current mode and the last mouse position
  const track = useCallback(() => {
    const svg = svgRef.current;
    if (!svg) return;

    const m = modeRef.current;
    if (m === "watching") return aim(-MAX_X * 0.85, MAX_Y * 0.4); // glance toward the form
    if (m !== "idle" || !mouse.current) return aim(0, 0);

    const r = svg.getBoundingClientRect();
    const nx = clamp((mouse.current.x - (r.left + r.width / 2)) / (window.innerWidth / 2), -1, 1);
    const ny = clamp((mouse.current.y - (r.top + r.height / 2)) / (window.innerHeight / 2), -1, 1);
    aim(nx * MAX_X, ny * MAX_Y);
  }, [aim]);

  useEffect(() => {
    modeRef.current = mode;
    track();
  }, [mode, track]);

  // Follow the pointer
  useEffect(() => {
    let raf = 0;
    const onMove = (e: PointerEvent) => {
      mouse.current = { x: e.clientX, y: e.clientY };
      cancelAnimationFrame(raf);
      raf = requestAnimationFrame(track);
    };
    window.addEventListener("pointermove", onMove, { passive: true });
    return () => {
      window.removeEventListener("pointermove", onMove);
      cancelAnimationFrame(raf);
    };
  }, [track]);

  // Random blinking (only while the eye is "awake")
  useEffect(() => {
    if (mode !== "idle" && mode !== "watching") return;
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    let t1: ReturnType<typeof setTimeout> | undefined;
    let t2: ReturnType<typeof setTimeout> | undefined;
    const schedule = () => {
      t1 = setTimeout(() => {
        setBlink(true);
        t2 = setTimeout(() => {
          setBlink(false);
          schedule();
        }, 150);
      }, 2500 + Math.random() * 3500);
    };
    schedule();

    return () => {
      clearTimeout(t1);
      clearTimeout(t2);
      setBlink(false);
    };
  }, [mode]);

  const closed = mode === "private" || blink;
  const error = mode === "error";
  const success = mode === "success";
  const lidMove = `transform ${closed ? ".22s" : ".16s"} ease`;

  return (
    <div className={className}>
      <style>{`
        @keyframes eg-spin { to { transform: rotate(360deg); } }
        @keyframes eg-scan { from { transform: translateX(-80px); } to { transform: translateX(340px); } }
        @keyframes eg-shake {
          0%, 100% { transform: translateX(0); }
          15% { transform: translateX(-10px); }
          30% { transform: translateX(10px); }
          45% { transform: translateX(-8px); }
          60% { transform: translateX(8px); }
          75% { transform: translateX(-4px); }
          90% { transform: translateX(4px); }
        }
        .eg-spin { transform-origin: 160px 100px; animation: eg-spin 14s linear infinite; }
        .eg-scan { animation: eg-scan 4.5s ease-in-out infinite; }
        .eg-shake { animation: eg-shake .55s ease-in-out; }
        .eg-glow { filter: drop-shadow(0 0 22px rgba(20, 184, 166, .35)); }
        @media (prefers-reduced-motion: reduce) {
          .eg-spin, .eg-scan, .eg-shake { animation: none; }
        }
      `}</style>

      {/* key changes on error so the shake animation restarts every time */}
      <div key={error ? "error" : "ok"} className={error ? "eg-shake" : undefined}>
        <svg ref={svgRef} aria-hidden viewBox="0 0 320 200" className="eg-glow h-auto w-full overflow-visible">
          <defs>
            <clipPath id={`${uid}-clip`}>
              <path d={ALMOND} />
            </clipPath>
            <radialGradient id={`${uid}-iris`}>
              <stop offset="0" stopColor="#99F6E4" />
              <stop offset=".45" stopColor="#14B8A6" />
              <stop offset="1" stopColor="#0F766E" />
            </radialGradient>
            <radialGradient id={`${uid}-iris-red`}>
              <stop offset="0" stopColor="#FECACA" />
              <stop offset=".45" stopColor="#EF4444" />
              <stop offset="1" stopColor="#991B1B" />
            </radialGradient>
            <linearGradient id={`${uid}-sclera`} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0" stopColor="#D5F3EE" />
              <stop offset="1" stopColor="#F2FFFC" />
            </linearGradient>
            <linearGradient id={`${uid}-shade`} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0" stopColor="#0A2540" stopOpacity=".55" />
              <stop offset=".5" stopColor="#0A2540" stopOpacity="0" />
            </linearGradient>
            <linearGradient id={`${uid}-scan`} x1="0" y1="0" x2="1" y2="0">
              <stop offset="0" stopColor="#5EEAD4" stopOpacity="0" />
              <stop offset=".5" stopColor="#5EEAD4" stopOpacity=".35" />
              <stop offset="1" stopColor="#5EEAD4" stopOpacity="0" />
            </linearGradient>
          </defs>

          {/* radar rings around the eye */}
          <g fill="none" stroke="#fff" strokeOpacity=".1" vectorEffect="non-scaling-stroke">
            <path d={ALMOND} transform="translate(160 100) scale(1.14) translate(-160 -100)" vectorEffect="non-scaling-stroke" />
            <path d={ALMOND} transform="translate(160 100) scale(1.28) translate(-160 -100)" strokeOpacity=".06" vectorEffect="non-scaling-stroke" />
          </g>

          {/* the eye (fades out and shrinks on success) */}
          <g
            style={{
              opacity: success ? 0 : 1,
              transform: success ? "scale(.7)" : "none",
              transformOrigin: "160px 100px",
              transition: "opacity .35s ease, transform .35s ease",
            }}
          >
            <g clipPath={`url(#${uid}-clip)`}>
              <rect x="0" y="0" width="320" height="200" fill={`url(#${uid}-sclera)`} />

              {/* iris + pupil: this group is what follows the mouse */}
              <g ref={irisRef} style={{ transition: "transform .14s ease-out" }}>
                <circle cx="160" cy="100" r="54" fill="none" stroke="#14B8A6" strokeOpacity=".45" strokeWidth="1.5" strokeDasharray="3 9" className="eg-spin" />
                <circle cx="160" cy="100" r="46" fill={`url(#${uid}-iris)`} />
                <circle
                  cx="160"
                  cy="100"
                  r="46"
                  fill={`url(#${uid}-iris-red)`}
                  style={{ opacity: error ? 1 : 0, transition: "opacity .3s ease" }}
                />
                <circle cx="160" cy="100" r="46" fill="none" stroke="#0B4F4A" strokeOpacity=".6" strokeWidth="2" />
                {TICKS.map((a) => (
                  <line
                    key={a}
                    x1="160"
                    y1="56"
                    x2="160"
                    y2="76"
                    transform={`rotate(${a} 160 100)`}
                    stroke="#fff"
                    strokeOpacity=".2"
                    strokeWidth="1.5"
                  />
                ))}
                <circle
                  cx="160"
                  cy="100"
                  r="19"
                  fill="#04141F"
                  style={{
                    transform: error ? "scale(.55)" : "scale(1)",
                    transformOrigin: "160px 100px",
                    transition: "transform .3s ease",
                  }}
                />
                <circle cx="150" cy="88" r="7" fill="#fff" fillOpacity=".85" />
              </g>

              <rect x="0" y="0" width="320" height="200" fill={`url(#${uid}-shade)`} />
              <rect className="eg-scan" x="0" y="0" width="50" height="200" fill={`url(#${uid}-scan)`} />

              {/* eyelids */}
              <rect x="0" y="-1" width="320" height="102" fill={LID} style={{ transform: closed ? "translateY(0)" : "translateY(-102px)", transition: lidMove }} />
              <rect x="0" y="99" width="320" height="102" fill={LID} style={{ transform: closed ? "translateY(0)" : "translateY(102px)", transition: lidMove }} />
            </g>

            {/* the closed-eye line */}
            <path
              d="M22 100 Q160 140 298 100"
              fill="none"
              stroke="#5EEAD4"
              strokeWidth="3"
              strokeLinecap="round"
              style={{ opacity: closed ? 1 : 0, transition: "opacity .2s ease" }}
            />

            {/* outline */}
            <path
              d={ALMOND}
              fill="none"
              strokeWidth="2.5"
              strokeLinejoin="round"
              style={{ stroke: error ? "#FCA5A5" : "#5EEAD4", transition: "stroke .3s ease" }}
            />
          </g>

          {/* shield + check (drawn in on success) */}
          <g transform="translate(160 100) scale(1.5)" style={{ opacity: success ? 1 : 0, transition: "opacity .3s ease .1s" }}>
            <path d={SHIELD} fill="rgba(20,184,166,.18)" style={{ opacity: success ? 1 : 0, transition: "opacity .5s ease .5s" }} />
            <path
              d={SHIELD}
              pathLength={1}
              fill="none"
              stroke="#5EEAD4"
              strokeWidth="3"
              strokeLinejoin="round"
              strokeDasharray="1"
              style={{ strokeDashoffset: success ? 0 : 1, transition: "stroke-dashoffset .7s ease .15s" }}
            />
            <path
              d="M-14 -1 L-4 9 L16 -13"
              pathLength={1}
              fill="none"
              stroke="#fff"
              strokeWidth="4"
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeDasharray="1"
              style={{ strokeDashoffset: success ? 0 : 1, transition: "stroke-dashoffset .45s ease .75s" }}
            />
          </g>
        </svg>
      </div>

      <p
        aria-live="polite"
        className={`mt-4 min-h-4 text-center text-xs font-medium tracking-wide transition-colors duration-300 ${
          error ? "text-red-300" : success ? "text-teal-300" : "text-teal-200/80"
        }`}
      >
        {CAPTION[mode]}
      </p>
    </div>
  );
}