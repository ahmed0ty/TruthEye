"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { LifeBuoy, Plus } from "lucide-react";
import { EmptyState } from "@/components/ui/empty-state";
import { SelectField, SubmitButton, TextAreaField, TextField } from "@/components/ui/form-fields";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { ErrorState } from "@/components/ui/states";
import { ticketsApi } from "@/lib/api/tickets";
import { fmtDateTime } from "@/lib/format";
import { CATEGORIES, PRIORITIES, PRIORITY_STYLE, STATUS_STYLE, statusLabel } from "@/lib/ticket-meta";
import { cn } from "@/lib/utils";
import type { Ticket } from "@/types/ticket";

const schema = z.object({
  category: z.string().min(1, "Select a category"),
  priority: z.string().min(1, "Select a priority"),
  subject: z.string().trim().min(3, "At least 3 characters").max(150),
  description: z.string().trim().min(10, "Describe the problem (10+ characters)").max(2000),
});
type Values = z.infer<typeof schema>;

function NewTicketForm({ onDone }: { onDone: () => void }) {
  const qc = useQueryClient();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { category: "Technical", priority: "Medium" } });

  const save = useMutation({
    mutationFn: ticketsApi.create,
    onSuccess: () => {
      toast.success("Ticket submitted. The support team will get back to you.");
      qc.invalidateQueries({ queryKey: ["tickets", "mine"] });
      onDone();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <form onSubmit={handleSubmit((v) => save.mutate(v))} className="space-y-4" noValidate>
      <div className="grid gap-4 sm:grid-cols-2">
        <SelectField label="Category" options={CATEGORIES.map((c) => ({ value: c, label: c }))} error={errors.category?.message} {...register("category")} />
        <SelectField label="Priority" options={PRIORITIES.map((p) => ({ value: p, label: p }))} error={errors.priority?.message} {...register("priority")} />
      </div>
      <TextField label="Subject" error={errors.subject?.message} {...register("subject")} />
      <TextAreaField label="Description" rows={5} error={errors.description?.message} {...register("description")} />
      <div className="flex justify-end pt-2">
        <SubmitButton loading={save.isPending}>Submit ticket</SubmitButton>
      </div>
    </form>
  );
}

export function MyTickets() {
  const [creating, setCreating] = useState(false);
  const [open, setOpen] = useState<Ticket | null>(null);
  const { data, isLoading, isError, error, refetch } = useQuery({ queryKey: ["tickets", "mine"], queryFn: ticketsApi.mine });

  const rows = [...(data ?? [])].sort((a, b) => b.id - a.id);

  return (
    <div className="space-y-6">
      <PageHeader
        title="Support"
        subtitle="Having a problem? Open a ticket and our team will help."
        action={
          <button
            onClick={() => setCreating(true)}
            className="inline-flex items-center gap-2 rounded-xl bg-teal-600 px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-teal-900/10 transition hover:bg-teal-700 active:scale-[0.98]"
          >
            <Plus size={18} /> New ticket
          </button>
        }
      />

      {isLoading ? (
        <div className="space-y-3">{[0, 1, 2].map((i) => <div key={i} className="h-24 animate-pulse rounded-2xl bg-slate-100" />)}</div>
      ) : isError ? (
        <ErrorState title="Couldn’t load your tickets" message={(error as Error).message} onRetry={() => refetch()} />
      ) : rows.length === 0 ? (
        <EmptyState icon={LifeBuoy} title="No tickets yet" text="When you open a ticket, you can follow its progress here." />
      ) : (
        <ul className="space-y-3">
          {rows.map((t, i) => (
            <li key={t.id} className="animate-fade-up" style={{ animationDelay: `${Math.min(i, 8) * 0.05}s` }}>
              <button
                onClick={() => setOpen(t)}
                className="w-full rounded-2xl border border-slate-200 bg-white p-5 text-start transition hover:-translate-y-0.5 hover:border-teal-300 hover:shadow-lg hover:shadow-slate-200/70"
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <p className="font-bold text-[#0A2540]">{t.subject}</p>
                  <div className="flex gap-2">
                    <span className={cn("rounded-full px-2.5 py-1 text-xs font-semibold", PRIORITY_STYLE[t.priority] ?? "bg-slate-100 text-slate-600")}>{t.priority}</span>
                    <span className={cn("rounded-full px-2.5 py-1 text-xs font-semibold", STATUS_STYLE[t.status] ?? "bg-slate-100 text-slate-600")}>{statusLabel(t.status)}</span>
                  </div>
                </div>
                <p className="mt-1 text-xs text-slate-400">
                  {t.ticketNumber} · {t.category}
                  {t.createdAt ? ` · ${fmtDateTime(t.createdAt)}` : ""}
                </p>
                <p className="mt-2 line-clamp-2 text-sm text-slate-600">{t.description}</p>
              </button>
            </li>
          ))}
        </ul>
      )}

      <Modal open={creating} title="New support ticket" onClose={() => setCreating(false)}>
        <NewTicketForm onDone={() => setCreating(false)} />
      </Modal>

      <Modal open={!!open} title={open?.subject ?? ""} onClose={() => setOpen(null)}>
        {open && (
          <div className="space-y-4 text-sm">
            <div className="flex flex-wrap gap-2">
              <span className={cn("rounded-full px-2.5 py-1 text-xs font-semibold", STATUS_STYLE[open.status] ?? "bg-slate-100")}>{statusLabel(open.status)}</span>
              <span className={cn("rounded-full px-2.5 py-1 text-xs font-semibold", PRIORITY_STYLE[open.priority] ?? "bg-slate-100")}>{open.priority}</span>
              <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-600">{open.category}</span>
            </div>
            <div className="rounded-xl bg-slate-50 p-4 text-slate-700">
              <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-400">Your message</p>
              <p className="whitespace-pre-wrap">{open.description}</p>
            </div>
            <p className="text-slate-500">
              Handled by: <b className="text-[#0A2540]">{open.assignedToName || "Not assigned yet"}</b>
            </p>
            {open.resolution && (
              <div className="rounded-xl bg-teal-50 p-4 text-teal-900">
                <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-teal-600">Resolution</p>
                <p className="whitespace-pre-wrap">{open.resolution}</p>
              </div>
            )}
          </div>
        )}
      </Modal>
    </div>
  );
}