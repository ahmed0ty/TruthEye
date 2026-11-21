"use client";

import { useState } from "react";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { DataTable } from "@/components/ui/data-table";
import { FilterSelect } from "@/components/ui/filter-select";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { EvidenceImage } from "@/components/review/evidence-image";
import { useServerTable } from "@/hooks/use-server-table";
import { reviewApi } from "@/lib/api/reviews";
import type { ReviewRow } from "@/types/admin";

const LEVEL_TONE: Record<string, string> = {
  High: "bg-red-50 text-red-700",
  Medium: "bg-amber-50 text-amber-700",
  Low: "bg-sky-50 text-sky-700",
  None: "bg-slate-100 text-slate-500",
};
const DECISION_TONE: Record<string, string> = {
  Pending: "bg-slate-100 text-slate-600",
  Cleared: "bg-emerald-50 text-emerald-700",
  Confirmed: "bg-red-50 text-red-700",
};

const Badge = ({ text, tone }: { text: string; tone: string }) => (
  <span className={`rounded-full px-2.5 py-1 text-xs font-bold ${tone}`}>{text}</span>
);

function ReviewDialog({ id, onClose }: { id: number | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [note, setNote] = useState("");

  const detail = useQuery({
    queryKey: ["review", id],
    queryFn: () => reviewApi.get(id!),
    enabled: id !== null,
  });

  const decide = useMutation({
    mutationFn: (decision: "Cleared" | "Confirmed") => reviewApi.decide(id!, { decision, note }),
    onSuccess: (d) => {
      toast.success(d.decision === "Confirmed" ? "Violation confirmed" : "Session cleared");
      qc.invalidateQueries({ queryKey: ["reviews"] });
      qc.invalidateQueries({ queryKey: ["review", id] });
      qc.invalidateQueries({ queryKey: ["activity"] });
      onClose();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const d = detail.data;
  const evidence = d?.events.filter((e) => e.evidence) ?? [];

  return (
    <Modal open={id !== null} title="Review session" onClose={onClose}>
      {detail.isLoading || !d ? (
        <p className="py-8 text-center text-sm text-slate-500">{detail.error ? (detail.error as Error).message : "Loading..."}</p>
      ) : (
        <div className="space-y-5">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div>
              <p className="font-bold text-slate-800">{d.studentName}</p>
              <p className="text-sm text-slate-500">{d.examTitle} · {d.sessionStatus} · {d.percentage.toFixed(0)}%</p>
            </div>
            <div className="flex gap-2">
              <Badge text={`${d.riskLevel} · ${d.riskScore}`} tone={LEVEL_TONE[d.riskLevel]} />
              <Badge text={d.decision} tone={DECISION_TONE[d.decision]} />
            </div>
          </div>

          <div>
            <h3 className="mb-2 text-sm font-bold text-slate-700">Timeline ({d.events.length})</h3>
            {d.events.length === 0 ? (
              <p className="text-sm text-slate-500">No events were recorded.</p>
            ) : (
              <ul className="max-h-48 space-y-1 overflow-y-auto">
                {d.events.map((e) => (
                  <li key={e.id} className="flex items-center justify-between rounded-lg bg-slate-50 px-3 py-1.5 text-sm">
                    <span className="font-semibold text-slate-700">{e.eventType}</span>
                    <span className="text-xs text-slate-500">
                      +{e.weight} · {(e.confidence * 100).toFixed(0)}% · {new Date(e.timestamp).toLocaleTimeString()}
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </div>

          {evidence.length > 0 && (
            <div>
              <h3 className="mb-2 text-sm font-bold text-slate-700">Evidence ({evidence.length})</h3>
              <div className="grid max-h-64 grid-cols-2 gap-2 overflow-y-auto sm:grid-cols-3">
                {evidence.map((e) => (
                  <div key={e.id}>
                    <EvidenceImage name={e.evidence!} alt={e.eventType} />
                    <p className="mt-1 truncate text-xs text-slate-500">{e.eventType}</p>
                  </div>
                ))}
              </div>
            </div>
          )}

          {d.note && (
            <p className="rounded-lg bg-slate-50 p-3 text-sm text-slate-600">
              <b>Note:</b> {d.note}
            </p>
          )}

          {d.canDecide ? (
            <div className="space-y-3 border-t border-slate-100 pt-4">
              <textarea
                value={note}
                onChange={(e) => setNote(e.target.value)}
                maxLength={1000}
                rows={3}
                placeholder="Note (required to confirm a violation)"
                className="w-full rounded-xl border border-slate-200 p-3 text-sm outline-none focus:border-teal-600 focus:ring-4 focus:ring-teal-100"
              />
              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  disabled={decide.isPending}
                  onClick={() => decide.mutate("Cleared")}
                  className="rounded-xl border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-60"
                >
                  Clear (ignore)
                </button>
                <button
                  type="button"
                  disabled={decide.isPending || note.trim().length < 5}
                  onClick={() => decide.mutate("Confirmed")}
                  className="rounded-xl bg-red-600 px-4 py-2 text-sm font-bold text-white hover:bg-red-700 disabled:opacity-50"
                >
                  Confirm violation
                </button>
              </div>
            </div>
          ) : (
            <p className="border-t border-slate-100 pt-3 text-xs text-slate-400">View only.</p>
          )}
        </div>
      )}
    </Modal>
  );
}

export function ReviewsView() {
  const table = useServerTable(10);
  const [openId, setOpenId] = useState<number | null>(null);

  const { data, isLoading, isFetching, error, refetch } = useQuery({
    queryKey: ["reviews", table.params],
    queryFn: () => reviewApi.list(table.params),
    placeholderData: keepPreviousData,
  });

  return (
    <div className="space-y-6">
      <PageHeader title="Session Review" subtitle="Finished exam sessions ranked by risk. Open one to see the evidence and decide." />

      <DataTable<ReviewRow & { id: number }>
        rows={(data?.items ?? []).map((r) => ({ ...r, id: r.sessionId }))}
        loading={isLoading}
        error={error ? (error as Error).message : undefined}
        onRetry={() => refetch()}
        server={table.bind(data?.total ?? 0, isFetching)}
        placeholder="Search is not available here"
        toolbar={
          <>
            <FilterSelect label="Risk" value={table.filters.level ?? "all"} onChange={(v) => table.setFilter("level", v)}
              options={["High", "Medium", "Low"].map((x) => ({ value: x, label: x }))} />
            <FilterSelect label="Decision" value={table.filters.decision ?? "all"} onChange={(v) => table.setFilter("decision", v)}
              options={["Pending", "Cleared", "Confirmed"].map((x) => ({ value: x, label: x }))} />
          </>
        }
        columns={[
          { header: "Student", cell: (r) => <span className="font-semibold text-slate-800">{r.studentName}</span> },
          { header: "Exam", cell: (r) => r.examTitle },
          { header: "Risk", cell: (r) => <Badge text={`${r.riskLevel} · ${r.riskScore}`} tone={LEVEL_TONE[r.riskLevel]} /> },
          { header: "Events", cell: (r) => `${r.violationCount} (${r.evidenceCount} photos)` },
          { header: "Decision", cell: (r) => <Badge text={r.decision} tone={DECISION_TONE[r.decision]} /> },
        ]}
        actions={(r) => (
          <button type="button" onClick={() => setOpenId(r.sessionId)}
            className="rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50">
            Review
          </button>
        )}
      />

      <ReviewDialog id={openId} onClose={() => setOpenId(null)} />
    </div>
  );
}