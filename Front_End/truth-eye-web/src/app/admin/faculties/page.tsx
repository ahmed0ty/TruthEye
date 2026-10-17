"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { Confirm } from "@/components/ui/confirm";
import { DataTable } from "@/components/ui/data-table";
import { SelectField, SubmitButton, TextField } from "@/components/ui/form-fields";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { AddButton, RowActions } from "@/components/ui/row-actions";
import { adminApi } from "@/lib/api/admin";
import { useDelete } from "@/hooks/use-crud";
import type { Faculty } from "@/types/admin";

const schema = z.object({
  name: z.string().trim().min(2, "Required").max(100),
  universityId: z.coerce.number().int().min(1, "Select a university"),
});

function FacultyForm({ editing, onDone }: { editing: Faculty | null; onDone: () => void }) {
  const qc = useQueryClient();
  const universities = useQuery({ queryKey: ["universities"], queryFn: adminApi.universityList });
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({ resolver: zodResolver(schema) });

  useEffect(() => {
    reset(editing ? { name: editing.name, universityId: editing.universityId } : { name: "" });
  }, [editing, reset]);

  const save = useMutation({
    mutationFn: (v: Record<string, unknown>) =>
      editing ? adminApi.updateFaculty(editing.id, v) : adminApi.createFaculty(v),
    onSuccess: () => {
      toast.success(editing ? "Faculty updated" : "Faculty created");
      qc.invalidateQueries({ queryKey: ["faculties"] });
      qc.invalidateQueries({ queryKey: ["universities"] });
      qc.invalidateQueries({ queryKey: ["admin-stat"] });
      onDone();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <form onSubmit={handleSubmit((v) => save.mutate(v as Record<string, unknown>))} className="space-y-4" noValidate>
      <TextField label="Faculty name" error={errors.name?.message as string | undefined} {...register("name")} />
      <SelectField
        label="University"
        placeholder="Select a university"
        options={(universities.data ?? []).map((u) => ({ value: u.id, label: u.name }))}
        error={errors.universityId?.message as string | undefined}
        {...register("universityId")}
      />
      <div className="flex justify-end pt-2">
        <SubmitButton loading={save.isPending}>{editing ? "Save changes" : "Create faculty"}</SubmitButton>
      </div>
    </form>
  );
}

export default function FacultiesPage() {
  const [modal, setModal] = useState<{ open: boolean; editing: Faculty | null }>({ open: false, editing: null });
  const { data, isLoading, error } = useQuery({ queryKey: ["faculties"], queryFn: adminApi.facultyList });
  const del = useDelete<Faculty>(["faculties"], adminApi.deleteFaculty, "Faculty deleted");

  return (
    <div className="space-y-6">
      <PageHeader
        title="Faculties"
        subtitle="Each faculty belongs to one university."
        action={<AddButton onClick={() => setModal({ open: true, editing: null })}>Add faculty</AddButton>}
      />

      <DataTable
        rows={data ?? []}
        loading={isLoading}
        error={error ? (error as Error).message : undefined}
        searchText={(r) => `${r.name} ${r.universityName}`}
        placeholder="Search faculties..."
        columns={[
          { header: "Faculty", cell: (r) => <span className="font-bold text-slate-800">{r.name}</span> },
          { header: "University", cell: (r) => r.universityName || "—" },
          {
            header: "Departments",
            cell: (r) => <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-bold text-slate-600">{r.departmentsCount}</span>,
          },
        ]}
        actions={(r) => <RowActions onEdit={() => setModal({ open: true, editing: r })} onDelete={() => del.setTarget(r)} />}
      />

      <Modal
        open={modal.open}
        title={modal.editing ? "Edit faculty" : "New faculty"}
        onClose={() => setModal({ open: false, editing: null })}
      >
        <FacultyForm editing={modal.editing} onDone={() => setModal({ open: false, editing: null })} />
      </Modal>

      <Confirm
        open={!!del.target}
        title="Delete faculty"
        text={`Delete "${del.target?.name}"? A faculty that still has departments may be rejected by the server.`}
        loading={del.mutation.isPending}
        onCancel={() => del.setTarget(null)}
        onConfirm={() => del.target && del.mutation.mutate(del.target.id)}
      />
    </div>
  );
}