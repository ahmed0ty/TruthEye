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
import { SelectField, SubmitButton, TextField } from "@/components/ui/form-fields";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { AddButton, RowActions } from "@/components/ui/row-actions";
import { adminApi } from "@/lib/api/admin";
import { useAuth } from "@/lib/auth/store";
import type { StaffMember } from "@/types/admin";

const ROLE_NAME: Record<string, string> = {
  FacultySecretary: "Faculty Secretary",
  TechnicalStaff: "Technical Team",
};

const base = z.object({
  firstName: z.string().trim().min(2, "Required").max(50),
  lastName: z.string().trim().min(2, "Required").max(50),
  phoneNumber: z.string().trim().min(8, "Required"),
});

const createSchema = base.extend({
  role: z.string().min(1, "Select a role"),
  email: z.string().trim().email("Invalid email"),
  nationalId: z.string().regex(/^\d{14}$/, "Must be 14 digits"),
  password: z.string().min(6, "At least 6 characters"),
});

function StaffForm({
  editing,
  roleOptions,
  onDone,
}: {
  editing: StaffMember | null;
  roleOptions: { value: string; label: string }[];
  onDone: () => void;
}) {
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
        ? { firstName: editing.firstName, lastName: editing.lastName, phoneNumber: editing.phoneNumber ?? "" }
        : roleOptions.length === 1
          ? { role: roleOptions[0].value }
          : {}
    );
  }, [editing, reset, roleOptions]);

  const save = useMutation({
    mutationFn: (v: Record<string, unknown>) =>
      editing ? adminApi.updateStaff(editing.id, { ...v, isActive: editing.isActive }) : adminApi.createStaff(v),
    onSuccess: () => {
      toast.success(editing ? "Staff member updated" : "Staff member appointed");
      qc.invalidateQueries({ queryKey: ["staff"] });
      onDone();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const err = (k: string) => (errors as Record<string, { message?: string }>)[k]?.message;

  return (
    <form onSubmit={handleSubmit((v) => save.mutate(v as Record<string, unknown>))} className="space-y-4" noValidate>
      {!editing && (
        <SelectField label="Role" placeholder="Select" options={roleOptions} error={err("role")} {...register("role")} />
      )}

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

      <div className="flex justify-end pt-2">
        <SubmitButton loading={save.isPending}>{editing ? "Save changes" : "Appoint"}</SubmitButton>
      </div>
    </form>
  );
}

export default function StaffPage() {
const user = useAuth((s) => s.user);
  const role = user?.role;
  const canAppoint = role === "Admin" || role === "FacultySecretary";

  // منع التصعيد: الأمين يعيّن تيم فني بس، والعميد يعيّن الاتنين
  const roleOptions =
    role === "Admin"
      ? [
          { value: "FacultySecretary", label: "Faculty Secretary" },
          { value: "TechnicalStaff", label: "Technical Team" },
        ]
      : [{ value: "TechnicalStaff", label: "Technical Team" }];

  const qc = useQueryClient();
  const [modal, setModal] = useState<{ open: boolean; editing: StaffMember | null }>({ open: false, editing: null });
  const [removeTarget, setRemoveTarget] = useState<StaffMember | null>(null);
  const { data, isLoading, error, refetch } = useQuery({ queryKey: ["staff"], queryFn: adminApi.staffList });

  const toggle = useMutation({
    mutationFn: (m: StaffMember) => (m.isActive ? adminApi.disableStaff(m.id) : adminApi.enableStaff(m.id)),
    onSuccess: (_d, m) => {
      toast.success(m.isActive ? "Staff member disabled" : "Staff member enabled");
      qc.invalidateQueries({ queryKey: ["staff"] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const remove = useMutation({
    mutationFn: (id: number) => adminApi.deleteStaff(id),
    onSuccess: () => {
      toast.success("Staff member removed");
      qc.invalidateQueries({ queryKey: ["staff"] });
      setRemoveTarget(null);
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <div className="space-y-6">
      <PageHeader
        title="Faculty Staff"
        subtitle="Secretaries and technical team members of your faculty."
        action={canAppoint ? <AddButton onClick={() => setModal({ open: true, editing: null })}>Appoint</AddButton> : undefined}
      />

      <DataTable
        rows={data ?? []}
        loading={isLoading}
        error={error ? (error as Error).message : undefined}
        onRetry={() => refetch()}
        searchText={(r) => `${r.firstName} ${r.lastName} ${r.email} ${r.nationalId}`}
        placeholder="Search staff..."
        columns={[
          { header: "Member", sortValue: (r) => `${r.firstName} ${r.lastName}`, cell: (r) => <PersonCell first={r.firstName} last={r.lastName} email={r.email} /> },
          { header: "Role", sortValue: (r) => r.role, cell: (r) => ROLE_NAME[r.role] ?? r.role },
          { header: "Status", sortValue: (r) => Number(r.isActive), cell: (r) => <StatusBadge active={r.isActive} /> },
        ]}
        actions={(r) =>
          r.canManage ? (
            <div className="flex items-center justify-end gap-1">
              <button
                type="button"
                onClick={() => toggle.mutate(r)}
                title={r.isActive ? "Disable" : "Enable"}
                className="rounded-lg p-2 text-slate-500 transition hover:bg-slate-100"
              >
                {r.isActive ? <Ban size={16} /> : <CheckCircle2 size={16} />}
              </button>
              <RowActions onEdit={() => setModal({ open: true, editing: r })} onDelete={() => setRemoveTarget(r)} />
            </div>
          ) : null
        }
      />

      <Modal
        open={modal.open}
        title={modal.editing ? "Edit staff member" : "Appoint staff member"}
        onClose={() => setModal({ open: false, editing: null })}
      >
        <StaffForm editing={modal.editing} roleOptions={roleOptions} onDone={() => setModal({ open: false, editing: null })} />
      </Modal>

      <Confirm
        open={!!removeTarget}
        title="Remove staff member"
        text={`Remove ${removeTarget?.firstName} ${removeTarget?.lastName}? They will be deleted from the system. You can undo this from the activity log.`}
        loading={remove.isPending}
        onCancel={() => setRemoveTarget(null)}
        onConfirm={() => removeTarget && remove.mutate(removeTarget.id)}
      />
    </div>
  );
}