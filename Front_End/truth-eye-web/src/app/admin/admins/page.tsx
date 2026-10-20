"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm, type FieldValues, type Resolver } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { Confirm } from "@/components/ui/confirm";
import { DataTable, PersonCell, StatusBadge } from "@/components/ui/data-table";
import { SelectField, SubmitButton, TextField } from "@/components/ui/form-fields";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { AddButton, RowActions } from "@/components/ui/row-actions";
import { adminApi } from "@/lib/api/admin";
import { useDelete } from "@/hooks/use-crud";
import type { FacultyAdmin } from "@/types/admin";

const base = z.object({
  firstName: z.string().trim().min(2, "Required").max(50),
  lastName: z.string().trim().min(2, "Required").max(50),
  phoneNumber: z.string().trim().min(8, "Required"),
  universityId: z.coerce.number().int().min(1, "Select a university"),
  facultyId: z.coerce.number().int().min(1, "Select a faculty"),
});

const createSchema = base.extend({
  email: z.string().trim().email("Invalid email"),
  nationalId: z.string().regex(/^\d{14}$/, "Must be 14 digits"),
  password: z.string().min(6, "At least 6 characters"),
});

function AdminForm({ editing, onDone }: { editing: FacultyAdmin | null; onDone: () => void }) {
  const qc = useQueryClient();
  const universities = useQuery({ queryKey: ["universities"], queryFn: adminApi.universityList });
  const faculties = useQuery({ queryKey: ["faculties"], queryFn: adminApi.facultyList });

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<FieldValues>({ resolver: zodResolver(editing ? base : createSchema) as unknown as Resolver<FieldValues> });

  useEffect(() => {
    reset(
      editing
        ? {
            firstName: editing.firstName,
            lastName: editing.lastName,
            phoneNumber: editing.phoneNumber ?? "",
            universityId: editing.universityId,
            facultyId: editing.facultyId,
          }
        : {}
    );
  }, [editing, reset]);

  const universityId = Number(watch("universityId") || 0);
  const facultyOptions = (faculties.data ?? [])
    .filter((f) => f.universityId === universityId)
    .map((f) => ({ value: f.id, label: f.name }));

  const save = useMutation({
    mutationFn: (v: Record<string, unknown>) =>
      editing ? adminApi.updateAdmin(editing.id, { ...v, isActive: editing.isActive }) : adminApi.createAdmin(v),
    onSuccess: () => {
      toast.success(editing ? "Admin updated" : "Admin created");
      qc.invalidateQueries({ queryKey: ["admins"] });
      qc.invalidateQueries({ queryKey: ["admin-stat"] });
      qc.invalidateQueries({ queryKey: ["admin-recent"] });
      onDone();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const err = (k: string) => (errors as Record<string, { message?: string }>)[k]?.message;

  return (
    <form onSubmit={handleSubmit((v) => save.mutate(v as Record<string, unknown>))} className="space-y-4" noValidate>
      <div className="grid gap-4 sm:grid-cols-2">
        <TextField label="First name" error={err("firstName")} {...register("firstName")} />
        <TextField label="Last name" error={err("lastName")} {...register("lastName")} />
      </div>

      {!editing && (
        <>
          <TextField label="Email" type="email" dir="ltr" error={err("email")} {...register("email")} />
          <div className="grid gap-4 sm:grid-cols-2">
            <TextField label="National ID" dir="ltr" inputMode="numeric" maxLength={14} error={err("nationalId")} {...register("nationalId")} />
            <TextField label="Password" type="password" dir="ltr" error={err("password")} {...register("password")} />
          </div>
        </>
      )}

      <TextField label="Phone number" dir="ltr" placeholder="+201000000000" error={err("phoneNumber")} {...register("phoneNumber")} />

      <div className="grid gap-4 sm:grid-cols-2">
        <SelectField
          label="University"
          placeholder="Select"
          options={(universities.data ?? []).map((u) => ({ value: u.id, label: u.name }))}
          error={err("universityId")}
          {...register("universityId", { onChange: () => setValue("facultyId", undefined as never) })}
        />
        <SelectField
          label="Faculty"
          placeholder={universityId ? "Select" : "Pick a university first"}
          disabled={!universityId}
          options={facultyOptions}
          error={err("facultyId")}
          {...register("facultyId")}
        />
      </div>

      <div className="flex justify-end pt-2">
        <SubmitButton loading={save.isPending}>{editing ? "Save changes" : "Create admin"}</SubmitButton>
      </div>
    </form>
  );
}

export default function AdminsPage() {
  const [modal, setModal] = useState<{ open: boolean; editing: FacultyAdmin | null }>({ open: false, editing: null });
  const { data, isLoading, error } = useQuery({ queryKey: ["admins"], queryFn: adminApi.adminList });
  const del = useDelete<FacultyAdmin>(["admins"], adminApi.deleteAdmin, "Admin deactivated");

  return (
    <div className="space-y-6">
      <PageHeader
        title="Faculty Admins"
        subtitle="Each admin manages the students, doctors and courses of one faculty."
        action={<AddButton onClick={() => setModal({ open: true, editing: null })}>Add admin</AddButton>}
      />

      <DataTable
        rows={data ?? []}
        loading={isLoading}
        error={error ? (error as Error).message : undefined}
        searchText={(r) => `${r.firstName} ${r.lastName} ${r.email} ${r.facultyName ?? ""} ${r.universityName ?? ""}`}
        placeholder="Search admins..."
        columns={[
          { header: "Admin", cell: (r) => <PersonCell first={r.firstName} last={r.lastName} email={r.email} /> },
          { header: "Faculty", cell: (r) => r.facultyName || "—" },
          { header: "University", cell: (r) => r.universityName || "—" },
          { header: "Status", cell: (r) => <StatusBadge active={r.isActive} /> },
        ]}
        actions={(r) => <RowActions onEdit={() => setModal({ open: true, editing: r })} onDelete={() => del.setTarget(r)} />}
      />

      <Modal
        open={modal.open}
        title={modal.editing ? "Edit admin" : "New faculty admin"}
        onClose={() => setModal({ open: false, editing: null })}
      >
        <AdminForm editing={modal.editing} onDone={() => setModal({ open: false, editing: null })} />
      </Modal>

      <Confirm
        open={!!del.target}
        title="Deactivate admin"
        text={`Deactivate ${del.target?.firstName} ${del.target?.lastName}? They won't be able to log in.`}
        loading={del.mutation.isPending}
        onCancel={() => del.setTarget(null)}
        onConfirm={() => del.target && del.mutation.mutate(del.target.id)}
      />
    </div>
  );
}