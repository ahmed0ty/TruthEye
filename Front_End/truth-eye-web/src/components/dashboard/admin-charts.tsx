"use client";

import { useQuery } from "@tanstack/react-query";
import { ChartCard, ChartEmpty, ColumnChart, Donut, HBarChart } from "@/components/charts/charts";
import { adminApi } from "@/lib/api/admin";
import { YEARS, labelOf } from "@/lib/enums";

export function AdminCharts() {
  const students = useQuery({ queryKey: ["students"], queryFn: adminApi.studentList });
  const list = students.data ?? [];

  if (students.isLoading) return <div className="h-72 animate-pulse rounded-2xl bg-slate-100" />;

  const byDept = new Map<string, number>();
  const byYear = new Map<number, number>();
  list.forEach((s) => {
    const d = s.departmentName || "Unassigned";
    byDept.set(d, (byDept.get(d) ?? 0) + 1);
    byYear.set(s.year, (byYear.get(s.year) ?? 0) + 1);
  });

  const deptData = [...byDept.entries()].map(([label, value]) => ({ label, value })).sort((a, b) => b.value - a.value).slice(0, 8);
  const yearData = [...byYear.entries()].sort((a, b) => a[0] - b[0]).map(([y, value]) => ({ label: labelOf(YEARS, y), value }));
  const active = list.filter((s) => s.isActive).length;

  return (
    <div className="grid gap-4 lg:grid-cols-3">
      <ChartCard title="Students by department">
        {deptData.length ? <HBarChart data={deptData} /> : <ChartEmpty />}
      </ChartCard>
      <ChartCard title="Students by year">
        {yearData.length ? <ColumnChart data={yearData} /> : <ChartEmpty />}
      </ChartCard>
      <ChartCard title="Active students">
        {list.length ? <Donut value={active} total={list.length} label="active" /> : <ChartEmpty />}
      </ChartCard>
    </div>
  );
}