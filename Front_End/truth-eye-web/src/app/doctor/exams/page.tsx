"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { EyeOff, FileQuestion, Loader2, Rocket } from "lucide-react";
import { Confirm } from "@/components/ui/confirm";
import { DataTable } from "@/components/ui/data-table";
import { SelectField, SubmitButton, TextAreaField, TextField } from "@/components/ui/form-fields";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { AddButton, RowActions } from "@/components/ui/row-actions";
import { doctorApi } from "@/lib/api/doctor";
import { fmtDateTime, fromInputValue, toInputValue } from "@/lib/format";
import { useDelete } from "@/hooks/use-crud";
import { cn } from "@/lib/utils";
import { EXAM_TYPES, type DoctorExam } from "@/types/doctor";
import { getExamState } from "@/types/exam";

const schema = z
  .object({
    title: z.string().trim().min(2, "Required").max(150),
    courseId: z.coerce.number().int().min(1, "Select a course"),
    type: z.string().optional(),
    duration: z.coerce.number().int().min(1, "At least 1 minute").max(600),
    maxAttempts: z.coerce.number().int().min(1).max(10),
    passingScore: z.coerce.number().min(0).max(100),
    totalMarks: z.coerce.number().int().min(1, "At least 1"),
    startTime: z.string().min(1, "Required"),
    endTime: z.string().min(1, "Required"),
    instructions: z.string().max(2000).optional(),
    randomizeQuestions: z.boolean().optional(),
    randomizeOptions: z.boolean().optional(),
  })
  .refine((d) => !d.startTime || !d.endTime || new Date(d.endTime) > new Date(d.startTime), {
    path: ["endTime"],
    message: "Must be after the start time",
  });

const STATE_STYLE = {
  open: "bg-emerald-50 text-emerald-700",
  upcoming: "bg-amber-50 text-amber-700",
  closed: "bg-slate-100 text-slate-500",
} as const;

