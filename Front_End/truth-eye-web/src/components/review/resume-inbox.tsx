"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { resumeApi } from "@/lib/api/resume";

export function ResumeInbox() {
  const qc = useQueryClient();
  const [note, setNote] = useState<Record<number, string>>({});
  const { data } = useQuery({ queryKey: ["resume-pending"], queryFn: resumeApi.pending, refetchInterval: 10_000 });

  const decide = useMutation({
    mutationFn: (v: { id: number; d: "Approved" | "Rejected" }) => resumeApi.decide(v.id, v.d, note[v.id]),
    onSuccess: () => { toast.success("Done"); qc.invalidateQueries({ queryKey: ["resume-pending"] }); },
    onError: (e: Error) => toast.error(e.message),
  });

  if (!data?.length) return null;

  return (
    <section className="space-y-3 rounded-2xl border-2 border-amber-200 bg-amber-50/50 p-4">
      <h3 className="font-bold text-amber-900">Resume requests ({data.length})</h3>
      {data.map((r) => (
        <div key={r.id} className="space-y-2 rounded-xl bg-white p-3 text-sm">
          <p className="font-semibold text-slate-800">{r.studentName} · {r.examTitle}</p>
          <p className="text-slate-600">{r.reason}</p>
          <p className="text-xs text-slate-400">Disconnected {new Date(r.disconnectedAt).toLocaleTimeString()} · expires in {Math.ceil(r.secondsLeftToDecide / 60)} min</p>
          <input value={note[r.id] ?? ""} onChange={(e) => setNote({ ...note, [r.id]: e.target.value })}
            placeholder="Note (required to reject)" className="w-full rounded-lg border border-slate-200 px-3 py-1.5 text-sm" />
          <div className="flex justify-end gap-2">
            <button type="button" onClick={() => decide.mutate({ id: r.id, d: "Rejected" })} disabled={decide.isPending || (note[r.id] ?? "").trim().length < 3}
              className="rounded-lg border border-red-200 px-3 py-1.5 text-xs font-semibold text-red-700 disabled:opacity-50">Reject</button>
            <button type="button" onClick={() => decide.mutate({ id: r.id, d: "Approved" })} disabled={decide.isPending}
              className="rounded-lg bg-teal-700 px-3 py-1.5 text-xs font-bold text-white disabled:opacity-50">Approve</button>
          </div>
        </div>
      ))}
    </section>
  );
}