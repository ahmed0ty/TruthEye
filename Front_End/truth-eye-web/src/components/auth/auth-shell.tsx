import type { ReactNode } from "react";
import { BellRing, FileCheck, ShieldCheck, Video } from "lucide-react";
import { EyeGuardian, type EyeMode } from "./eye-guardian";

const FEATURES = [
  {
    icon: Video,
    title: "AI-powered proctoring",
    text: "Tracks the student's movement and face throughout the exam.",
  },
  {
    icon: BellRing,
    title: "Instant proctor alerts",
    text: "Any suspicious behavior reaches the proctor the moment it happens.",
  },
  {
    icon: FileCheck,
    title: "Full report after every exam",
    text: "All notes and evidence in one place.",
  },
];

function Logo({ light = false }: { light?: boolean }) {
  return (
    <div className="flex items-center gap-3">
      <div
        className={`flex h-11 w-11 items-center justify-center rounded-xl ${
          light
            ? "bg-teal-400/15 text-teal-300 ring-1 ring-teal-300/30"
            : "bg-[#0A2540] text-teal-300"
        }`}
      >
        <ShieldCheck size={24} />
      </div>
      <span className={`text-xl font-bold ${light ? "text-white" : "text-[#0A2540]"}`}>
        ExamGurd
      </span>
    </div>
  );
}

export function AuthShell({ children, eye = "idle" }: { children: ReactNode; eye?: EyeMode }) {
  return (
    <main className="grid min-h-screen bg-[#F6F8FA] text-slate-900 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)]">
      {/* Form side */}
      <section className="flex items-center justify-center px-5 py-10 sm:px-10">
        <div className="w-full max-w-[26rem]">
          <div className="mb-10 lg:hidden">
            <Logo />
          </div>
          {children}
        </div>
      </section>

      {/* Brand side */}
      <aside className="relative hidden overflow-hidden bg-[#0A2540] lg:flex lg:flex-col lg:justify-between lg:p-14">
        <div
          aria-hidden
          className="absolute inset-0 opacity-[0.07]"
          style={{
            backgroundImage:
              "linear-gradient(#fff 1px, transparent 1px), linear-gradient(90deg, #fff 1px, transparent 1px)",
            backgroundSize: "44px 44px",
          }}
        />

        <svg
          aria-hidden
          viewBox="0 0 600 600"
          className="absolute -bottom-40 -right-40 h-[640px] w-[640px]"
          fill="none"
        >
          <circle cx="300" cy="300" r="290" stroke="white" strokeOpacity="0.06" />
          <circle cx="300" cy="300" r="220" stroke="white" strokeOpacity="0.08" />
          <circle cx="300" cy="300" r="150" stroke="white" strokeOpacity="0.12" />
          <circle cx="300" cy="300" r="80" stroke="#5EEAD4" strokeOpacity="0.5" />
        </svg>

        <div className="relative">
          <Logo light />
        </div>

        <div className="relative max-w-md">
          {/* The eye is only shown on tall screens so the panel never overflows */}
          {/* The eye scales with the screen height instead of disappearing */}
<div className="mb-6 w-44 [@media(min-height:700px)]:mb-8 [@media(min-height:700px)]:w-56 [@media(min-height:860px)]:w-72">
  <EyeGuardian mode={eye} />
</div>
          <h2 className="text-4xl font-bold leading-[1.35] text-white">
            Exam integrity starts here
          </h2>
          <p className="mt-4 text-base leading-7 text-slate-300">
            An online exam proctoring platform that helps universities make sure every result
            reflects the student's own effort.
          </p>

          <ul className="mt-10 space-y-6">
            {FEATURES.map(({ icon: Icon, title, text }) => (
              <li key={title} className="flex items-start gap-4">
                <span className="mt-0.5 flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-white/[0.06] text-teal-300 ring-1 ring-white/10">
                  <Icon size={20} />
                </span>
                <div>
                  <p className="font-semibold text-white">{title}</p>
                  <p className="mt-1 text-sm leading-6 text-slate-400">{text}</p>
                </div>
              </li>
            ))}
          </ul>
        </div>

        <div className="relative flex items-center gap-2 text-sm text-slate-400">
          <span className="relative flex h-2.5 w-2.5">
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-teal-400 opacity-60 motion-reduce:animate-none" />
            <span className="relative inline-flex h-2.5 w-2.5 rounded-full bg-teal-400" />
          </span>
          Proctoring is active throughout the exam
        </div>
      </aside>
    </main>
  );
}