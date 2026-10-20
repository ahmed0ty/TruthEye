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
import type { SupportMember } from "@/types/admin";

const base = z.object({
  firstName: z.string().trim().min(2, "Required").max(50),
  lastName: z.string().trim().min(2, "Required").max(50),
  phoneNumber: z.string().trim().min(8, "Required"),
  department: z.string().trim().min(2, "Required").max(100),
  specialization: z.string().trim().min(2, "Required").max(100),
  shiftTiming: z.string().trim().min(2, "Required").max(50),
});

const createSchema = base.extend({
  email: z.string().trim().email("Invalid email"),
  nationalId: z.string().regex(/^\d{14}$/, "Must be 14 digits"),
  password: z.string().min(6, "At least 6 characters"),
});

function SupportForm({ editing, onDone }: { editing: SupportMember | null; onDone: () => void }) {
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
            specialization: editing.specialization ?? "",
            shiftTiming: editing.shiftTiming ?? "",
          }
        : {}
    );
  }, [editing, reset]);

  const save = useMutation({
    mutationFn: (v: Record<string, unknown>) =>
      editing ? adminApi.updateSupport(editing.id, { ...v, isActive: editing.isActive }) : adminApi.createSupport(v),
    onSuccess: () => {
      toast.success(editing ? "Support member updated" : "Support member created");
      qc.invalidateQueries({ queryKey: ["supports"] });
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

      <div className="grid gap-4 sm:grid-cols-2">
        <TextField label="Department" placeholder="e.g. Technical Support" error={err("department")} {...register("department")} />
        <TextField label="Specialization" placeholder="e.g. Exam issues" error={err("specialization")} {...register("specialization")} />
      </div>
      <TextField label="Shift timing" placeholder="e.g. 9:00 AM - 5:00 PM" error={err("shiftTiming")} {...register("shiftTiming")} />

      <div className="flex justify-end pt-2">
        <SubmitButton loading={save.isPending}>{editing ? "Save changes" : "Create support member"}</SubmitButton>
      </div>
    </form>
  );
}

export default function SupportsPage() {
  const [modal, setModal] = useState<{ open: boolean; editing: SupportMember | null }>({ open: false, editing: null });
  const { data, isLoading, error } = useQuery({ queryKey: ["supports"], queryFn: adminApi.supportList });
  const del = useDelete<SupportMember>(["supports"], adminApi.deleteSupport, "Support member deactivated");

  return (
    <div className="space-y-6">
      <PageHeader
        title="Support Team"
        subtitle="Staff who handle tickets from students and doctors."
        action={<AddButton onClick={() => setModal({ open: true, editing: null })}>Add support member</AddButton>}
      />

      <DataTable
        rows={data ?? []}
        loading={isLoading}
        error={error ? (error as Error).message : undefined}
        searchText={(r) => `${r.firstName} ${r.lastName} ${r.email} ${r.department ?? ""} ${r.specialization ?? ""}`}
        placeholder="Search support team..."
        columns={[
          { header: "Member", cell: (r) => <PersonCell first={r.firstName} last={r.lastName} email={r.email} /> },
          { header: "Department", cell: (r) => r.department || "—" },
          { header: "Shift", cell: (r) => r.shiftTiming || "—" },
          {
            header: "Open tickets",
            cell: (r) => <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-bold text-slate-600">{r.openTickets ?? 0}</span>,
          },
          { header: "Status", cell: (r) => <StatusBadge active={r.isActive} /> },
        ]}
        actions={(r) => <RowActions onEdit={() => setModal({ open: true, editing: r })} onDelete={() => del.setTarget(r)} />}
      />

      <Modal
        open={modal.open}
        title={modal.editing ? "Edit support member" : "New support member"}
        onClose={() => setModal({ open: false, editing: null })}
      >
        <SupportForm editing={modal.editing} onDone={() => setModal({ open: false, editing: null })} />
      </Modal>

      <Confirm
        open={!!del.target}
        title="Deactivate support member"
        text={`Deactivate ${del.target?.firstName} ${del.target?.lastName}?`}
        loading={del.mutation.isPending}
        onCancel={() => del.setTarget(null)}
        onConfirm={() => del.target && del.mutation.mutate(del.target.id)}
      />
    </div>
  );
}