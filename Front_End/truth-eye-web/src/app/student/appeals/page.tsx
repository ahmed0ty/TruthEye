"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { appealApi } from "@/lib/api/grading";
import type { StudentAppealItem } from "@/types/grading";

const TONE: Record<string, string> = {
  Pending: "bg-amber-50 text-amber-700",
  Accepted: "bg-emerald-50 text-emerald-700",
  Rejected: "bg-red-50 text-red-700",
};

export default function StudentAppealsPage() {
  const qc = useQueryClient();
  const [target, setTarget] = useState<StudentAppealItem | null>(null);
  const [message, setMessage] = useState("");

  const { data, isLoading, error } = useQuery({ queryKey: ["my-appeals"], queryFn: appealApi.mine });

  const create = useMutation({
    mutationFn: () => appealApi.create({ examSessionId: target!.sessionId, message }),
    onSuccess: () => {
      toast.success("Appeal submitted");
      qc.invalidateQueries({ queryKey: ["my-appeals"] });
      setTarget(null);
      setMessage("");
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <div className="space-y-6">
      <PageHeader title="Appeals" subtitle="Object to a result within 7 days after the exam ends. One appeal per exam." />

      {isLoading && <p className="text-sm text-slate-500">Loading...</p>}
      {error && <p className="text-sm text-red-600">{(error as Error).message}</p>}
      {data && data.length === 0 && <p className="py-10 text-center text-sm text-slate-500">No finished exams yet.</p>}

      <div className="space-y-3">
        {data?.map((i) => (
          <div key={i.sessionId} className="space-y-2 rounded-2xl border border-slate-200 bg-white p-4">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div>
                <p className="font-bold text-slate-800">{i.examTitle}</p>
                <p className="text-sm text-slate-500">{i.percentage.toFixed(0)}% · {i.isPassed ? "Passed" : "Failed"}</p>
              </div>
              {i.status ? (
                <span className={`rounded-full px-2.5 py-1 text-xs font-bold ${TONE[i.status]}`}>{i.status}</span>
              ) : i.canAppeal ? (
                <button type="button" onClick={() => { setTarget(i); setMessage(""); }}
                  className="rounded-xl bg-teal-700 px-3 py-1.5 text-xs font-bold text-white hover:bg-teal-800">
                  Appeal
                </button>
              ) : (
                <span className="text-xs text-slate-400">Appeal period ended</span>
              )}
            </div>
            {i.message && <p className="rounded-lg bg-slate-50 p-3 text-sm text-slate-700"><b>Your appeal:</b> {i.message}</p>}
            {i.response && <p className="rounded-lg bg-teal-50 p-3 text-sm text-slate-700"><b>Response:</b> {i.response}</p>}
          </div>
        ))}
      </div>

      <Modal open={!!target} title={`Appeal: ${target?.examTitle ?? ""}`} onClose={() => setTarget(null)}>
        <div className="space-y-4">
          <textarea
            value={message} onChange={(e) => setMessage(e.target.value)} maxLength={1000} rows={5}
            placeholder="Explain what you think is wrong with your result"
            className="w-full rounded-xl border border-slate-200 p-3 text-sm outline-none focus:border-teal-600 focus:ring-4 focus:ring-teal-100"
          />
          <div className="flex justify-end">
            <button type="button" disabled={create.isPending || message.trim().length < 10}
              onClick={() => create.mutate()}
              className="rounded-xl bg-teal-700 px-4 py-2 text-sm font-bold text-white hover:bg-teal-800 disabled:opacity-50">
              {create.isPending ? "Sending..." : "Submit appeal"}
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
}