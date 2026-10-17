"use client";

import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { Plus, Settings2, Stethoscope, Users, X } from "lucide-react";
import { Confirm } from "@/components/ui/confirm";
import { DataTable } from "@/components/ui/data-table";
import { SelectField, SubmitButton, TextAreaField, TextField } from "@/components/ui/form-fields";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { AddButton, RowActions } from "@/components/ui/row-actions";
import { adminApi } from "@/lib/api/admin";
import { useDelete } from "@/hooks/use-crud";
import { cn } from "@/lib/utils";
import type { Course } from "@/types/admin";

const schema = z.object({
  name: z.string().trim().min(2, "Required").max(100),
  description: z.string().trim().min(2, "Required").max(500),
  departmentId: z.coerce.number().int().min(1, "Select a department"),
});
type Values = z.infer<typeof schema>;

function CourseForm({ editing, onDone }: { editing: Course | null; onDone: () => void }) {
  const qc = useQueryClient();
  const departments = useQuery({ queryKey: ["departments"], queryFn: adminApi.departments });
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({ resolver: zodResolver(schema) });

  useEffect(() => {
    reset(editing ? { name: editing.name, description: editing.description, departmentId: editing.departmentId } : {});
  }, [editing, reset]);

  const save = useMutation({
    mutationFn: (v: Values) => (editing ? adminApi.updateCourse(editing.id, v) : adminApi.createCourse(v)),
    onSuccess: () => {
      toast.success(editing ? "Course updated" : "Course created");
      qc.invalidateQueries({ queryKey: ["courses"] });
      qc.invalidateQueries({ queryKey: ["admin-stat"] });
      onDone();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <form onSubmit={handleSubmit((v) => save.mutate(v as Values))} className="space-y-4" noValidate>
      <TextField label="Course name" error={errors.name?.message} {...register("name")} />
      <SelectField
        label="Department"
        placeholder="Select"
        options={(departments.data ?? []).map((d) => ({ value: d.id, label: d.name }))}
        error={errors.departmentId?.message as string | undefined}
        {...register("departmentId")}
      />
      <TextAreaField label="Description" error={errors.description?.message} {...register("description")} />
      <div className="flex justify-end pt-2">
        <SubmitButton loading={save.isPending}>{editing ? "Save changes" : "Create course"}</SubmitButton>
      </div>
    </form>
  );
}

/* ---------- تعيين الدكاترة والطلاب ---------- */

type Person = { id: number; firstName: string; lastName: string; email: string };

function Assignment({
  title,
  icon: Icon,
  assigned,
  candidates,
  loading,
  onAdd,
  onRemove,
  busy,
}: {
  title: string;
  icon: typeof Users;
  assigned: Person[];
  candidates: Person[];
  loading: boolean;
  onAdd: (id: number) => void;
  onRemove: (id: number) => void;
  busy: boolean;
}) {
  const [pick, setPick] = useState("");
  const assignedIds = useMemo(() => new Set(assigned.map((p) => p.id)), [assigned]);
  const available = candidates.filter((c) => !assignedIds.has(c.id));

  return (
    <div className="rounded-2xl border border-slate-200 p-4">
      <div className="mb-3 flex items-center gap-2">
        <Icon size={18} className="text-teal-700" />
        <h4 className="font-extrabold text-slate-900">{title}</h4>
        <span className="rounded-full bg-teal-50 px-2 py-0.5 text-xs font-bold text-teal-700">{assigned.length}</span>
      </div>

      <div className="mb-3 flex gap-2">
        <select
          value={pick}
          onChange={(e) => setPick(e.target.value)}
          className="min-w-0 flex-1 rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-teal-600 focus:ring-4 focus:ring-teal-600/15"
        >
          <option value="">Select to add...</option>
          {available.map((c) => (
            <option key={c.id} value={c.id}>{c.firstName} {c.lastName} ({c.email})</option>
          ))}
        </select>
        <button
          disabled={!pick || busy}
          onClick={() => {
            onAdd(Number(pick));
            setPick("");
          }}
          className="flex items-center gap-1 rounded-xl bg-teal-700 px-3 text-sm font-bold text-white transition hover:bg-teal-800 disabled:opacity-40"
        >
          <Plus size={16} /> Add
        </button>
      </div>

      {loading ? (
        <div className="h-16 animate-pulse rounded-xl bg-slate-100" />
      ) : assigned.length === 0 ? (
        <p className="py-4 text-center text-sm text-slate-400">No one assigned yet.</p>
      ) : (
        <ul className="max-h-56 space-y-1.5 overflow-y-auto">
          {assigned.map((p) => (
            <li key={p.id} className="flex items-center justify-between rounded-xl bg-slate-50 px-3 py-2">
              <div className="min-w-0">
                <p className="truncate text-sm font-bold text-slate-800">{p.firstName} {p.lastName}</p>
                <p className="truncate text-xs text-slate-400" dir="ltr">{p.email}</p>
              </div>
              <button onClick={() => onRemove(p.id)} disabled={busy} className="rounded-lg p-1.5 text-slate-400 transition hover:bg-red-50 hover:text-red-600" aria-label="Remove">
                <X size={16} />
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function ManageCourse({ course }: { course: Course }) {
  const qc = useQueryClient();
  const doctors = useQuery({ queryKey: ["course-doctors", course.id], queryFn: () => adminApi.courseDoctors(course.id) });
  const students = useQuery({ queryKey: ["course-students", course.id], queryFn: () => adminApi.courseStudents(course.id) });
  const allDoctors = useQuery({ queryKey: ["doctors"], queryFn: adminApi.doctorList });
  const allStudents = useQuery({ queryKey: ["students"], queryFn: adminApi.studentList });

  const run = (fn: (id: number) => Promise<unknown>, key: string, msg: string) =>
    useMutation({
      mutationFn: fn,
      onSuccess: () => {
        toast.success(msg);
        qc.invalidateQueries({ queryKey: [key, course.id] });
      },
      onError: (e: Error) => toast.error(e.message),
    });

  const addDoc = run((id) => adminApi.assignDoctor(course.id, id), "course-doctors", "Doctor assigned");
  const rmDoc = run((id) => adminApi.removeDoctor(course.id, id), "course-doctors", "Doctor removed");
  const addStu = run((id) => adminApi.assignStudent(course.id, id), "course-students", "Student assigned");
  const rmStu = run((id) => adminApi.removeStudent(course.id, id), "course-students", "Student removed");

  return (
    <div className={cn("grid gap-4 lg:grid-cols-2")}>
      <Assignment
        title="Doctors"
        icon={Stethoscope}
        assigned={doctors.data ?? []}
        candidates={allDoctors.data ?? []}
        loading={doctors.isLoading}
        onAdd={addDoc.mutate}
        onRemove={rmDoc.mutate}
        busy={addDoc.isPending || rmDoc.isPending}
      />
      <Assignment
        title="Students"
        icon={Users}
        assigned={students.data ?? []}
        candidates={allStudents.data ?? []}
        loading={students.isLoading}
        onAdd={addStu.mutate}
        onRemove={rmStu.mutate}
        busy={addStu.isPending || rmStu.isPending}
      />
    </div>
  );
}

export default function CoursesPage() {
  const [modal, setModal] = useState<{ open: boolean; editing: Course | null }>({ open: false, editing: null });
  const [manage, setManage] = useState<Course | null>(null);
  const { data, isLoading, error } = useQuery({ queryKey: ["courses"], queryFn: adminApi.courses });
  const del = useDelete<Course>(["courses"], adminApi.deleteCourse, "Course deleted");

  return (
    <div className="space-y-6">
      <PageHeader
        title="Courses"
        subtitle="Create courses and assign doctors and students to them."
        action={<AddButton onClick={() => setModal({ open: true, editing: null })}>Add course</AddButton>}
      />

      <DataTable
        rows={data ?? []}
        loading={isLoading}
        error={error ? (error as Error).message : undefined}
        searchText={(r) => `${r.name} ${r.departmentName ?? ""}`}
        placeholder="Search courses..."
        columns={[
          { header: "Course", cell: (r) => <span className="font-bold text-slate-800">{r.name}</span> },
          { header: "Department", cell: (r) => r.departmentName || "—" },
          { header: "Description", cell: (r) => <span className="line-clamp-1 max-w-xs text-slate-500">{r.description}</span> },
        ]}
        actions={(r) => (
          <RowActions
            extra={
              <button
                onClick={() => setManage(r)}
                className="mr-1 flex items-center gap-1.5 rounded-lg bg-teal-50 px-3 py-1.5 text-xs font-bold text-teal-700 transition hover:bg-teal-100"
              >
                <Settings2 size={14} /> Manage
              </button>
            }
            onEdit={() => setModal({ open: true, editing: r })}
            onDelete={() => del.setTarget(r)}
          />
        )}
      />

      <Modal open={modal.open} title={modal.editing ? "Edit course" : "New course"} onClose={() => setModal({ open: false, editing: null })}>
        <CourseForm editing={modal.editing} onDone={() => setModal({ open: false, editing: null })} />
      </Modal>

      <Modal open={!!manage} title={`Manage: ${manage?.name ?? ""}`} onClose={() => setManage(null)} wide>
        {manage && <ManageCourse course={manage} />}
      </Modal>

      <Confirm
        open={!!del.target}
        title="Delete course"
        text={`Delete "${del.target?.name}"?`}
        loading={del.mutation.isPending}
        onCancel={() => del.setTarget(null)}
        onConfirm={() => del.target && del.mutation.mutate(del.target.id)}
      />
    </div>
  );
}