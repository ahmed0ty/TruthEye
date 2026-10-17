"use client";

import { useEffect } from "react";
import { X } from "lucide-react";

export function Modal({
  open,
  title,
  onClose,
  children,
  wide,
}: {
  open: boolean;
  title: string;
  onClose: () => void;
  children: React.ReactNode;
  wide?: boolean;
}) {
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && onClose();
    document.addEventListener("keydown", onKey);
    document.body.style.overflow = "hidden";
    return () => {
      document.removeEventListener("keydown", onKey);
      document.body.style.overflow = "";
    };
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center sm:items-center sm:p-4">
      <div onClick={onClose} className="animate-fade-in absolute inset-0 bg-slate-950/60 backdrop-blur-sm" />
      <div
        role="dialog"
        aria-modal="true"
        className={`animate-pop relative flex max-h-[92dvh] w-full flex-col rounded-t-3xl bg-white shadow-2xl sm:rounded-3xl ${wide ? "max-w-3xl" : "max-w-lg"}`}
      >
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-5">
          <h3 className="text-lg font-extrabold text-slate-900">{title}</h3>
          <button onClick={onClose} className="rounded-lg p-1.5 text-slate-400 transition hover:bg-slate-100 hover:text-slate-700" aria-label="Close">
            <X size={20} />
          </button>
        </div>
        <div className="overflow-y-auto p-6">{children}</div>
      </div>
    </div>
  );
}