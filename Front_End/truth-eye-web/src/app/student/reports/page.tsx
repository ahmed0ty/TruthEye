"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { FileSpreadsheet, FileText, FilePlus2, Loader2 } from "lucide-react";
import { EmptyState } from "@/components/ui/empty-state";
import { PageHeader } from "@/components/ui/page-header";
import { ErrorState } from "@/components/ui/states";
import { reportsApi } from "@/lib/api/reports";
import { studentApi } from "@/lib/api/student";
import { downloadFile } from "@/lib/api/download";
import { useAuth } from "@/lib/auth/store";
import { fmtDateTime, fmtSeconds, parseTimeSpan } from "@/lib/format";

export default function ReportsPage() {
  const user = useAuth((s) => s.user);
  const qc = useQueryClient();
  const [busy, setBusy] = useState<string | null>(null);

  const reports = useQuery({ queryKey: ["student", "reports"], queryFn: reportsApi.mine });
  const history = useQuery({ queryKey: ["student", "history"], queryFn: studentApi.history });

  const generate = useMutation({
    mutationFn: (examId: number) => reportsApi.generate(user!.id, examId),
    onSuccess: () => {
      toast.success("Report generated");
      qc.invalidateQueries({ queryKey: ["student", "reports"] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const download = async (id: number, kind: "pdf" | "excel") => {
    const key = `${id}-${kind}`;
    setBusy(key);
    try {
      await downloadFile(`/api/Report/${id}/${kind}`, `report_${id}.${kind === "pdf" ? "pdf" : "xlsx"}`);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Download failed");
    } finally {
      setBusy(null);
    }
  };

  const done = (history.data ?? []).filter((s) => s.status !== "InProgress");
  const reportedExams = new Set((reports.data ?? []).map((r) => r.examId));
  const pending = done.filter((s) => !reportedExams.has(s.examId));
  const rows = [...(reports.data ?? [])].sort((a, b) => b.id - a.id);

  return (
    <div className="space-y-6">
      <PageHeader title="Reports" subtitle="Generate and download a report for each exam you completed." />

      {pending.length > 0 && (
        <section className="rounded-2xl border border-slate-200 bg-white p-5">
          <h3 className="mb-3 font-bold text-[#0A2540]">Ready to generate</h3>
          <ul className="space-y-2">
            {pending.map((s) => (
              <li key={s.id} className="flex items-center justify-between gap-3 rounded-xl bg-slate-50 px-4 py-3">
                <span className="truncate text-sm font-semibold text-[#0A2540]">{s.examTitle ?? `Exam #${s.examId}`}</span>
                <button
                  onClick={() => generate.mutate(s.examId)}
                  disabled={generate.isPending}
                  className="inline-flex items-center gap-1.5 rounded-lg bg-teal-600 px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-teal-700 disabled:opacity-60"
                >
                  {generate.isPending && generate.variables === s.examId ? <Loader2 size={14} className="animate-spin" /> : <FilePlus2 size={14} />}
                  Generate
                </button>
              </li>
            ))}
          </ul>
        </section>
      )}

      {reports.isLoading ? (
        <div className="space-y-3">{[0, 1].map((i) => <div key={i} className="h-24 animate-pulse rounded-2xl bg-slate-100" />)}</div>
      ) : reports.isError ? (
        <ErrorState title="Couldn’t load reports" message={(reports.error as Error).message} onRetry={() => reports.refetch()} />
      ) : rows.length === 0 ? (
        <EmptyState icon={FileText} title="No reports yet" text="Complete an exam, then generate its report here." />
      ) : (
        <ul className="space-y-3">
          {rows.map((r) => (
            <li key={r.id} className="flex flex-wrap items-center justify-between gap-4 rounded-2xl border border-slate-200 bg-white p-5">
              <div className="min-w-0">
                <p className="truncate font-bold text-[#0A2540]">{r.exam?.title ?? `Exam #${r.examId}`}</p>
                <p className="text-xs text-slate-500">
                  Score {r.score.toFixed(0)}% · {r.aiAlertsCount} alerts · {fmtSeconds(parseTimeSpan(r.duration))} · {fmtDateTime(r.generatedAt)}
                </p>
              </div>
              <div className="flex gap-2">
                {(["pdf", "excel"] as const).map((k) => (
                  <button
                    key={k}
                    onClick={() => download(r.id, k)}
                    disabled={busy === `${r.id}-${k}`}
                    className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-semibold text-slate-700 transition hover:bg-slate-50 disabled:opacity-60"
                  >
                    {busy === `${r.id}-${k}` ? <Loader2 size={14} className="animate-spin" /> : k === "pdf" ? <FileText size={14} /> : <FileSpreadsheet size={14} />}
                    {k === "pdf" ? "PDF" : "Excel"}
                  </button>
                ))}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}