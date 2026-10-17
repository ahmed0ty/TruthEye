"use client";

import { Loader2, TriangleAlert } from "lucide-react";
import { Modal } from "./modal";

export function Confirm({
  open,
  title,
  text,
  loading,
  onCancel,
  onConfirm,
}: {
  open: boolean;
  title: string;
  text: string;
  loading?: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  return (
    <Modal open={open} title={title} onClose={onCancel}>
      <div className="flex gap-4">
        <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-red-50 text-red-600">
          <TriangleAlert size={24} />
        </div>
        <p className="pt-1 text-sm text-slate-600">{text}</p>
      </div>
      <div className="mt-6 flex justify-end gap-3">
        <button onClick={onCancel} className="rounded-xl border border-slate-200 px-5 py-2.5 text-sm font-bold text-slate-600 hover:bg-slate-50">
          Cancel
        </button>
        <button
          onClick={onConfirm}
          disabled={loading}
          className="flex items-center gap-2 rounded-xl bg-red-600 px-5 py-2.5 text-sm font-bold text-white transition hover:bg-red-700 disabled:opacity-60"
        >
          {loading && <Loader2 size={16} className="animate-spin" />}
          Delete
        </button>
      </div>
    </Modal>
  );
}