"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { DataTable } from "@/components/ui/data-table";
import { PageHeader } from "@/components/ui/page-header";
import { proctorApi } from "@/lib/api/proctor";
import { fmtDateTime } from "@/lib/format";
import { EVENT_META, metaOf, SEVERITY_STYLE } from "@/lib/proctoring/events";
import { cn } from "@/lib/utils";
import { EvidenceThumb } from "@/components/proctoring/evidence-thumb";

const THRESHOLDS = [0.5, 0.7, 0.9];
const select =
  "rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-teal-600 focus:ring-4 focus:ring-teal-100";

export default function AlertsPage() {
  const [threshold, setThreshold] = useState(0.5);
  const [type, setType] = useState("all");

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ["proctor", "flagged", threshold],
    queryFn: () => proctorApi.flagged(threshold),
  });

  const rows = [...(data ?? [])]
    .filter((l) => type === "all" || l.eventType === type)
    .sort((a, b) => +new Date(b.timestamp) - +new Date(a.timestamp));

  return (
    <div className="space-y-6">
      <PageHeader
        title="Alerts"
        subtitle="Every suspicious event recorded during exams."
        action={
          <div className="flex flex-wrap gap-3">
            <select value={type} onChange={(e) => setType(e.target.value)} className={select}>
              <option value="all">All event types</option>
              {Object.entries(EVENT_META).map(([k, v]) => (
                <option key={k} value={k}>{v.label}</option>
              ))}
            </select>
            <select value={threshold} onChange={(e) => setThreshold(Number(e.target.value))} className={select}>
              {THRESHOLDS.map((t) => (
                <option key={t} value={t}>Confidence ≥ {Math.round(t * 100)}%</option>
              ))}
            </select>
          </div>
        }
      />

      <DataTable
        rows={rows}
        loading={isLoading}
        error={error ? (error as Error).message : undefined}
        onRetry={() => refetch()}
        searchText={(r) => `${metaOf(r.eventType).label} ${r.examSessionId}`}
        placeholder="Search by event or session..."
        columns={[
          {
            header: "Event",
            sortValue: (r) => metaOf(r.eventType).label,
            cell: (r) => {
              const m = metaOf(r.eventType);
              return <span className={cn("rounded-full px-2.5 py-1 text-xs font-semibold", SEVERITY_STYLE[m.severity])}>{m.label}</span>;
            },
          },
          { header: "Session", sortValue: (r) => r.examSessionId, cell: (r) => <span className="font-semibold text-[#0A2540]">#{r.examSessionId}</span> },
          { header: "Confidence", sortValue: (r) => r.confidence, cell: (r) => `${Math.round(r.confidence * 100)}%` },
          { header: "Evidence", cell: (r) => <EvidenceThumb name={r.screenshotPath} /> },
          { header: "Time", sortValue: (r) => +new Date(r.timestamp), cell: (r) => <span className="text-slate-500">{fmtDateTime(r.timestamp)}</span> },
        ]}
      />
    </div>
  );
}