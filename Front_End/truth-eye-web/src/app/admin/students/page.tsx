"use client";

import { TwoFactorResetButton } from "@/components/admin/two-factor-reset-button";
import { useEffect, useState } from "react";
import Link from "next/link";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm, type FieldValues, type Resolver } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { Ban, CheckCircle2, KeyRound, Upload } from "lucide-react";
import { Confirm } from "@/components/ui/confirm";
import { DataTable, PersonCell, StatusBadge } from "@/components/ui/data-table";
import { FilterSelect } from "@/components/ui/filter-select";
import { SelectField, SubmitButton, TextField } from "@/components/ui/form-fields";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { AddButton, RowActions } from "@/components/ui/row-actions";
import { ResetPasswordDialog, type ResetTarget } from "@/components/admin/reset-password-dialog";
import { PasswordModeFields, passwordModeSchema, withPasswordMode } from "@/components/admin/password-mode-fields";
import { adminApi } from "@/lib/api/admin";
import { YEARS, labelOf } from "@/lib/enums";
import { useDelete } from "@/hooks/use-crud";
import { useServerTable } from "@/hooks/use-server-table";
import { useAuth } from "@/lib/auth/store";
import type { Student } from "@/types/admin";

const base = z.object({
  firstName: z.string().trim().min(2, "Required").max(50),
  lastName: z.string().trim().min(2, "Required").max(50),
  phoneNumber: z.string().trim().min(8, "Required"),
  departmentId: z.coerce.number().int().min(1, "Select a department"),
  year: z.coerce.number().int().min(1, "Select a year"),
});

const createSchema = base.extend({
  email: z.string().trim().email("Invalid email"),
  nationalId: z.string().regex(/^\d{14}$/, "Must be 14 digits"),
});

type CreateValues = z.infer<typeof createSchema>;

function StudentForm({ editing, onDone }: { editing: Student | null; onDone: () => void }) {
  const qc = useQueryClient();
  const departments = useQuery({ queryKey: ["departments"], queryFn: adminApi.departments });

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<FieldValues>({ resolver: zodResolver(editing ? base : createSchema.and(passwordModeSchema)) as unknown as Resolver<FieldValues> });

  useEffect(() => {
    reset(
      editing
        ? {
            firstName: editing.firstName,
            lastName: editing.lastName,
            phoneNumber: editing.phoneNumber ?? "",
            departmentId: editing.departmentId,
            year: editing.year,
          }
        : {}
    );
  }, [editing, reset]);

  const save = useMutation({
    mutationFn: (v: Record<string, unknown>) =>
      editing ? adminApi.updateStudent(editing.id, { ...v, isActive: editing.isActive }) : adminApi.createStudent(withPasswordMode(v)),
    onSuccess: () => {
      toast.success(editing ? "Student updated" : "Student created");
      qc.invalidateQueries({ queryKey: ["students"] });
      qc.invalidateQueries({ queryKey: ["admin-stat"] });
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
          </div>
          <PasswordModeFields register={register} watch={watch} error={err} />
        </>
      )}

      <TextField label="Phone number" dir="ltr" placeholder="+201000000000" error={err("phoneNumber")} {...register("phoneNumber")} />

      <div className="grid gap-4 sm:grid-cols-2">
        <SelectField
          label="Department"
          placeholder="Select"
          options={(departments.data ?? []).map((d) => ({ value: d.id, label: d.name }))}
          error={err("departmentId")}
          {...register("departmentId")}
        />
        <SelectField label="Year" placeholder="Select" options={YEARS} error={err("year")} {...register("year")} />
      </div>

      <div className="flex justify-end pt-2">
        <SubmitButton loading={save.isPending}>{editing ? "Save changes" : "Create student"}</SubmitButton>
      </div>
    </form>
  );
}

