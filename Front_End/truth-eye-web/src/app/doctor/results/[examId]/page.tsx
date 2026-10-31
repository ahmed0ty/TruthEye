// "use client";
// import { use, useMemo, useState } from "react";
// import Link from "next/link";
// import { useQuery } from "@tanstack/react-query";
// import { ArrowLeft, Download, Eye, Percent, ShieldAlert, Trophy, Users } from "lucide-react";
// import { DataTable, PersonCell } from "@/components/ui/data-table";
// import { PageHeader } from "@/components/ui/page-header";
// import { StatCard } from "@/components/ui/stat-card";
// import { IntegrityBadge } from "@/components/proctoring/integrity-badge";
// import { IntegrityFilter, type IntegrityFilterValue } from "@/components/proctoring/integrity-filter";
// import { doctorApi } from "@/lib/api/doctor";
// import { fmtDateTime } from "@/lib/format";
// import { isFlagged } from "@/lib/proctoring/events";
// import { cn } from "@/lib/utils";
// import { downloadCsv } from "@/lib/export-csv";

// export default function ExamResultsPage({
//   params,
// }: {
//   params: Promise<{ examId: string }>;
// }) {
//   const { examId: raw } = use(params);
//   const examId = Number(raw);
//   const [filter, setFilter] = useState<IntegrityFilterValue>("all");

//   const results = useQuery({
//     queryKey: ["doctor", "results", examId],
//     queryFn: () => doctorApi.results(examId),
//   });
//   const analysis = useQuery({
//     queryKey: ["doctor", "analysis", examId],
//     queryFn: () => doctorApi.analysis(examId),
//   });

//   const a = analysis.data;
//   const rowsData = useMemo(() => results.data ?? [], [results.data]);

//   const counts = useMemo(() => {
//     const flagged = rowsData.filter((r) => isFlagged(r.riskScore ?? 0)).length;
//     return { all: rowsData.length, flagged, clean: rowsData.length - flagged };
//   }, [rowsData]);

//   const visibleRows = useMemo(
//     () =>
//       rowsData
//         .filter((r) => {
//           const f = isFlagged(r.riskScore ?? 0);
//           return filter === "all" || (filter === "flagged" ? f : !f);
//         })
//         .map((r) => ({ ...r, id: r.sessionId })),
//     [rowsData, filter],
//   );

//   const exportCsv = () => {
//     downloadCsv(
//       `${(a?.examTitle ?? "exam").replace(/[^\w\-]+/g, "_")}_results.csv`,
//       [
//         "Student",
//         "Score",
//         "Percentage",
//         "Result",
//         "Status",
//         "Integrity",
//         "Violations",
//         "Correct answers",
//         "Total questions",
//         "Submitted",
//       ],
//       rowsData.map((r) => [
//         r.studentName,
//         r.totalScore,
//         Number(r.percentage.toFixed(1)),
//         r.isPassed ? "Passed" : "Failed",
//         r.status === "TimedOut" ? "Timed out" : "Completed",
//         isFlagged(r.riskScore ?? 0) ? "Flagged" : "Clean",
//         r.violationCount ?? 0,
//         r.correctAnswers,
//         r.totalQuestions,
//         fmtDateTime(r.submittedAt),
//       ]),
//     );
//   };

//   const stats = [
//     {
//       label: "Students",
//       value: a?.totalStudents ?? 0,
//       decimals: 0,
//       suffix: "",
//       icon: Users,
//       tone: "bg-teal-50 text-teal-700",
//     },
//     {
//       label: "Average score",
//       value: a?.averageScore ?? 0,
//       decimals: 1,
//       suffix: "",
//       icon: Percent,
//       tone: "bg-sky-50 text-sky-700",
//     },
//     {
//       label: "Pass rate",
//       value: a?.passRate ?? 0,
//       decimals: 0,
//       suffix: "%",
//       icon: Trophy,
//       tone: "bg-emerald-50 text-emerald-700",
//     },
//     {
//       label: "Flagged",
//       value: counts.flagged,
//       decimals: 0,
//       suffix: "",
//       icon: ShieldAlert,
//       tone: "bg-red-50 text-red-700",
//     },
//   ];

