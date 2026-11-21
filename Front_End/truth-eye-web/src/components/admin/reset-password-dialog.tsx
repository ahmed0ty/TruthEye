"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import { Modal } from "@/components/ui/modal";
import { adminApi } from "@/lib/api/admin";

export interface ResetTarget {
  id: number;
  name: string;
}

export function ResetPasswordDialog({ target, onClose }: { target: ResetTarget | null; onClose: () => void }) {
  const [sent, setSent] = useState(false);

  useEffect(() => {
    setSent(false);
  }, [target]);

  // The masked email is shown first so the admin can confirm it before anything is sent
  const preview = useQuery({
    queryKey: ["reset-preview", target?.id],
    queryFn: () => adminApi.resetPreview(target!.id),
    enabled: !!target && !sent,
    retry: false,
  });

  const reset = useMutation({
    mutationFn: (id: number) => adminApi.resetPassword(id),
    onSuccess: () => setSent(true),
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <Modal open={!!target} title="Reset password" onClose={onClose}>
      {sent ? (
        <div className="space-y-4">
          <p className="text-sm text-slate-600">
            A reset link was sent to <b dir="ltr">{preview.data?.email}</b>. It works once and expires in 30 minutes.
            The account&apos;s current sessions were signed out.
          </p>
          <div className="flex justify-end">
            <button type="button" onClick={onClose} className="rounded-xl bg-teal-700 px-4 py-2 text-sm font-bold text-white hover:bg-teal-800">
              Done
            </button>
          </div>
        </div>
      ) : (
        <div className="space-y-4">
          <p className="text-sm text-slate-600">
            Send a password reset link for <b>{target?.name}</b> to this email:
          </p>
          <div dir="ltr" className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 font-semibold text-slate-900">
            {preview.isLoading ? "..." : preview.isError ? "Couldn't load the email" : preview.data?.email}
          </div>
          <p className="text-xs text-slate-500">
            Check that it is correct before sending. The link works once and expires in 30 minutes.
          </p>
          <div className="flex justify-end gap-2">
            <button type="button" onClick={onClose} className="rounded-xl border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50">
              Cancel
            </button>
            <button
              type="button"
              disabled={reset.isPending || !preview.data || !target}
              onClick={() => target && reset.mutate(target.id)}
              className="rounded-xl bg-teal-700 px-4 py-2 text-sm font-bold text-white hover:bg-teal-800 disabled:opacity-60"
            >
              {reset.isPending ? "Sending..." : "Send reset link"}
            </button>
          </div>
        </div>
      )}
    </Modal>
  );
}