export default function StudentsPage() {
  const [modal, setModal] = useState<{ open: boolean; editing: Student | null }>({ open: false, editing: null });
  const [resetTarget, setResetTarget] = useState<ResetTarget | null>(null);
  const user = useAuth((s) => s.user);
  const canDelete = user?.role === "Admin" || user?.role === "FacultySecretary";
  const qc = useQueryClient();

  const table = useServerTable(10);
  const departments = useQuery({ queryKey: ["departments"], queryFn: adminApi.departments });

  const { data, isLoading, isFetching, error, refetch } = useQuery({
    queryKey: ["students", "paged", table.params],
    queryFn: () => adminApi.studentsPaged(table.params),
    placeholderData: keepPreviousData,
  });

  const del = useDelete<Student>(["students"], adminApi.deleteStudent, "Student deleted");
  const toggle = useMutation({
    mutationFn: (s: Student) => (s.isActive ? adminApi.disableStudent(s.id) : adminApi.enableStudent(s.id)),
    onSuccess: (_d, s) => {
      toast.success(s.isActive ? "Student disabled" : "Student enabled");
      qc.invalidateQueries({ queryKey: ["students"] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <div className="space-y-6">
      <PageHeader
        title="Students"
        subtitle="Manage the students in your faculty."
        action={
          <div className="flex flex-wrap gap-2">
            <Link
              href="/admin/students/import"
              className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-5 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50"
            >
              <Upload size={17} /> Import
            </Link>
            <AddButton onClick={() => setModal({ open: true, editing: null })}>Add student</AddButton>
          </div>
        }
      />

      <DataTable
        rows={data?.items ?? []}
        loading={isLoading}
        error={error ? (error as Error).message : undefined}
        onRetry={() => refetch()}
        server={table.bind(data?.total ?? 0, isFetching)}
        placeholder="Search by name, email or national ID..."
        toolbar={
          <>
            <FilterSelect
              label="Department"
              value={table.filters.departmentId ?? "all"}
              onChange={(v) => table.setFilter("departmentId", v)}
              options={(departments.data ?? []).map((d) => ({ value: String(d.id), label: d.name }))}
            />
            <FilterSelect
              label="Year"
              value={table.filters.year ?? "all"}
              onChange={(v) => table.setFilter("year", v)}
              options={YEARS.map((y) => ({ value: String(y.value), label: y.label }))}
            />
            <FilterSelect
              label="Status"
              value={table.filters.status ?? "all"}
              onChange={(v) => table.setFilter("status", v)}
              options={[{ value: "active", label: "Active" }, { value: "inactive", label: "Inactive" }]}
            />
          </>
        }
        columns={[
          { header: "Student", sortKey: "name", cell: (r) => <PersonCell first={r.firstName} last={r.lastName} email={r.email} /> },
          { header: "National ID", cell: (r) => <span dir="ltr" className="text-slate-600">{r.nationalId}</span> },
          { header: "Department", sortKey: "department", cell: (r) => r.departmentName || "—" },
          { header: "Year", sortKey: "year", cell: (r) => labelOf(YEARS, r.year) },
          { header: "Status", sortKey: "status", cell: (r) => <StatusBadge active={r.isActive} /> },
        ]}
        actions={(r) => (
          <RowActions
            extra={
              <>
                <TwoFactorResetButton userId={r.id} name={`${r.firstName} ${r.lastName}`} />
                <button
                  type="button"
                  onClick={() => toggle.mutate(r)}
                  title={r.isActive ? "Disable" : "Enable"}
                  className="rounded-lg p-2 text-slate-400 transition hover:bg-slate-100 hover:text-slate-700"
                >
                  {r.isActive ? <Ban size={17} /> : <CheckCircle2 size={17} />}
                </button>
                <button
                  type="button"
                  onClick={() => setResetTarget({ id: r.id, name: `${r.firstName} ${r.lastName}` })}
                  title="Reset password"
                  className="rounded-lg p-2 text-slate-400 transition hover:bg-slate-100 hover:text-slate-700"
                >
                  <KeyRound size={17} />
                </button>
              </>
            }
            onEdit={() => setModal({ open: true, editing: r })}
            onDelete={canDelete ? () => del.setTarget(r) : undefined}
          />
        )}
      />

      <Modal
        open={modal.open}
        title={modal.editing ? "Edit student" : "New student"}
        onClose={() => setModal({ open: false, editing: null })}
      >
        <StudentForm editing={modal.editing} onDone={() => setModal({ open: false, editing: null })} />
      </Modal>

      <Confirm
        open={!!del.target}
        title="Delete student"
        text={`Delete ${del.target?.firstName} ${del.target?.lastName}? They will be removed and won't be able to log in.`}
        loading={del.mutation.isPending}
        onCancel={() => del.setTarget(null)}
        onConfirm={() => del.target && del.mutation.mutate(del.target.id)}
      />

      <ResetPasswordDialog target={resetTarget} onClose={() => setResetTarget(null)} />
    </div>
  );
}