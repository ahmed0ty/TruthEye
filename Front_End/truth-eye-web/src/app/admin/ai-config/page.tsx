"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { Info } from "lucide-react";
import { DataTable } from "@/components/ui/data-table";
import { SubmitButton, TextField } from "@/components/ui/form-fields";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { AddButton, RowActions } from "@/components/ui/row-actions";
import { adminApi } from "@/lib/api/admin";
import { fmtDateTime } from "@/lib/format";
import type { AiConfig } from "@/types/admin";

const schema = z.object({
  parameterName: z.string().trim().min(2, "Required").max(100),
  value: z.string().trim().min(1, "Required").max(500),
  threshold: z.coerce.number().min(0, "0 to 1").max(1, "0 to 1"),
});

function ConfigForm({ editing, onDone }: { editing: AiConfig | null; onDone: () => void }) {
  const qc = useQueryClient();
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({ resolver: zodResolver(schema) });

  useEffect(() => {
    reset(
      editing
        ? { parameterName: editing.parameterName ?? "", value: editing.value ?? "", threshold: editing.threshold }
        : { parameterName: "", value: "", threshold: 0.7 }
    );
  }, [editing, reset]);

  const save = useMutation({
    mutationFn: (v: z.infer<typeof schema>) => adminApi.saveAiConfig(v),
    onSuccess: () => {
      toast.success("Setting saved");
      qc.invalidateQueries({ queryKey: ["ai-config"] });
      onDone();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const err = (k: string) => (errors as Record<string, { message?: string }>)[k]?.message;

  return (
    <form onSubmit={handleSubmit((v) => save.mutate(v as z.infer<typeof schema>))} className="space-y-4" noValidate>
      <TextField
        label="Parameter name"
        placeholder="e.g. FaceDetectionSensitivity"
        disabled={!!editing}
        error={err("parameterName")}
        {...register("parameterName")}
      />
      <TextField label="Value" error={err("value")} {...register("value")} />
      <TextField label="Threshold (0 to 1)" type="number" step="0.05" min={0} max={1} error={err("threshold")} {...register("threshold")} />
      <div className="flex justify-end pt-2">
        <SubmitButton loading={save.isPending}>{editing ? "Save changes" : "Add setting"}</SubmitButton>
      </div>
    </form>
  );
}

export default function AiConfigPage() {
  const [modal, setModal] = useState<{ open: boolean; editing: AiConfig | null }>({ open: false, editing: null });
  const { data, isLoading, error } = useQuery({ queryKey: ["ai-config"], queryFn: adminApi.aiConfigs });

  return (
    <div className="space-y-6">
      <PageHeader
        title="AI Settings"
        subtitle="Tunable parameters for AI grading and proctoring."
        action={<AddButton onClick={() => setModal({ open: true, editing: null })}>Add setting</AddButton>}
      />

      <div className="flex items-start gap-3 rounded-2xl border border-sky-200 bg-sky-50 p-4 text-sm text-sky-800">
        <Info size={18} className="mt-0.5 shrink-0" />
        <p>
          These values are stored in the database. The backend does not read them yet, so changing them has no effect until the grading and proctoring code is wired to use them.
        </p>
      </div>

      <DataTable
        rows={data ?? []}
        loading={isLoading}
        error={error ? (error as Error).message : undefined}
        searchText={(r) => `${r.parameterName ?? ""} ${r.value ?? ""}`}
        placeholder="Search settings..."
        columns={[
          { header: "Parameter", cell: (r) => <span className="font-semibold text-[#0A2540]">{r.parameterName}</span> },
          { header: "Value", cell: (r) => r.value },
          { header: "Threshold", cell: (r) => r.threshold },
          { header: "Updated", cell: (r) => <span className="text-slate-500">{fmtDateTime(r.updatedAt ?? r.createdAt)}</span> },
        ]}
        actions={(r) => <RowActions onEdit={() => setModal({ open: true, editing: r })} />}
      />

      <Modal
        open={modal.open}
        title={modal.editing ? "Edit setting" : "New setting"}
        onClose={() => setModal({ open: false, editing: null })}
      >
        <ConfigForm editing={modal.editing} onDone={() => setModal({ open: false, editing: null })} />
      </Modal>
    </div>
  );
}