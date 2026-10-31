"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm, type FieldValues, type Resolver } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { Ban, CheckCircle2 } from "lucide-react";
import { Confirm } from "@/components/ui/confirm";
import { DataTable, PersonCell, StatusBadge } from "@/components/ui/data-table";
import { SubmitButton, TextField } from "@/components/ui/form-fields";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { AddButton, RowActions } from "@/components/ui/row-actions";
import { adminApi } from "@/lib/api/admin";
import { useDelete } from "@/hooks/use-crud";
import { useAuth } from "@/lib/auth/store";
import type { Proctor } from "@/types/admin";

const base = z.object({
  firstName: z.string().trim().min(2, "Required").max(50),
  lastName: z.string().trim().min(2, "Required").max(50),
  phoneNumber: z.string().trim().min(8, "Required"),
  department: z.string().trim().min(2, "Required").max(100),
});

const createSchema = base.extend({
  email: z.string().trim().email("Invalid email"),
  nationalId: z.string().regex(/^\d{14}$/, "Must be 14 digits"),
  password: z.string().min(6, "At least 6 characters"),
});

function ProctorForm({ editing, onDone }: { editing: Proctor | null; onDone: () => void }) {
  const qc = useQueryClient();
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FieldValues>({ resolver: zodResolver(editing ? base : createSchema) as unknown as Resolver<FieldValues> });

  useEffect(() => {
    reset(
      editing
        ? {
            firstName: editing.firstName,
            lastName: editing.lastName,
            phoneNumber: editing.phoneNumber ?? "",
            department: editing.department ?? "",
          }
        : {}
    );
  }, [editing, reset]);

  const save = useMutation({
    mutationFn: (v: Record<string, unknown>) =>
      editing ? adminApi.updateProctor(editing.id, { ...v, isActive: editing.isActive }) : adminApi.createProctor(v),
    onSuccess: () => {
      toast.success(editing ? "Proctor updated" : "Proctor created");
      qc.invalidateQueries({ queryKey: ["proctors"] });
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
            <TextField label="Password" type="password" dir="ltr" error={err("password")} {...register("password")} />
          </div>
        </>
      )}

      <TextField label="Phone number" dir="ltr" placeholder="+201000000000" error={err("phoneNumber")} {...register("phoneNumber")} />
      <TextField label="Department" placeholder="e.g. Exams Control" error={err("department")} {...register("department")} />

      <div className="flex justify-end pt-2">
        <SubmitButton loading={save.isPending}>{editing ? "Save changes" : "Create proctor"}</SubmitButton>
      </div>
    </form>
  );
}

export default function ProctorsPage() {
  const [modal, setModal] = useState<{ open: boolean; editing: Proctor | null }>({ open: false, editing: null });
  const { data, isLoading, error } = useQuery({ queryKey: ["proctors"], queryFn: adminApi.proctorList });
 const user = useAuth((s) => s.user);
  const canDelete = user?.role === "Admin" || user?.role === "FacultySecretary";
  const qc = useQueryClient();
  const del = useDelete<Proctor>(["proctors"], adminApi.deleteProctor, "Proctor deleted");
  const toggle = useMutation({
    mutationFn: (p: Proctor) => (p.isActive ? adminApi.disableProctor(p.id) : adminApi.enableProctor(p.id)),
    onSuccess: (_r, p) => {
      toast.success(p.isActive ? "Proctor disabled" : "Proctor enabled");
      qc.invalidateQueries({ queryKey: ["proctors"] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <div className="space-y-6">
      <PageHeader
        title="Proctors"
        subtitle="Exam supervisors who monitor students live."
        action={<AddButton onClick={() => setModal({ open: true, editing: null })}>Add proctor</AddButton>}
      />

      <DataTable
        rows={data ?? []}
        loading={isLoading}
        error={error ? (error as Error).message : undefined}
        searchText={(r) => `${r.firstName} ${r.lastName} ${r.email} ${r.department ?? ""}`}
        placeholder="Search proctors..."
        columns={[
          { header: "Proctor", cell: (r) => <PersonCell first={r.firstName} last={r.lastName} email={r.email} /> },
          { header: "Department", cell: (r) => r.department || "—" },
          {
            header: "Duty",
            cell: (r) => (
              <span className={`rounded-full px-2.5 py-1 text-xs font-bold ${r.isOnDuty ? "bg-sky-50 text-sky-600" : "bg-slate-100 text-slate-500"}`}>
                {r.isOnDuty ? "On duty" : "Off duty"}
              </span>
            ),
          },
          { header: "Status", cell: (r) => <StatusBadge active={r.isActive} /> },
        ]}
        actions={(r) => (
          <RowActions
            extra={
              <button
                type="button"
                onClick={() => toggle.mutate(r)}
                title={r.isActive ? "Disable" : "Enable"}
                className="rounded-lg p-2 text-slate-400 transition hover:bg-slate-100 hover:text-slate-700"
              >
                {r.isActive ? <Ban size={17} /> : <CheckCircle2 size={17} />}
              </button>
            }
            onEdit={() => setModal({ open: true, editing: r })}
            onDelete={canDelete ? () => del.setTarget(r) : undefined}
          />
        )}
      />

      <Modal
        open={modal.open}
        title={modal.editing ? "Edit proctor" : "New proctor"}
        onClose={() => setModal({ open: false, editing: null })}
      >
        <ProctorForm editing={modal.editing} onDone={() => setModal({ open: false, editing: null })} />
      </Modal>

      <Confirm
        open={!!del.target}
        title="Delete proctor"
        text={`Delete ${del.target?.firstName} ${del.target?.lastName}? They will be removed from your lists. You can undo this from the activity log.`}
        loading={del.mutation.isPending}
        onCancel={() => del.setTarget(null)}
        onConfirm={() => del.target && del.mutation.mutate(del.target.id)}
      />
    </div>
  );
}