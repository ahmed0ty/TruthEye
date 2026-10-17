"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { Confirm } from "@/components/ui/confirm";
import { DataTable } from "@/components/ui/data-table";
import { SubmitButton, TextField } from "@/components/ui/form-fields";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { AddButton, RowActions } from "@/components/ui/row-actions";
import { adminApi } from "@/lib/api/admin";
import { fmtDateTime } from "@/lib/format";
import { useDelete } from "@/hooks/use-crud";
import type { University } from "@/types/admin";

const schema = z.object({
  name: z.string().trim().min(2, "Required").max(100),
  location: z.string().trim().min(2, "Required").max(200),
  city: z.string().trim().min(2, "Required").max(50),
  country: z.string().trim().min(2, "Required").max(50),
});
type Values = z.infer<typeof schema>;

function UniversityForm({ editing, onDone }: { editing: University | null; onDone: () => void }) {
  const qc = useQueryClient();
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<Values>({ resolver: zodResolver(schema) });

  useEffect(() => {
    reset(
      editing
        ? { name: editing.name, location: editing.location, city: editing.city, country: editing.country }
        : { name: "", location: "", city: "", country: "" }
    );
  }, [editing, reset]);

  const save = useMutation({
    mutationFn: (v: Values) => (editing ? adminApi.updateUniversity(editing.id, v) : adminApi.createUniversity(v)),
    onSuccess: () => {
      toast.success(editing ? "University updated" : "University created");
      qc.invalidateQueries({ queryKey: ["universities"] });
      qc.invalidateQueries({ queryKey: ["admin-stat"] });
      onDone();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <form onSubmit={handleSubmit((v) => save.mutate(v))} className="space-y-4" noValidate>
      <TextField label="University name" error={errors.name?.message} {...register("name")} />
      <TextField label="Address / location" error={errors.location?.message} {...register("location")} />
      <div className="grid gap-4 sm:grid-cols-2">
        <TextField label="City" error={errors.city?.message} {...register("city")} />
        <TextField label="Country" error={errors.country?.message} {...register("country")} />
      </div>
      <div className="flex justify-end pt-2">
        <SubmitButton loading={save.isPending}>{editing ? "Save changes" : "Create university"}</SubmitButton>
      </div>
    </form>
  );
}

export default function UniversitiesPage() {
  const [modal, setModal] = useState<{ open: boolean; editing: University | null }>({ open: false, editing: null });
  const { data, isLoading, error } = useQuery({ queryKey: ["universities"], queryFn: adminApi.universityList });
  const del = useDelete<University>(["universities"], adminApi.deleteUniversity, "University deleted");

  return (
    <div className="space-y-6">
      <PageHeader
        title="Universities"
        subtitle="The top level of your academic structure."
        action={<AddButton onClick={() => setModal({ open: true, editing: null })}>Add university</AddButton>}
      />

      <DataTable
        rows={data ?? []}
        loading={isLoading}
        error={error ? (error as Error).message : undefined}
        searchText={(r) => `${r.name} ${r.city} ${r.country}`}
        placeholder="Search universities..."
        columns={[
          { header: "University", cell: (r) => <span className="font-bold text-slate-800">{r.name}</span> },
          { header: "Location", cell: (r) => `${r.city}, ${r.country}` },
          {
            header: "Faculties",
            cell: (r) => <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-bold text-slate-600">{r.facultiesCount}</span>,
          },
          { header: "Created", cell: (r) => <span className="text-slate-500">{fmtDateTime(r.createdAt)}</span> },
        ]}
        actions={(r) => <RowActions onEdit={() => setModal({ open: true, editing: r })} onDelete={() => del.setTarget(r)} />}
      />

      <Modal
        open={modal.open}
        title={modal.editing ? "Edit university" : "New university"}
        onClose={() => setModal({ open: false, editing: null })}
      >
        <UniversityForm editing={modal.editing} onDone={() => setModal({ open: false, editing: null })} />
      </Modal>

      <Confirm
        open={!!del.target}
        title="Delete university"
        text={`Delete "${del.target?.name}"? A university that still has faculties may be rejected by the server.`}
        loading={del.mutation.isPending}
        onCancel={() => del.setTarget(null)}
        onConfirm={() => del.target && del.mutation.mutate(del.target.id)}
      />
    </div>
  );
}