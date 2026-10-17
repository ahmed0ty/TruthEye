"use client";
import { use } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Download, Eye, Percent, Trophy, Users } from "lucide-react";
import { DataTable, PersonCell } from "@/components/ui/data-table";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { doctorApi } from "@/lib/api/doctor";
import { fmtDateTime } from "@/lib/format";
import { cn } from "@/lib/utils";
import { downloadCsv } from "@/lib/export-csv";

export default function ExamResultsPage({
  params,
}: {
  params: Promise<{ examId: string }>;
}) {
  const { examId: raw } = use(params);
  const examId = Number(raw);

  const results = useQuery({
    queryKey: ["doctor", "results", examId],
    queryFn: () => doctorApi.results(examId),
  });
  const analysis = useQuery({
    queryKey: ["doctor", "analysis", examId],
    queryFn: () => doctorApi.analysis(examId),
  });

  const a = analysis.data;
  const rowsData = results.data ?? [];

  const exportCsv = () => {
    downloadCsv(
      `${(a?.examTitle ?? "exam").replace(/[^\w\-]+/g, "_")}_results.csv`,
      [
        "Student",
        "Score",
        "Percentage",
        "Result",
        "Status",
        "Correct answers",
        "Total questions",
        "Submitted",
      ],
      rowsData.map((r) => [
        r.studentName,
        r.totalScore,
        Number(r.percentage.toFixed(1)),
        r.isPassed ? "Passed" : "Failed",
        r.status === "TimedOut" ? "Timed out" : "Completed",
        r.correctAnswers,
        r.totalQuestions,
        fmtDateTime(r.submittedAt),
      ]),
    );
  };

  const stats = [
    {
      label: "Students",
      value: a?.totalStudents ?? 0,
      decimals: 0,
      suffix: "",
      icon: Users,
      tone: "bg-teal-50 text-teal-700",
    },
    {
      label: "Average score",
      value: a?.averageScore ?? 0,
      decimals: 1,
      suffix: "",
      icon: Percent,
      tone: "bg-sky-50 text-sky-700",
    },
    {
      label: "Pass rate",
      value: a?.passRate ?? 0,
      decimals: 0,
      suffix: "%",
      icon: Trophy,
      tone: "bg-emerald-50 text-emerald-700",
    },
  ];

  return (
    <div className="space-y-6">
      <Link
        href="/doctor/results"
        className="inline-flex items-center gap-2 text-sm font-semibold text-slate-500 transition hover:text-[#0A2540]"
      >
        <ArrowLeft size={16} /> Back to results
      </Link>

      <PageHeader
        title={a?.examTitle ?? "Exam results"}
        subtitle="Completed and timed-out attempts."
        action={
          <button
            onClick={exportCsv}
            disabled={rowsData.length === 0}
            className="inline-flex items-center gap-2 rounded-xl bg-teal-600 px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-teal-900/10 transition hover:bg-teal-700 active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Download size={17} /> Export Excel
          </button>
        }
      />

      <div className="grid gap-4 sm:grid-cols-3">
        {stats.map((s, i) => (
          <StatCard
            key={s.label}
            {...s}
            loading={analysis.isLoading}
            delay={i * 0.07}
          />
        ))}
      </div>

      <DataTable
        rows={(results.data ?? []).map((r) => ({ ...r, id: r.sessionId }))}
        loading={results.isLoading}
        error={results.error ? (results.error as Error).message : undefined}
        searchText={(r) => r.studentName}
        placeholder="Search students..."
        columns={[
          {
            header: "Student",
            cell: (r) => (
              <PersonCell
                first={r.studentName.split(" ")[0] ?? ""}
                last={r.studentName.split(" ").slice(1).join(" ")}
              />
            ),
          },
          {
            header: "Score",
            cell: (r) => (
              <span className="font-bold text-[#0A2540]">{r.totalScore}</span>
            ),
          },
          { header: "Percentage", cell: (r) => `${r.percentage.toFixed(0)}%` },
          {
            header: "Result",
            cell: (r) => (
              <div className="flex flex-wrap items-center gap-1.5">
                <span
                  className={cn(
                    "rounded-full px-2.5 py-1 text-xs font-semibold",
                    r.isPassed
                      ? "bg-emerald-50 text-emerald-700"
                      : "bg-red-50 text-red-700",
                  )}
                >
                  {r.isPassed ? "Passed" : "Failed"}
                </span>
                {r.status === "TimedOut" && (
                  <span className="rounded-full bg-amber-50 px-2.5 py-1 text-xs font-semibold text-amber-700">
                    Timed out
                  </span>
                )}
              </div>
            ),
          },
          {
            header: "Submitted",
            cell: (r) => (
              <span className="text-slate-500">
                {fmtDateTime(r.submittedAt)}
              </span>
            ),
          },
        ]}
        actions={(r) => (
          <Link
            href={`/doctor/results/${examId}/${r.sessionId}`}
            className="inline-flex items-center gap-1.5 rounded-lg bg-teal-50 px-3 py-1.5 text-xs font-semibold text-teal-700 transition hover:bg-teal-100"
          >
            <Eye size={14} /> Review
          </Link>
        )}
      />
    </div>
  );
}


