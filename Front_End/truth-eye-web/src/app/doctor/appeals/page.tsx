"use client";

import { useState } from "react";
import Link from "next/link";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { FilterSelect } from "@/components/ui/filter-select";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { appealApi } from "@/lib/api/grading";
import type { AppealRow } from "@/types/grading";

const TONE: Record<string, string> = {
  Pending: "bg-amber-50 text-amber-700",
  Accepted: "bg-emerald-50 text-emerald-700",
  Rejected: "bg-red-50 text-red-700",
};

export default function DoctorAppealsPage() {
  const qc = useQueryClient();
  const [status, setStatus] = useState("Pending");
  const [page, setPage] = useState(1);
  const [target, setTarget] = useState<AppealRow | null>(null);
  const [response, setResponse] = useState("");

  const { data, isLoading, error } = useQuery({
    queryKey: ["appeals", status, page],
    queryFn: () => appealApi.list({ status: status === "all" ? undefined : status, page, pageSize: 10 }),
    placeholderData: keepPreviousData,
  });

  const respond = useMutation({
    mutationFn: (decision: "Accepted" | "Rejected") => appealApi.respond(target!.id, { decision, response }),
    onSuccess: (r) => {
      toast.success(r.status === "Accepted" ? "Appeal accepted" : "Appeal rejected");
      qc.invalidateQueries({ queryKey: ["appeals"] });
      qc.invalidateQueries({ queryKey: ["activity"] });
      setTarget(null);
      setResponse("");
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const pages = data ? Math.max(1, Math.ceil(data.total / data.pageSize)) : 1;

  return (
    <div className="space-y-6">
      <PageHeader title="Appeals" subtitle="Students' objections to their results. Accepting an appeal does not change the grade automatically." />

      <FilterSelect
        label="Status"
        value={status}
        onChange={(v) => { setPage(1); setStatus(v); }}
        options={["Pending", "Accepted", "Rejected"].map((s) => ({ value: s, label: s }))}
      />

      {isLoading && <p className="text-sm text-slate-500">Loading...</p>}
      {error && <p className="text-sm text-red-600">{(error as Error).message}</p>}
      {data && data.items.length === 0 && <p className="py-10 text-center text-sm text-slate-500">No appeals.</p>}

      <div className="space-y-3">
        {data?.items.map((a) => (
          <div key={a.id} className="space-y-2 rounded-2xl border border-slate-200 bg-white p-4">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div>
                <p className="font-bold text-slate-800">{a.studentName}</p>
                <p className="text-sm text-slate-500">{a.examTitle} · {a.percentage.toFixed(0)}% · {new Date(a.createdAt).toLocaleDateString()}</p>
              </div>
              <span className={`rounded-full px-2.5 py-1 text-xs font-bold ${TONE[a.status]}`}>{a.status}</span>
            </div>
            <p className="rounded-lg bg-slate-50 p-3 text-sm text-slate-700">{a.message}</p>
            {a.response && <p className="text-sm text-slate-500"><b>Your response:</b> {a.response}</p>}
            <div className="flex flex-wrap justify-end gap-2">
              <Link href={`/doctor/grading/${a.sessionId}`}
                className="rounded-xl border border-slate-200 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50">
                Open grading
              </Link>
              {a.status === "Pending" && (
                <button type="button" onClick={() => { setTarget(a); setResponse(""); }}
                  className="rounded-xl bg-teal-700 px-3 py-1.5 text-xs font-bold text-white hover:bg-teal-800">
                  Respond
                </button>
              )}
            </div>
          </div>
        ))}
      </div>

      {data && data.total > data.pageSize && (
        <div className="flex items-center justify-end gap-2 text-sm text-slate-600">
          <button disabled={page <= 1} onClick={() => setPage(page - 1)} className="rounded-lg border border-slate-200 px-3 py-1.5 disabled:opacity-40">Previous</button>
          <span>Page {page} of {pages}</span>
          <button disabled={page >= pages} onClick={() => setPage(page + 1)} className="rounded-lg border border-slate-200 px-3 py-1.5 disabled:opacity-40">Next</button>
        </div>
      )}

      <Modal open={!!target} title="Respond to appeal" onClose={() => setTarget(null)}>
        <div className="space-y-4">
          <p className="rounded-lg bg-slate-50 p-3 text-sm text-slate-700">{target?.message}</p>
          <textarea
            value={response} onChange={(e) => setResponse(e.target.value)} maxLength={1000} rows={4}
            placeholder="Your response to the student"
            className="w-full rounded-xl border border-slate-200 p-3 text-sm outline-none focus:border-teal-600 focus:ring-4 focus:ring-teal-100"
          />
          <div className="flex justify-end gap-2">
            <button type="button" disabled={respond.isPending || response.trim().length < 5}
              onClick={() => respond.mutate("Rejected")}
              className="rounded-xl border border-red-200 px-4 py-2 text-sm font-semibold text-red-700 hover:bg-red-50 disabled:opacity-50">
              Reject
            </button>
            <button type="button" disabled={respond.isPending || response.trim().length < 5}
              onClick={() => respond.mutate("Accepted")}
              className="rounded-xl bg-teal-700 px-4 py-2 text-sm font-bold text-white hover:bg-teal-800 disabled:opacity-50">
              Accept
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
}