//   return (
//     <div className="space-y-6">
//       <Link
//         href="/doctor/results"
//         className="inline-flex items-center gap-2 text-sm font-semibold text-slate-500 transition hover:text-[#0A2540]"
//       >
//         <ArrowLeft size={16} /> Back to results
//       </Link>

//       <PageHeader
//         title={a?.examTitle ?? "Exam results"}
//         subtitle="Completed and timed-out attempts."
//         action={
//           <button
//             onClick={exportCsv}
//             disabled={rowsData.length === 0}
//             className="inline-flex items-center gap-2 rounded-xl bg-teal-600 px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-teal-900/10 transition hover:bg-teal-700 active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-50"
//           >
//             <Download size={17} /> Export Excel
//           </button>
//         }
//       />

//       <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
//         {stats.map((s, i) => (
//           <StatCard
//             key={s.label}
//             {...s}
//             loading={s.label === "Flagged" ? results.isLoading : analysis.isLoading}
//             delay={i * 0.07}
//           />
//         ))}
//       </div>

//       <IntegrityFilter value={filter} onChange={setFilter} counts={counts} />

//       <DataTable
//         rows={visibleRows}
//         loading={results.isLoading}
//         error={results.error ? (results.error as Error).message : undefined}
//         onRetry={() => results.refetch()}
//         searchText={(r) => r.studentName}
//         placeholder="Search students..."
//         columns={[
//           {
//             header: "Student",
//             sortValue: (r) => r.studentName,
//             cell: (r) => (
//               <div className="flex items-center gap-3">
//                 <span
//                   aria-hidden
//                   className={cn(
//                     "h-9 w-1 shrink-0 rounded-full",
//                     isFlagged(r.riskScore ?? 0) ? "bg-red-500" : "bg-transparent",
//                   )}
//                 />
//                 <PersonCell
//                   first={r.studentName.split(" ")[0] ?? ""}
//                   last={r.studentName.split(" ").slice(1).join(" ")}
//                 />
//               </div>
//             ),
//           },
//           {
//             header: "Score",
//             sortValue: (r) => r.totalScore,
//             cell: (r) => (
//               <span className="font-bold text-[#0A2540]">{r.totalScore}</span>
//             ),
//           },
//           {
//             header: "Percentage",
//             sortValue: (r) => r.percentage,
//             cell: (r) => `${r.percentage.toFixed(0)}%`,
//           },
//           {
//             header: "Result",
//             cell: (r) => (
//               <div className="flex flex-wrap items-center gap-1.5">
//                 <span
//                   className={cn(
//                     "rounded-full px-2.5 py-1 text-xs font-semibold",
//                     r.isPassed
//                       ? "bg-emerald-50 text-emerald-700"
//                       : "bg-red-50 text-red-700",
//                   )}
//                 >
//                   {r.isPassed ? "Passed" : "Failed"}
//                 </span>
//                 {r.status === "TimedOut" && (
//                   <span className="rounded-full bg-amber-50 px-2.5 py-1 text-xs font-semibold text-amber-700">
//                     Timed out
//                   </span>
//                 )}
//               </div>
//             ),
//           },
//           {
//             header: "Integrity",
//             sortValue: (r) => r.riskScore ?? 0,
//             cell: (r) => (
//               <IntegrityBadge score={r.riskScore ?? 0} count={r.violationCount ?? 0} />
//             ),
//           },
//           {
//             header: "Submitted",
//             sortValue: (r) => +new Date(r.submittedAt),
//             cell: (r) => (
//               <span className="text-slate-500">
//                 {fmtDateTime(r.submittedAt)}
//               </span>
//             ),
//           },
//         ]}
//         actions={(r) => (
//           <Link
//             href={`/doctor/results/${examId}/${r.sessionId}`}
//             className="inline-flex items-center gap-1.5 rounded-lg bg-teal-50 px-3 py-1.5 text-xs font-semibold text-teal-700 transition hover:bg-teal-100"
//           >
//             <Eye size={14} /> Review
//           </Link>
//         )}
//       />
//     </div>
//   );
// }

