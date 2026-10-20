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
import { DEGREES, labelOf } from "@/lib/enums";
import { useDelete } from "@/hooks/use-crud";
import type { Doctor } from "@/types/admin";

const base = z.object({
  firstName: z.string().trim().min(2, "Required").max(50),
  lastName: z.string().trim().min(2, "Required").max(50),
  phoneNumber: z.string().trim().min(8, "Required"),
  departmentId: z.coerce.number().int().min(1, "Select a department"),
  specialization: z.string().trim().min(2, "Required").max(100),
  academicDegree: z.coerce.number().int().min(1, "Select a degree"),
  yearsOfExperience: z.coerce.number().int().min(0).max(50),
});

const createSchema = base.extend({
  email: z.string().trim().email("Invalid email"),
  nationalId: z.string().regex(/^\d{14}$/, "Must be 14 digits"),
  password: z.string().min(6, "At least 6 characters"),
});

function DoctorForm({ editing, onDone }: { editing: Doctor | null; onDone: () => void }) {
  const qc = useQueryClient();
  const departments = useQuery({ queryKey: ["departments"], queryFn: adminApi.departments });

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
            departmentId: editing.departmentId,
            specialization: editing.specialization ?? "",
            academicDegree: editing.academicDegree,
            yearsOfExperience: editing.yearsOfExperience,
          }
        : { yearsOfExperience: 0 }
    );
  }, [editing, reset]);

  const save = useMutation({
    mutationFn: (v: Record<string, unknown>) =>
      editing ? adminApi.updateDoctor(editing.id, { ...v, isActive: editing.isActive }) : adminApi.createDoctor(v),
    onSuccess: () => {
      toast.success(editing ? "Doctor updated" : "Doctor created");
      qc.invalidateQueries({ queryKey: ["doctors"] });
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

      <SelectField
        label="Department"
        placeholder="Select"
        options={(departments.data ?? []).map((d) => ({ value: d.id, label: d.name }))}
        error={err("departmentId")}
        {...register("departmentId")}
      />

      <TextField label="Specialization" error={err("specialization")} {...register("specialization")} />

      <div className="grid gap-4 sm:grid-cols-2">
        <SelectField label="Academic degree" placeholder="Select" options={DEGREES} error={err("academicDegree")} {...register("academicDegree")} />
        <TextField label="Years of experience" type="number" min={0} max={50} error={err("yearsOfExperience")} {...register("yearsOfExperience")} />
      </div>

      <div className="flex justify-end pt-2">
        <SubmitButton loading={save.isPending}>{editing ? "Save changes" : "Create doctor"}</SubmitButton>
      </div>
    </form>
  );
}

export default function DoctorsPage() {
  const [modal, setModal] = useState<{ open: boolean; editing: Doctor | null }>({ open: false, editing: null });
  const { data, isLoading, error } = useQuery({ queryKey: ["doctors"], queryFn: adminApi.doctorList });
  const del = useDelete<Doctor>(["doctors"], adminApi.deleteDoctor, "Doctor deactivated");

  return (
    <div className="space-y-6">
      <PageHeader
        title="Doctors"
        subtitle="Manage the teaching staff in your faculty."
        action={<AddButton onClick={() => setModal({ open: true, editing: null })}>Add doctor</AddButton>}
      />

      <DataTable
        rows={data ?? []}
        loading={isLoading}
        error={error ? (error as Error).message : undefined}
        searchText={(r) => `${r.firstName} ${r.lastName} ${r.email} ${r.specialization ?? ""} ${r.departmentName}`}
        placeholder="Search doctors..."
        columns={[
          { header: "Doctor", cell: (r) => <PersonCell first={r.firstName} last={r.lastName} email={r.email} /> },
          { header: "Department", cell: (r) => r.departmentName || "—" },
          { header: "Specialization", cell: (r) => r.specialization || "—" },
          { header: "Degree", cell: (r) => labelOf(DEGREES, r.academicDegree) },
          { header: "Status", cell: (r) => <StatusBadge active={r.isActive} /> },
        ]}
        actions={(r) => (
          <RowActions onEdit={() => setModal({ open: true, editing: r })} onDelete={() => del.setTarget(r)} />
        )}
      />

      <Modal
        open={modal.open}
        title={modal.editing ? "Edit doctor" : "New doctor"}
        onClose={() => setModal({ open: false, editing: null })}
      >
        <DoctorForm editing={modal.editing} onDone={() => setModal({ open: false, editing: null })} />
      </Modal>

      <Confirm
        open={!!del.target}
        title="Deactivate doctor"
        text={`Deactivate Dr. ${del.target?.firstName} ${del.target?.lastName}? They won't be able to log in.`}
        loading={del.mutation.isPending}
        onCancel={() => del.setTarget(null)}
        onConfirm={() => del.target && del.mutation.mutate(del.target.id)}
      />
    </div>
  );
}