function ExamForm({ editing, onDone }: { editing: DoctorExam | null; onDone: () => void }) {
  const qc = useQueryClient();
  const courses = useQuery({ queryKey: ["doctor", "courses"], queryFn: doctorApi.courses });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({ resolver: zodResolver(schema) });

  useEffect(() => {
    reset(
      editing
        ? {
            title: editing.title,
            courseId: editing.courseId,
            type: editing.type ?? "",
            duration: editing.duration,
            maxAttempts: editing.maxAttempts ?? 1,
            passingScore: editing.passingScore ?? 50,
            totalMarks: editing.totalMarks ?? 100,
            startTime: toInputValue(editing.startTime),
            endTime: toInputValue(editing.endTime),
            instructions: editing.instructions ?? "",
            randomizeQuestions: !!editing.randomizeQuestions,
            randomizeOptions: !!editing.randomizeOptions,
          }
        : { duration: 60, maxAttempts: 1, passingScore: 50, totalMarks: 100, type: "Quiz", randomizeQuestions: false, randomizeOptions: false }
    );
  }, [editing, reset]);

  const save = useMutation({
    mutationFn: (v: z.infer<typeof schema>) => {
      const payload = {
        ...v,
        type: v.type || undefined,
        instructions: v.instructions || undefined,
        randomizeQuestions: !!v.randomizeQuestions,
        randomizeOptions: !!v.randomizeOptions,
        startTime: fromInputValue(v.startTime),
        endTime: fromInputValue(v.endTime),
      };
      return editing ? doctorApi.updateExam(editing.id, payload) : doctorApi.createExam(payload);
    },
    onSuccess: () => {
      toast.success(editing ? "Exam updated" : "Exam created. Now add its questions.");
      qc.invalidateQueries({ queryKey: ["doctor", "exams"] });
      onDone();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const err = (k: string) => (errors as Record<string, { message?: string }>)[k]?.message;
  const check = "h-4 w-4 rounded border-slate-300 text-teal-600 focus:ring-teal-500";

  return (
    <form onSubmit={handleSubmit((v) => save.mutate(v as z.infer<typeof schema>))} className="space-y-4" noValidate>
      <TextField label="Exam title" error={err("title")} {...register("title")} />

      <div className="grid gap-4 sm:grid-cols-2">
        <SelectField
          label="Course"
          placeholder={courses.isLoading ? "Loading..." : "Select a course"}
          options={(courses.data ?? []).map((c) => ({ value: c.id, label: c.name }))}
          error={err("courseId")}
          {...register("courseId")}
        />
        <SelectField
          label="Type"
          options={EXAM_TYPES.map((t) => ({ value: t, label: t }))}
          error={err("type")}
          {...register("type")}
        />
      </div>

      {courses.isError && (
        <p className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-600">
          Couldn’t load your courses: {(courses.error as Error).message}
        </p>
      )}
      {courses.data?.length === 0 && (
        <p className="rounded-xl bg-amber-50 px-4 py-3 text-sm text-amber-700">
          No courses are assigned to you yet. Ask your faculty admin to assign you from the Courses page.
        </p>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        <TextField label="Start time" type="datetime-local" error={err("startTime")} {...register("startTime")} />
        <TextField label="End time" type="datetime-local" error={err("endTime")} {...register("endTime")} />
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <TextField label="Duration (min)" type="number" min={1} error={err("duration")} {...register("duration")} />
        <TextField label="Attempts" type="number" min={1} error={err("maxAttempts")} {...register("maxAttempts")} />
        <TextField label="Total marks" type="number" min={1} error={err("totalMarks")} {...register("totalMarks")} />
        <TextField label="Pass at (%)" type="number" min={0} max={100} error={err("passingScore")} {...register("passingScore")} />
      </div>

      <TextAreaField label="Instructions (shown to students)" error={err("instructions")} {...register("instructions")} />

      <div className="flex flex-wrap gap-6">
        <label className="flex items-center gap-2 text-sm font-medium text-slate-700">
          <input type="checkbox" className={check} {...register("randomizeQuestions")} /> Randomize questions
        </label>
        <label className="flex items-center gap-2 text-sm font-medium text-slate-700">
          <input type="checkbox" className={check} {...register("randomizeOptions")} /> Randomize options
        </label>
      </div>

      <div className="flex justify-end pt-2">
        <SubmitButton loading={save.isPending}>{editing ? "Save changes" : "Create exam"}</SubmitButton>
      </div>
    </form>
  );
}

export default function DoctorExamsPage() {
  const qc = useQueryClient();
  const [modal, setModal] = useState<{ open: boolean; editing: DoctorExam | null }>({ open: false, editing: null });
  const { data, isLoading, error, refetch } = useQuery({ queryKey: ["doctor", "exams"], queryFn: doctorApi.exams });
  const del = useDelete<DoctorExam>(["doctor", "exams"], doctorApi.deleteExam, "Exam deleted");

  const publish = useMutation({
    mutationFn: ({ id, status }: { id: number; status: "Active" | "Draft" }) => doctorApi.setStatus(id, status),
    onSuccess: (_, v) => {
      toast.success(v.status === "Active" ? "Exam published. Students can see it now." : "Exam moved back to draft");
      qc.invalidateQueries({ queryKey: ["doctor", "exams"] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const rows = [...(data ?? [])].sort((a, b) => +new Date(b.startTime) - +new Date(a.startTime));

  return (
    <div className="space-y-6">
      <PageHeader
        title="My Exams"
        subtitle="Create exams for your courses and manage their questions."
        action={<AddButton onClick={() => setModal({ open: true, editing: null })}>New exam</AddButton>}
      />

      <DataTable
        rows={rows}
        loading={isLoading}
        error={error ? (error as Error).message : undefined}
        onRetry={() => refetch()}
        searchText={(r) => `${r.title} ${r.courseName ?? ""} ${r.type ?? ""}`}
        placeholder="Search exams..."
        columns={[
          {
            header: "Exam",
            sortValue: (r) => r.title,
            cell: (r) => (
              <div>
                <p className="font-semibold text-[#0A2540]">{r.title}</p>
                <p className="text-xs text-slate-400">{r.courseName ?? "—"}{r.type ? ` · ${r.type}` : ""}</p>
              </div>
            ),
          },
          {
            header: "Schedule",
            sortValue: (r) => +new Date(r.startTime),
            cell: (r) => (
              <div className="text-xs text-slate-500">
                <p>{fmtDateTime(r.startTime)}</p>
                <p>→ {fmtDateTime(r.endTime)}</p>
              </div>
            ),
          },
          { header: "Duration", sortValue: (r) => r.duration, cell: (r) => `${r.duration} min` },
          {
            header: "Questions",
            sortValue: (r) => r.questionsCount,
            cell: (r) => (
              <Link
                href={`/doctor/exams/${r.id}`}
                className={cn(
                  "inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold transition",
                  r.questionsCount === 0 ? "bg-amber-50 text-amber-700 hover:bg-amber-100" : "bg-teal-50 text-teal-700 hover:bg-teal-100"
                )}
              >
                <FileQuestion size={13} />
                {r.questionsCount === 0 ? "Add questions" : r.questionsCount}
              </Link>
            ),
          },
          {
            header: "State",
            sortValue: (r) => getExamState(r),
            cell: (r) => {
              const s = getExamState(r);
              return <span className={cn("rounded-full px-2.5 py-1 text-xs font-semibold capitalize", STATE_STYLE[s])}>{s}</span>;
            },
          },
          {
            header: "Publishing",
            cell: (r) => {
              const live = r.status === "Active";
              const busy = publish.isPending && publish.variables?.id === r.id;
              return (
                <button
                  onClick={() => publish.mutate({ id: r.id, status: live ? "Draft" : "Active" })}
                  disabled={busy}
                  className={cn(
                    "inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-semibold transition disabled:opacity-60",
                    live
                      ? "bg-emerald-50 text-emerald-700 hover:bg-emerald-100"
                      : "bg-slate-100 text-slate-600 hover:bg-teal-50 hover:text-teal-700"
                  )}
                >
                  {busy ? <Loader2 size={13} className="animate-spin" /> : live ? <EyeOff size={13} /> : <Rocket size={13} />}
                  {live ? "Published · Unpublish" : "Publish"}
                </button>
              );
            },
          },
        ]}
        actions={(r) => <RowActions onEdit={() => setModal({ open: true, editing: r })} onDelete={() => del.setTarget(r)} />}
      />

      <Modal
        open={modal.open}
        title={modal.editing ? "Edit exam" : "New exam"}
        onClose={() => setModal({ open: false, editing: null })}
        wide
      >
        <ExamForm editing={modal.editing} onDone={() => setModal({ open: false, editing: null })} />
      </Modal>

      <Confirm
        open={!!del.target}
        title="Delete exam"
        text={`Delete "${del.target?.title}"? Its questions will be removed as well.`}
        loading={del.mutation.isPending}
        onCancel={() => del.setTarget(null)}
        onConfirm={() => del.target && del.mutation.mutate(del.target.id)}
      />
    </div>
  );
}