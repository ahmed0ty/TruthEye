"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import { resumeApi } from "@/lib/api/resume";

export function ResumeRequest({ sessionId }: { sessionId: number }) {
  const [reason, setReason] = useState("");

  const status = useQuery({
    queryKey: ["resume", sessionId],
    queryFn: () => resumeApi.status(sessionId),
    refetchInterval: (q) => (q.state.data?.status === "Pending" ? 5000 : false),
  });

  const ask = useMutation({
    mutationFn: () => resumeApi.request(sessionId, reason),
    onSuccess: () => { toast.success("Request sent"); status.refetch(); },
    onError: (e: Error) => toast.error(e.message),
  });

  // Approved: clear the "left the exam" marker and do a FULL reload.
  // router.replace to the same path would not reload the page, so the "exam ended" screen would stay,
  // and a leftover marker would end the reopened session again as soon as the page loads.
  useEffect(() => {
    if (status.data?.status === "Approved") {
      try { sessionStorage.removeItem(`te-exam-left-${sessionId}`); } catch { /* ignore */ }
      window.location.assign(`/exam/${sessionId}`);
    }
  }, [status.data?.status, sessionId]);

  const d = status.data;
  if (!d) return null;
  if (d.status === "Pending") return <p className="rounded-xl bg-amber-50 p-4 text-sm text-amber-800">Waiting for your doctor or proctor to approve. Keep this page open.</p>;
  if (d.status === "Rejected") return <p className="rounded-xl bg-red-50 p-4 text-sm text-red-700">Your request was rejected. {d.note}</p>;
  if (!d.canRequest) return null;

  return (
    <div className="space-y-3 rounded-xl border border-slate-200 bg-white p-4">
      <p className="text-sm font-semibold text-slate-700">Lost your connection? You can ask to continue (once). Your time keeps running.</p>
      <textarea value={reason} onChange={(e) => setReason(e.target.value)} rows={3} maxLength={500}
        placeholder="What happened?" className="w-full rounded-xl border border-slate-200 p-3 text-sm outline-none focus:border-teal-600" />
      <button type="button" disabled={reason.trim().length < 5 || ask.isPending} onClick={() => ask.mutate()}
        className="rounded-xl bg-teal-700 px-4 py-2 text-sm font-bold text-white hover:bg-teal-800 disabled:opacity-50">
        Request to continue
      </button>
    </div>
  );
}