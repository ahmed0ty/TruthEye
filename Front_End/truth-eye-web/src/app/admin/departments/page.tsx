"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { Confirm } from "@/components/ui/confirm";
import { DataTable } from "@/components/ui/data-table";
import { SelectField, SubmitButton, TextAreaField, TextField } from "@/components/ui/form-fields";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { AddButton, RowActions } from "@/components/ui/row-actions";
import { adminApi } from "@/lib/api/admin";
import { useDelete } from "@/hooks/use-crud";
import type { Department } from "@/types/admin";

const schema = z.object({
  name: z.string().trim().min(2, "Name is required").max(100),
  description: z.string().max(500).optional(),
  facultyId: z.coerce.number().int().min(1, "Select a faculty"),
});
type Values = z.infer<typeof schema>;

function DepartmentForm({ editing, onDone }: { editing: Department | null; onDone: () => void }) {
  const qc = useQueryClient();
  const faculties = useQuery({
    queryKey: ["faculties"],
    queryFn: () => adminApi.faculties() as Promise<{ id: number; name: string }[]>,
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({ resolver: zodResolver(schema) });

  useEffect(() => {
    reset(
      editing
        ? { name: editing.name, description: editing.description, facultyId: editing.facultyId }
        : { name: "", description: "", facultyId: undefined }
    );
  }, [editing, reset]);

  const save = useMutation({
    mutationFn: (v: Values) =>
      editing ? adminApi.updateDepartment(editing.id, v) : adminApi.createDepartment(v),
    onSuccess: () => {
      toast.success(editing ? "Department updated" : "Department created");
      qc.invalidateQueries({ queryKey: ["departments"] });
      onDone();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <form onSubmit={handleSubmit((v) => save.mutate(v as Values))} className="space-y-4" noValidate>
      <TextField label="Department name" error={errors.name?.message} {...register("name")} />
      <SelectField
        label="Faculty"
        placeholder="Select a faculty"
        options={(faculties.data ?? []).map((f) => ({ value: f.id, label: f.name }))}
        error={errors.facultyId?.message as string | undefined}
        {...register("facultyId")}
      />
      <TextAreaField label="Description" error={errors.description?.message} {...register("description")} />
      <div className="flex justify-end pt-2">
        <SubmitButton loading={save.isPending}>{editing ? "Save changes" : "Create department"}</SubmitButton>
      </div>
    </form>
  );
}

export default function DepartmentsPage() {
  const [modal, setModal] = useState<{ open: boolean; editing: Department | null }>({ open: false, editing: null });
  const { data, isLoading, error } = useQuery({ queryKey: ["departments"], queryFn: adminApi.departments });
  const del = useDelete<Department>(["departments"], adminApi.deleteDepartment, "Department deleted");

  return (
    <div className="space-y-6">
      <PageHeader
        title="Departments"
        subtitle="Organize your faculty into departments."
        action={<AddButton onClick={() => setModal({ open: true, editing: null })}>Add department</AddButton>}
      />

      <DataTable
        rows={data ?? []}
        loading={isLoading}
        error={error ? (error as Error).message : undefined}
        searchText={(r) => `${r.name} ${r.facultyName} ${r.universityName}`}
        placeholder="Search departments..."
        columns={[
          { header: "Department", cell: (r) => <span className="font-bold text-slate-800">{r.name}</span> },
          { header: "Faculty", cell: (r) => r.facultyName || "—" },
          { header: "University", cell: (r) => r.universityName || "—" },
          {
            header: "Courses",
            cell: (r) => <span className="rounded-full bg-teal-50 px-2.5 py-1 text-xs font-bold text-teal-700">{r.coursesCount}</span>,
          },
        ]}
        actions={(r) => (
          <RowActions onEdit={() => setModal({ open: true, editing: r })} onDelete={() => del.setTarget(r)} />
        )}
      />

      <Modal
        open={modal.open}
        title={modal.editing ? "Edit department" : "New department"}
        onClose={() => setModal({ open: false, editing: null })}
      >
        <DepartmentForm editing={modal.editing} onDone={() => setModal({ open: false, editing: null })} />
      </Modal>

      <Confirm
        open={!!del.target}
        title="Delete department"
        text={`Delete "${del.target?.name}"? A department that still has courses can't be deleted.`}
        loading={del.mutation.isPending}
        onCancel={() => del.setTarget(null)}
        onConfirm={() => del.target && del.mutation.mutate(del.target.id)}
      />
    </div>
  );
}