"use client";

import { useQueries } from "@tanstack/react-query";
import { ChartCard, ChartEmpty, ColumnChart, Donut, HBarChart } from "@/components/charts/charts";
import { doctorApi } from "@/lib/api/doctor";
import type { DoctorExam } from "@/types/doctor";

const BUCKETS = [
  { label: "<50", min: 0, max: 50 },
  { label: "50s", min: 50, max: 60 },
  { label: "60s", min: 60, max: 70 },
  { label: "70s", min: 70, max: 80 },
  { label: "80s", min: 80, max: 90 },
  { label: "90+", min: 90, max: 101 },
];

export function DoctorCharts({ exams }: { exams: DoctorExam[] }) {
  const recent = [...exams].sort((a, b) => +new Date(b.startTime) - +new Date(a.startTime)).slice(0, 8);

  const queries = useQueries({
    queries: recent.map((e) => ({
      queryKey: ["doctor", "results", e.id],
      queryFn: () => doctorApi.results(e.id),
      staleTime: 60_000,
      retry: false,
    })),
  });

  if (recent.length === 0) return null;

  const loading = queries.some((q) => q.isLoading);
  const perExam = recent.map((e, i) => ({ exam: e, rows: queries[i]?.data ?? [] }));
  const all = perExam.flatMap((p) => p.rows);
  const passed = all.filter((r) => r.isPassed).length;

  const distribution = BUCKETS.map((b) => ({
    label: b.label,
    value: all.filter((r) => r.percentage >= b.min && r.percentage < b.max).length,
  }));

  const averages = perExam
    .filter((p) => p.rows.length)
    .map((p) => ({
      label: p.exam.title,
      value: Math.round(p.rows.reduce((a, r) => a + r.percentage, 0) / p.rows.length),
    }));

  if (loading) return <div className="h-72 animate-pulse rounded-2xl bg-slate-100" />;

  const empty = all.length === 0;

  return (
    <div className="grid gap-4 lg:grid-cols-3">
      <ChartCard title="Score distribution">
        {empty ? <ChartEmpty text="Charts appear once students submit" /> : <ColumnChart data={distribution} />}
      </ChartCard>
      <ChartCard title="Pass rate">
        {empty ? <ChartEmpty /> : <Donut value={passed} total={all.length} label="passed" />}
      </ChartCard>
      <ChartCard title="Average score by exam">
        {averages.length === 0 ? <ChartEmpty /> : <HBarChart data={averages} suffix="%" max={100} />}
      </ChartCard>
    </div>
  );
}