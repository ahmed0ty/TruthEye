"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm, type FieldValues, type Resolver } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { Confirm } from "@/components/ui/confirm";
import { DataTable, PersonCell, StatusBadge } from "@/components/ui/data-table";
import { SubmitButton, TextField } from "@/components/ui/form-fields";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { AddButton, RowActions } from "@/components/ui/row-actions";
import { adminApi } from "@/lib/api/admin";
import { useDelete } from "@/hooks/use-crud";
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
  const del = useDelete<Proctor>(["proctors"], adminApi.deleteProctor, "Proctor deactivated");

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
          <RowActions onEdit={() => setModal({ open: true, editing: r })} onDelete={() => del.setTarget(r)} />
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
        title="Deactivate proctor"
        text={`Deactivate ${del.target?.firstName} ${del.target?.lastName}?`}
        loading={del.mutation.isPending}
        onCancel={() => del.setTarget(null)}
        onConfirm={() => del.target && del.mutation.mutate(del.target.id)}
      />
    </div>
  );
}