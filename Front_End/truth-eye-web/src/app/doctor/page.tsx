"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight, BookOpen, CalendarClock, ClipboardList, FileQuestion, Radio, Sparkles } from "lucide-react";
import { DoctorCharts } from "@/components/dashboard/doctor-charts";
import { doctorApi } from "@/lib/api/doctor";
import { useAuth } from "@/lib/auth/store";
import { fmtDateTime } from "@/lib/format";
import { cn } from "@/lib/utils";
import { getExamState } from "@/types/exam";

export default function DoctorHome() {
  const user = useAuth((s) => s.user);
  const courses = useQuery({ queryKey: ["doctor", "courses"], queryFn: doctorApi.courses });
  const exams = useQuery({ queryKey: ["doctor", "exams"], queryFn: doctorApi.exams });

  if (!user) return null;

  const list = exams.data ?? [];
  const open = list.filter((e) => getExamState(e) === "open").length;
  const upcoming = list.filter((e) => getExamState(e) === "upcoming").length;
  const questions = list.reduce((a, e) => a + e.questionsCount, 0);
  const recent = [...list].sort((a, b) => +new Date(b.startTime) - +new Date(a.startTime)).slice(0, 5);

  const hour = new Date().getHours();
  const greet = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";

  const stats = [
    { label: "My Courses", value: courses.data?.length ?? 0, icon: BookOpen, tone: "bg-teal-50 text-teal-700" },
    { label: "Exams", value: list.length, icon: ClipboardList, tone: "bg-sky-50 text-sky-700" },
    { label: "Live now", value: open, icon: Radio, tone: "bg-emerald-50 text-emerald-700" },
    { label: "Upcoming", value: upcoming, icon: CalendarClock, tone: "bg-amber-50 text-amber-700" },
    { label: "Questions", value: questions, icon: FileQuestion, tone: "bg-slate-100 text-[#0A2540]" },
  ];

  return (
    <div className="space-y-8">
      <section className="animate-fade-up relative overflow-hidden rounded-3xl bg-[#0A2540] p-8 text-white sm:p-10">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 opacity-[0.07]"
          style={{
            backgroundImage: "linear-gradient(#fff 1px, transparent 1px), linear-gradient(90deg, #fff 1px, transparent 1px)",
            backgroundSize: "44px 44px",
          }}
        />
        <div className="pointer-events-none absolute -top-20 -end-10 h-72 w-72 rounded-full bg-teal-500/25 blur-3xl" />
        <div className="relative">
          <span className="inline-flex items-center gap-1.5 rounded-full border border-teal-400/30 bg-teal-400/10 px-3 py-1 text-xs font-semibold text-teal-200">
            <Sparkles size={14} /> Doctor Dashboard
          </span>
          <h1 className="mt-4 text-3xl font-bold sm:text-4xl">
            {greet}, Dr. {user.name.split(" ")[0]} 👋
          </h1>
          <p className="mt-2 max-w-xl text-slate-300">Create exams, manage questions, and keep track of your courses.</p>
          <Link
            href="/doctor/exams"
            className="group mt-6 inline-flex items-center gap-2 rounded-xl bg-teal-500 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-400"
          >
            Manage exams <ArrowRight size={16} className="transition group-hover:translate-x-1" />
          </Link>
        </div>
      </section>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
        {stats.map(({ label, value, icon: Icon, tone }, i) => (
          <div
            key={label}
            className="animate-fade-up rounded-2xl border border-slate-200 bg-white p-5 transition duration-300 hover:-translate-y-1 hover:shadow-lg hover:shadow-slate-200/70"
            style={{ animationDelay: `${i * 0.07}s` }}
          >
            <div className={cn("mb-4 flex h-11 w-11 items-center justify-center rounded-xl", tone)}>
              <Icon size={21} />
            </div>
            <p className="text-sm font-medium text-slate-500">{label}</p>
            {courses.isLoading || exams.isLoading ? (
              <div className="mt-2 h-8 w-14 animate-pulse rounded-lg bg-slate-100" />
            ) : (
              <p className="mt-1 text-3xl font-bold tracking-tight text-[#0A2540]">{value}</p>
            )}
          </div>
        ))}
      </div>

      <DoctorCharts exams={list} />

      <section className="animate-fade-up rounded-2xl border border-slate-200 bg-white" style={{ animationDelay: "0.35s" }}>
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-5">
          <h3 className="font-bold text-[#0A2540]">Recent exams</h3>
          <Link href="/doctor/exams" className="text-sm font-semibold text-teal-700 hover:text-teal-800">View all</Link>
        </div>
        {recent.length === 0 ? (
          <p className="px-6 py-12 text-center text-sm text-slate-400">You haven’t created any exams yet.</p>
        ) : (
          <ul className="divide-y divide-slate-100">
            {recent.map((e) => (
              <li key={e.id} className="flex items-center justify-between gap-4 px-6 py-4">
                <div className="min-w-0">
                  <p className="truncate font-semibold text-[#0A2540]">{e.title}</p>
                  <p className="text-xs text-slate-500">
                    {e.courseName ?? "—"} · {e.questionsCount} questions · {fmtDateTime(e.startTime)}
                  </p>
                </div>
                <Link
                  href={`/doctor/exams/${e.id}`}
                  className="shrink-0 rounded-lg bg-teal-50 px-3 py-1.5 text-xs font-semibold text-teal-700 transition hover:bg-teal-100"
                >
                  Questions
                </Link>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}