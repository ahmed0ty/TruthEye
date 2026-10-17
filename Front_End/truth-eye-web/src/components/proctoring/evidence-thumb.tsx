"use client";

/* eslint-disable @next/next/no-img-element */
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { ImageOff, Loader2 } from "lucide-react";
import { Modal } from "@/components/ui/modal";
import { fetchEvidence } from "@/lib/api/evidence";

export function EvidenceThumb({ name, size = 64 }: { name?: string | null; size?: number }) {
  const [open, setOpen] = useState(false);
  const valid = !!name && /^[a-f0-9]{32}\.jpg$/.test(name);

  const img = useQuery({
    queryKey: ["evidence", name],
    enabled: valid,
    staleTime: Infinity,
    gcTime: 10 * 60_000,
    retry: false,
    queryFn: async () => URL.createObjectURL(await fetchEvidence(name!)),
  });

  if (!valid) return <span className="text-slate-300">—</span>;
  if (img.isLoading) return <Loader2 size={18} className="animate-spin text-slate-300" />;
  if (img.isError || !img.data) return <ImageOff size={18} className="text-slate-300" />;

  return (
    <>
      <button onClick={() => setOpen(true)} aria-label="View evidence">
        <img
          src={img.data}
          alt="Evidence"
          style={{ width: size, height: size * 0.75 }}
          className="rounded-lg object-cover ring-1 ring-slate-200 transition hover:ring-teal-400"
        />
      </button>
      <Modal open={open} title="Evidence snapshot" onClose={() => setOpen(false)}>
        <img src={img.data} alt="Evidence" className="w-full rounded-xl" />
      </Modal>
    </>
  );
}