"use client";
import { use, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import {
  ArrowLeft,
  ChevronLeft,
  ChevronRight,
  Download,
  Eye,
  Percent,
  Search,
  ShieldAlert,
  ShieldCheck,
  Trophy,
  Users,
} from "lucide-react";
import { PersonCell } from "@/components/ui/data-table";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { ErrorState } from "@/components/ui/states";
import { IntegrityBadge } from "@/components/proctoring/integrity-badge";
import { doctorApi } from "@/lib/api/doctor";
import { fmtDateTime } from "@/lib/format";
import { isFlagged } from "@/lib/proctoring/events";
import { cn } from "@/lib/utils";
import { downloadCsv } from "@/lib/export-csv";
import type { LucideIcon } from "lucide-react";

type Row = NonNullable<ReturnType<typeof doctorApi.results> extends Promise<infer T> ? T : never>[number];

const PAGE_SIZE = 6;

type Sort = "risk" | "name" | "score" | "recent";
const SORTS: { id: Sort; label: string }[] = [
  { id: "risk", label: "Highest risk" },
  { id: "name", label: "Name" },
  { id: "score", label: "Score" },
  { id: "recent", label: "Most recent" },
];

function compare(sort: Sort) {
  return (a: Row, b: Row) => {
    switch (sort) {
      case "name":
        return a.studentName.localeCompare(b.studentName);
      case "score":
        return b.percentage - a.percentage;
      case "recent":
        return +new Date(b.submittedAt) - +new Date(a.submittedAt);
      default:
        return (b.riskScore ?? 0) - (a.riskScore ?? 0) || a.studentName.localeCompare(b.studentName);
    }
  };
}

function StudentCard({ r, examId, tone }: { r: Row; examId: number; tone: "red" | "green" }) {
  return (
    <li
      className={cn(
        "rounded-2xl border bg-white p-4 transition hover:shadow-md",
        tone === "red" ? "border-red-200 hover:shadow-red-100" : "border-emerald-200 hover:shadow-emerald-100"
      )}
    >
      <div className="flex items-start justify-between gap-3">
        <PersonCell
          first={r.studentName.split(" ")[0] ?? ""}
          last={r.studentName.split(" ").slice(1).join(" ")}
        />
        <Link
          href={`/doctor/results/${examId}/${r.sessionId}`}
          className="inline-flex shrink-0 items-center gap-1.5 rounded-lg bg-teal-50 px-3 py-1.5 text-xs font-semibold text-teal-700 transition hover:bg-teal-100"
        >
          <Eye size={14} /> Review
        </Link>
      </div>

      <div className="mt-3 flex flex-wrap items-center gap-1.5">
        <IntegrityBadge score={r.riskScore ?? 0} count={r.violationCount ?? 0} />
        <span
          className={cn(
            "rounded-full px-2.5 py-1 text-xs font-semibold",
            r.isPassed ? "bg-emerald-50 text-emerald-700" : "bg-red-50 text-red-700"
          )}
        >
          {r.isPassed ? "Passed" : "Failed"}
        </span>
        {r.status === "TimedOut" && (
          <span className="rounded-full bg-amber-50 px-2.5 py-1 text-xs font-semibold text-amber-700">Timed out</span>
        )}
      </div>

      <dl className="mt-3 grid grid-cols-3 gap-2 rounded-xl bg-slate-50 p-3 text-center">
        <div>
          <dt className="text-[11px] font-medium text-slate-400">Score</dt>
          <dd className="font-bold text-[#0A2540]">{r.totalScore}</dd>
        </div>
        <div>
          <dt className="text-[11px] font-medium text-slate-400">Percentage</dt>
          <dd className="font-bold text-[#0A2540]">{r.percentage.toFixed(0)}%</dd>
        </div>
        <div>
          <dt className="text-[11px] font-medium text-slate-400">Correct</dt>
          <dd className="font-bold text-[#0A2540]">
            {r.correctAnswers}/{r.totalQuestions}
          </dd>
        </div>
      </dl>

      <p className="mt-2 text-xs text-slate-400">Submitted {fmtDateTime(r.submittedAt)}</p>
    </li>
  );
}

function Column({
  title,
  subtitle,
  icon: Icon,
  tone,
  rows,
  examId,
  emptyText,
}: {
  title: string;
  subtitle: string;
  icon: LucideIcon;
  tone: "red" | "green";
  rows: Row[];
  examId: number;
  emptyText: string;
}) {
  const [page, setPage] = useState(1);
  const pages = Math.max(1, Math.ceil(rows.length / PAGE_SIZE));
  const current = Math.min(page, pages);

  // go back to page 1 when the list changes (search / sort)
  useEffect(() => setPage(1), [rows.length]);

  const start = (current - 1) * PAGE_SIZE;
  const visible = rows.slice(start, start + PAGE_SIZE);

  const red = tone === "red";

  return (
    <section
      className={cn(
        "flex min-w-0 flex-col rounded-3xl border-2 p-4 sm:p-5",
        red ? "border-red-200 bg-red-50/40" : "border-emerald-200 bg-emerald-50/40"
      )}
    >
      <header className="mb-4 flex items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <span
            className={cn(
              "flex h-10 w-10 shrink-0 items-center justify-center rounded-xl",
              red ? "bg-red-100 text-red-700" : "bg-emerald-100 text-emerald-700"
            )}
          >
            <Icon size={20} />
          </span>
          <div className="min-w-0">
            <h3 className={cn("font-bold", red ? "text-red-800" : "text-emerald-800")}>{title}</h3>
            <p className="truncate text-xs text-slate-500">{subtitle}</p>
          </div>
        </div>
        <span
          className={cn(
            "shrink-0 rounded-full px-3 py-1 text-sm font-bold",
            red ? "bg-red-600 text-white" : "bg-emerald-600 text-white"
          )}
        >
          {rows.length}
        </span>
      </header>

      {rows.length === 0 ? (
        <p className="rounded-2xl border border-dashed border-slate-300 bg-white/60 px-4 py-12 text-center text-sm text-slate-400">
          {emptyText}
        </p>
      ) : (
        <>
          <ul className="space-y-3">
            {visible.map((r) => (
              <StudentCard key={r.sessionId} r={r} examId={examId} tone={tone} />
            ))}
          </ul>

          {rows.length > PAGE_SIZE && (
            <div className="mt-4 flex items-center justify-between gap-3 text-sm text-slate-500">
              <span>
                {start + 1} to {Math.min(start + PAGE_SIZE, rows.length)} of {rows.length}
              </span>
              <div className="flex gap-1">
                <button
                  onClick={() => setPage(current - 1)}
                  disabled={current <= 1}
                  className="rounded-lg border border-slate-200 bg-white p-1.5 transition hover:bg-slate-50 disabled:opacity-40"
                  aria-label="Previous page"
                >
                  <ChevronLeft size={16} />
                </button>
                <button
                  onClick={() => setPage(current + 1)}
                  disabled={current >= pages}
                  className="rounded-lg border border-slate-200 bg-white p-1.5 transition hover:bg-slate-50 disabled:opacity-40"
                  aria-label="Next page"
                >
                  <ChevronRight size={16} />
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </section>
  );
}

export default function ExamResultsPage({
  params,
}: {
  params: Promise<{ examId: string }>;
}) {
  const { examId: raw } = use(params);
  const examId = Number(raw);
  const [q, setQ] = useState("");
  const [sort, setSort] = useState<Sort>("risk");

  const results = useQuery({
    queryKey: ["doctor", "results", examId],
    queryFn: () => doctorApi.results(examId),
  });
  const analysis = useQuery({
    queryKey: ["doctor", "analysis", examId],
    queryFn: () => doctorApi.analysis(examId),
  });

  const a = analysis.data;
  const rowsData = useMemo(() => results.data ?? [], [results.data]);

  // search applies to both columns; the counts in the header follow the search
  const { flagged, clean } = useMemo(() => {
    const s = q.trim().toLowerCase();
    const list = (s ? rowsData.filter((r) => r.studentName.toLowerCase().includes(s)) : rowsData)
      .slice()
      .sort(compare(sort));
    return {
      flagged: list.filter((r) => isFlagged(r.riskScore ?? 0)),
      clean: list.filter((r) => !isFlagged(r.riskScore ?? 0)),
    };
  }, [rowsData, q, sort]);

  const totalFlagged = useMemo(() => rowsData.filter((r) => isFlagged(r.riskScore ?? 0)).length, [rowsData]);

  const exportCsv = () => {
    downloadCsv(
      `${(a?.examTitle ?? "exam").replace(/[^\w\-]+/g, "_")}_results.csv`,
      [
        "Student",
        "Score",
        "Percentage",
        "Result",
        "Status",
        "Integrity",
        "Violations",
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
        isFlagged(r.riskScore ?? 0) ? "Flagged" : "Clean",
        r.violationCount ?? 0,
        r.correctAnswers,
        r.totalQuestions,
        fmtDateTime(r.submittedAt),
      ]),
    );
  };

  const stats = [
    { label: "Students", value: a?.totalStudents ?? 0, decimals: 0, suffix: "", icon: Users, tone: "bg-teal-50 text-teal-700" },
    { label: "Average score", value: a?.averageScore ?? 0, decimals: 1, suffix: "", icon: Percent, tone: "bg-sky-50 text-sky-700" },
    { label: "Pass rate", value: a?.passRate ?? 0, decimals: 0, suffix: "%", icon: Trophy, tone: "bg-emerald-50 text-emerald-700" },
    { label: "Flagged", value: totalFlagged, decimals: 0, suffix: "", icon: ShieldAlert, tone: "bg-red-50 text-red-700" },
  ];

  const searching = q.trim() !== "";

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

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {stats.map((s, i) => (
          <StatCard
            key={s.label}
            {...s}
            loading={s.label === "Flagged" ? results.isLoading : analysis.isLoading}
            delay={i * 0.07}
          />
        ))}
      </div>

      {/* search and sorting apply to both columns */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative w-full max-w-sm">
          <Search size={18} className="pointer-events-none absolute inset-y-0 start-3.5 my-auto text-slate-400" />
          <input
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Search students..."
            className="w-full rounded-xl border border-slate-200 bg-white py-2.5 ps-10 pe-4 text-sm outline-none transition focus:border-teal-600 focus:ring-4 focus:ring-teal-100"
          />
        </div>
        <label className="flex items-center gap-2 text-sm text-slate-500">
          Sort by
          <select
            value={sort}
            onChange={(e) => setSort(e.target.value as Sort)}
            className="rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm outline-none focus:border-teal-600"
          >
            {SORTS.map((s) => (
              <option key={s.id} value={s.id}>
                {s.label}
              </option>
            ))}
          </select>
        </label>
      </div>

      {results.isLoading ? (
        <div className="grid gap-5 lg:grid-cols-2">
          {[0, 1].map((i) => (
            <div key={i} className="h-72 animate-pulse rounded-3xl bg-slate-100" />
          ))}
        </div>
      ) : results.error ? (
        <ErrorState
          title="Couldn’t load the results"
          message={(results.error as Error).message}
          onRetry={() => results.refetch()}
        />
      ) : (
        <div className="grid items-start gap-5 lg:grid-cols-2">
          <Column
            title="Flagged"
            subtitle="Suspicious activity was recorded"
            icon={ShieldAlert}
            tone="red"
            rows={flagged}
            examId={examId}
            emptyText={searching ? "No flagged student matches your search." : "No flagged students. Nice."}
          />
          <Column
            title="Clean"
            subtitle="No suspicious activity"
            icon={ShieldCheck}
            tone="green"
            rows={clean}
            examId={examId}
            emptyText={searching ? "No clean student matches your search." : "No students yet."}
          />
        </div>
      )}
    </div>
  );
}