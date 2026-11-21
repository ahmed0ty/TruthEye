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
    text: "Suspicious behavior reaches the proctor the moment it happens.",
  },
  {
    icon: FileCheck,
    title: "Full report after every exam",
    text: "Notes and evidence gathered in one place.",
  },
];

function Logo({ light = false }: { light?: boolean }) {
  return (
    <div className="flex items-center gap-2.5">
      <div
        className={`flex h-9 w-9 items-center justify-center rounded-lg ${
          light
            ? "bg-teal-400/15 text-teal-300 ring-1 ring-teal-300/30"
            : "bg-[#0A2540] text-teal-300"
        }`}
      >
        <ShieldCheck size={20} />
      </div>
      <span className={`text-lg font-bold tracking-tight ${light ? "text-white" : "text-[#0A2540]"}`}>
        ExamGurd
      </span>
    </div>
  );
}

export function AuthShell({ children, eye = "idle" }: { children: ReactNode; eye?: EyeMode }) {
  return (
    <main className="grid h-dvh overflow-hidden bg-[#F4F7FA] text-slate-900 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.05fr)]">
      {/* Form side */}
      <section className="relative flex flex-col items-center justify-center px-5 py-8 sm:px-10">
        {/* soft dotted backdrop */}
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 opacity-60"
          style={{
            backgroundImage: "radial-gradient(rgba(10,37,64,.10) 1px, transparent 1px)",
            backgroundSize: "22px 22px",
          }}
        />
        <div className="relative w-full max-w-[27rem]">
          <div className="mb-6 flex justify-center lg:hidden">
            <Logo />
          </div>
          <div className="relative overflow-hidden rounded-3xl bg-white p-7 shadow-2xl shadow-slate-900/[0.08] ring-1 ring-slate-200/80 sm:p-9">
            <div aria-hidden className="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-teal-600 via-teal-400 to-indigo-400" />
            {children}
          </div>
          <p className="mt-6 text-center text-xs text-slate-400">
            Protected by ExamGurd · Faculty exam platform
          </p>
        </div>
      </section>

      {/* Brand side */}
      <aside className="relative hidden overflow-hidden bg-[#0A2540] lg:flex lg:flex-col lg:justify-between lg:px-12 lg:py-10 xl:px-16">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0"
          style={{
            background:
              "radial-gradient(circle at 15% 15%, rgba(20,184,166,.22), transparent 45%), radial-gradient(circle at 90% 90%, rgba(99,102,241,.18), transparent 42%)",
          }}
        />
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 opacity-[0.06]"
          style={{
            backgroundImage:
              "linear-gradient(#fff 1px, transparent 1px), linear-gradient(90deg, #fff 1px, transparent 1px)",
            backgroundSize: "44px 44px",
          }}
        />
        <svg
          aria-hidden
          viewBox="0 0 600 600"
          className="pointer-events-none absolute -bottom-44 -right-44 h-[600px] w-[600px] animate-[spin_90s_linear_infinite]"
          fill="none"
        >
          <circle cx="300" cy="300" r="290" stroke="white" strokeOpacity="0.06" />
          <circle cx="300" cy="300" r="220" stroke="white" strokeOpacity="0.08" />
          <circle cx="300" cy="300" r="150" stroke="white" strokeOpacity="0.12" />
          <circle cx="300" cy="300" r="80" stroke="#5EEAD4" strokeOpacity="0.5" />
        </svg>

        <div className="relative animate-fade-in">
          <Logo light />
        </div>

        <div className="relative">
          <div className="animate-fade-in mx-auto mb-6 hidden w-52 [@media(min-height:640px)]:block xl:w-64">
            <EyeGuardian mode={eye} />
          </div>
          <h2 className="animate-fade-up text-3xl font-bold leading-tight text-white xl:text-4xl">
            Exam integrity starts here
          </h2>
          <p
            className="animate-fade-up mt-3 max-w-md text-sm leading-6 text-slate-300"
            style={{ animationDelay: "100ms" }}
          >
            An online exam proctoring platform that helps universities make sure every result
            reflects the student's own effort.
          </p>

          <ul className="mt-8 space-y-4">
            {FEATURES.map(({ icon: Icon, title, text }, i) => (
              <li
                key={title}
                className="animate-fade-up flex items-start gap-3.5"
                style={{ animationDelay: `${250 + i * 120}ms` }}
              >
                <span className="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-white/[0.06] text-teal-300 ring-1 ring-white/10">
                  <Icon size={18} />
                </span>
                <div>
                  <p className="text-sm font-semibold text-white">{title}</p>
                  <p className="mt-0.5 text-sm leading-5 text-slate-400">{text}</p>
                </div>
              </li>
            ))}
          </ul>
        </div>

        <div className="relative flex items-center gap-2 text-xs text-slate-400">
          <span className="relative flex h-2 w-2">
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-teal-400 opacity-60 motion-reduce:animate-none" />
            <span className="relative inline-flex h-2 w-2 rounded-full bg-teal-400" />
          </span>
          Proctoring is active throughout the exam
        </div>
      </aside>
    </main>
  );
}
