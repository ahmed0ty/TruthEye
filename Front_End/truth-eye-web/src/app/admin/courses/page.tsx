"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { FileSpreadsheet, GraduationCap, Plus, Search, Settings2, Upload, Users, X } from "lucide-react";
import { Confirm } from "@/components/ui/confirm";
import { DataTable } from "@/components/ui/data-table";
import { SelectField, SubmitButton, TextAreaField, TextField } from "@/components/ui/form-fields";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { AddButton, RowActions } from "@/components/ui/row-actions";
import { adminApi, type BulkEnrollRequest, type BulkEnrollResult } from "@/lib/api/admin";
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

/* ---------- Doctors (assign one by one, there are few) ---------- */

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

/* ---------- Students (bulk) ---------- */

// Only what this panel needs. The optional fields are used for the estimate in "By department & year".
type Candidate = {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  nationalId?: string | null;
  departmentId?: number | null;
  year?: number | string | null;
};

const LIMIT = 200;

function parseIdentifiers(text: string) {
  const parts = text
    .split(/[\s,;،]+/)
    .map((s) => s.trim().replace(/^'/, ""))
    .filter(Boolean);
  return Array.from(new Set(parts));
}

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const NID_RE = /^\d{14}$/;

// Reads every cell of the first sheet as text and keeps those that look like an email or a 14-digit national ID
async function readSheetIdentifiers(file: File): Promise<string[]> {
  const XLSX = await import("xlsx");
  const wb = XLSX.read(await file.arrayBuffer(), { type: "array" });
  const sheet = wb.Sheets[wb.SheetNames[0]];
  const rows = XLSX.utils.sheet_to_json<unknown[]>(sheet, { header: 1, raw: false, defval: "" });
  const found = new Set<string>();
  for (const row of rows)
    for (const cell of row) {
      const v = String(cell ?? "").trim().replace(/^'/, "");
      if (EMAIL_RE.test(v) || NID_RE.test(v)) found.add(v);
    }
  return [...found];
}

/** A searchable checkbox list. "Select all" selects every search result, not only the visible rows. Shift+click selects a range. */
function PickList({
  items,
  selected,
  setSelected,
  placeholder,
  emptyText,
}: {
  items: Candidate[];
  selected: Set<number>;
  setSelected: (s: Set<number>) => void;
  placeholder: string;
  emptyText: string;
}) {
  const [q, setQ] = useState("");
  const last = useRef<number | null>(null);

  const filtered = useMemo(() => {
    const s = q.trim().toLowerCase();
    if (!s) return items;
    return items.filter((p) => `${p.firstName} ${p.lastName} ${p.email} ${p.nationalId ?? ""}`.toLowerCase().includes(s));
  }, [items, q]);

  const visible = filtered.slice(0, LIMIT);
  const allSelected = filtered.length > 0 && filtered.every((p) => selected.has(p.id));

  const toggleAll = () => {
    const next = new Set(selected);
    if (allSelected) filtered.forEach((p) => next.delete(p.id));
    else filtered.forEach((p) => next.add(p.id));
    setSelected(next);
  };

  const click = (idx: number, shift: boolean) => {
    const id = visible[idx].id;
    const turnOn = !selected.has(id);
    const next = new Set(selected);
    const from = shift && last.current !== null ? Math.min(last.current, idx) : idx;
    const to = shift && last.current !== null ? Math.max(last.current, idx) : idx;
    for (let i = from; i <= to; i++) {
      if (turnOn) next.add(visible[i].id);
      else next.delete(visible[i].id);
    }
    last.current = idx;
    setSelected(next);
  };

  return (
    <div>
      <div className="relative mb-2">
        <Search size={16} className="pointer-events-none absolute inset-y-0 start-3 my-auto text-slate-400" />
        <input
          value={q}
          onChange={(e) => {
            setQ(e.target.value);
            last.current = null;
          }}
          placeholder={placeholder}
          className="w-full rounded-xl border border-slate-300 bg-white py-2 ps-9 pe-3 text-sm outline-none focus:border-teal-600 focus:ring-4 focus:ring-teal-600/15"
        />
      </div>

      {items.length === 0 ? (
        <p className="py-4 text-center text-sm text-slate-400">{emptyText}</p>
      ) : (
        <div className="overflow-hidden rounded-xl border border-slate-200">
          <label className="flex cursor-pointer items-center gap-3 border-b border-slate-100 bg-slate-50 px-3 py-2 text-sm font-semibold text-slate-700">
            <input type="checkbox" checked={allSelected} onChange={toggleAll} className="h-4 w-4 accent-teal-700" />
            Select all {filtered.length} {filtered.length === 1 ? "result" : "results"}
            <span className="ms-auto text-xs font-medium text-slate-400">{selected.size} selected</span>
          </label>

          {filtered.length === 0 ? (
            <p className="py-4 text-center text-sm text-slate-400">No match.</p>
          ) : (
            <ul className="max-h-56 overflow-y-auto">
              {visible.map((p, idx) => (
                <li key={p.id}>
                  <label className="flex cursor-pointer items-center gap-3 px-3 py-2 transition hover:bg-slate-50">
                    <input
                      type="checkbox"
                      checked={selected.has(p.id)}
                      onChange={() => {}}
                      onClick={(e) => click(idx, e.shiftKey)}
                      className="h-4 w-4 shrink-0 accent-teal-700"
                    />
                    <span className="min-w-0">
                      <span className="block truncate text-sm font-bold text-slate-800">{p.firstName} {p.lastName}</span>
                      <span className="block truncate text-xs text-slate-400" dir="ltr">{p.email}</span>
                    </span>
                  </label>
                </li>
              ))}
            </ul>
          )}

          {filtered.length > LIMIT && (
            <p className="border-t border-slate-100 bg-slate-50 px-3 py-2 text-xs text-slate-500">
              Showing the first {LIMIT}. “Select all” still selects all {filtered.length}. Search to narrow the list down.
            </p>
          )}
        </div>
      )}
    </div>
  );
}

type Mode = "list" | "paste" | "excel" | "group";

const MODES: { id: Mode; label: string }[] = [
  { id: "list", label: "Pick from list" },
  { id: "paste", label: "Paste emails / IDs" },
  { id: "excel", label: "Excel file" },
  { id: "group", label: "By department & year" },
];

function StudentsPanel({ course }: { course: Course }) {
  const qc = useQueryClient();
  const enrolledQ = useQuery({ queryKey: ["course-students", course.id], queryFn: () => adminApi.courseStudents(course.id) });
  const allQ = useQuery({ queryKey: ["students"], queryFn: adminApi.studentList });
  const departments = useQuery({ queryKey: ["departments"], queryFn: adminApi.departments });

  const enrolled = useMemo(() => (enrolledQ.data ?? []) as unknown as Candidate[], [enrolledQ.data]);
  const all = useMemo(() => (allQ.data ?? []) as unknown as Candidate[], [allQ.data]);
  const enrolledIds = useMemo(() => new Set(enrolled.map((s) => s.id)), [enrolled]);
  const available = useMemo(() => all.filter((s) => !enrolledIds.has(s.id)), [all, enrolledIds]);

  const [mode, setMode] = useState<Mode>("list");
  const [pickSel, setPickSel] = useState<Set<number>>(new Set());
  const [removeSel, setRemoveSel] = useState<Set<number>>(new Set());
  const [pasteText, setPasteText] = useState("");
  const [sheetIds, setSheetIds] = useState<string[]>([]);
  const [sheetName, setSheetName] = useState("");
  const [dept, setDept] = useState("");
  const [year, setYear] = useState("");
  const [confirmGroup, setConfirmGroup] = useState(false);
  const [confirmRemove, setConfirmRemove] = useState(false);
  const [result, setResult] = useState<BulkEnrollResult | null>(null);

  const refresh = () => {
    qc.invalidateQueries({ queryKey: ["course-students", course.id] });
    qc.invalidateQueries({ queryKey: ["admin-stat"] });
  };

  const enroll = useMutation({
    mutationFn: (d: BulkEnrollRequest) => adminApi.enrollStudents(course.id, d),
    onSuccess: (r) => {
      setResult(r);
      setPickSel(new Set());
      setPasteText("");
      setSheetIds([]);
      setSheetName("");
      setConfirmGroup(false);
      refresh();
      toast.success(r.added > 0 ? `${r.added} students added` : "No new students were added");
    },
    onError: (e: Error) => {
      setConfirmGroup(false);
      toast.error(e.message);
    },
  });

  const remove = useMutation({
    mutationFn: (ids: number[]) => adminApi.removeStudents(course.id, ids),
    onSuccess: (r) => {
      toast.success(`${r.removed} students removed`);
      setRemoveSel(new Set());
      setConfirmRemove(false);
      refresh();
    },
    onError: (e: Error) => {
      setConfirmRemove(false);
      toast.error(e.message);
    },
  });

  // paste mode
  const identifiers = useMemo(() => parseIdentifiers(pasteText), [pasteText]);

  // excel mode
  const pickSheet = async (f: File | undefined) => {
    if (!f) return;
    try {
      const list = await readSheetIdentifiers(f);
      setSheetIds(list);
      setSheetName(f.name);
      if (list.length === 0) toast.error("No emails or 14-digit national IDs were found in the sheet.");
    } catch {
      toast.error("Couldn't read the file. Use .xlsx, .xls or .csv.");
    }
  };

  // group mode
  const deptId = dept ? Number(dept) : null;
  const yr = year ? Number(year) : null;
  const deptName = (departments.data ?? []).find((d) => d.id === deptId)?.name ?? "";
  const estimate = useMemo(() => {
    const hasDept = all.some((s) => s.departmentId != null);
    const hasYear = all.some((s) => s.year != null && s.year !== "" && Number.isFinite(Number(s.year)));
    if (deptId === null && yr === null) return null;
    if ((deptId !== null && !hasDept) || (yr !== null && !hasYear)) return null; // the list doesn't have these fields, so no estimate
    const matches = all.filter((s) => (deptId === null || s.departmentId === deptId) && (yr === null || Number(s.year) === yr));
    return { total: matches.length, toAdd: matches.filter((s) => !enrolledIds.has(s.id)).length };
  }, [all, enrolledIds, deptId, yr]);

  const groupLabel = `${deptName ? `department "${deptName}"` : "all departments"}${yr ? `, year ${yr}` : ", all years"}`;
  const groupDisabled = (deptId === null && yr === null) || enroll.isPending || (estimate !== null && estimate.toAdd === 0);

  const loading = enrolledQ.isLoading || allQ.isLoading;

  return (
    <div className="rounded-2xl border border-slate-200 p-4">
      <div className="mb-3 flex items-center gap-2">
        <Users size={18} className="text-teal-700" />
        <h4 className="font-extrabold text-slate-900">Students</h4>
        <span className="rounded-full bg-teal-50 px-2 py-0.5 text-xs font-bold text-teal-700">{enrolled.length}</span>
      </div>

      {/* how to add */}
      <div className="mb-3 flex flex-wrap gap-1 rounded-xl bg-slate-100 p-1">
        {MODES.map((m) => (
          <button
            key={m.id}
            onClick={() => setMode(m.id)}
            className={cn(
              "flex-1 rounded-lg px-3 py-1.5 text-xs font-bold transition",
              mode === m.id ? "bg-white text-teal-700 shadow-sm" : "text-slate-500 hover:text-slate-800"
            )}
          >
            {m.label}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="h-24 animate-pulse rounded-xl bg-slate-100" />
      ) : (
        <>
          {mode === "list" && (
            <div className="space-y-3">
              <PickList
                items={available}
                selected={pickSel}
                setSelected={setPickSel}
                placeholder="Search by name, email or national ID..."
                emptyText="Every student is already in this course."
              />
              <div className="flex items-center justify-between gap-3">
                <p className="text-xs text-slate-400">Tip: hold Shift to select a range.</p>
                <button
                  disabled={pickSel.size === 0 || enroll.isPending}
                  onClick={() => enroll.mutate({ studentIds: Array.from(pickSel) })}
                  className="flex items-center gap-1 rounded-xl bg-teal-700 px-4 py-2 text-sm font-bold text-white transition hover:bg-teal-800 disabled:opacity-40"
                >
                  <Plus size={16} /> Add {pickSel.size || ""} {pickSel.size === 1 ? "student" : "students"}
                </button>
              </div>
            </div>
          )}

          {mode === "paste" && (
            <div className="space-y-3">
              <textarea
                value={pasteText}
                onChange={(e) => setPasteText(e.target.value)}
                rows={6}
                dir="ltr"
                placeholder={"One per line (or separated by commas):\nstudent1@mail.com\n29901011234567"}
                className="w-full rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-teal-600 focus:ring-4 focus:ring-teal-600/15"
              />
              <div className="flex items-center justify-between gap-3">
                <p className="text-xs text-slate-400">{identifiers.length} {identifiers.length === 1 ? "item" : "items"} detected (emails or national IDs).</p>
                <button
                  disabled={identifiers.length === 0 || enroll.isPending}
                  onClick={() => enroll.mutate({ identifiers })}
                  className="flex items-center gap-1 rounded-xl bg-teal-700 px-4 py-2 text-sm font-bold text-white transition hover:bg-teal-800 disabled:opacity-40"
                >
                  <Plus size={16} /> Add {identifiers.length || ""}
                </button>
              </div>
            </div>
          )}

          {mode === "excel" && (
            <div className="space-y-3">
              <p className="text-xs text-slate-500">
                Upload a sheet with each student’s email or 14-digit national ID (any column). Only students who already exist in your faculty are added.
                Keep national IDs formatted as Text in Excel so they don’t turn into 2.99E+13.
              </p>
              <label className="inline-flex cursor-pointer items-center gap-2 rounded-xl bg-slate-100 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-200">
                <Upload size={16} /> Choose Excel / CSV file
                <input type="file" accept=".xlsx,.xls,.csv" className="hidden" onChange={(e) => pickSheet(e.target.files?.[0])} />
              </label>
              {sheetName && (
                <p className="flex items-center gap-2 text-sm text-slate-600">
                  <FileSpreadsheet size={16} className="text-teal-600" /> {sheetName} · <b>{sheetIds.length}</b> found
                </p>
              )}
              <div className="flex justify-end">
                <button
                  disabled={sheetIds.length === 0 || enroll.isPending}
                  onClick={() => enroll.mutate({ identifiers: sheetIds })}
                  className="flex items-center gap-1 rounded-xl bg-teal-700 px-4 py-2 text-sm font-bold text-white transition hover:bg-teal-800 disabled:opacity-40"
                >
                  <Plus size={16} /> Add {sheetIds.length || ""}
                </button>
              </div>
            </div>
          )}

          {mode === "group" && (
            <div className="space-y-3">
              <div className="grid gap-2 sm:grid-cols-2">
                <select
                  value={dept}
                  onChange={(e) => setDept(e.target.value)}
                  className="min-w-0 rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-teal-600 focus:ring-4 focus:ring-teal-600/15"
                >
                  <option value="">All departments</option>
                  {(departments.data ?? []).map((d) => (
                    <option key={d.id} value={d.id}>{d.name}</option>
                  ))}
                </select>
                <select
                  value={year}
                  onChange={(e) => setYear(e.target.value)}
                  className="min-w-0 rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-teal-600 focus:ring-4 focus:ring-teal-600/15"
                >
                  <option value="">All years</option>
                  {[1, 2, 3, 4, 5, 6, 7].map((y) => (
                    <option key={y} value={y}>Year {y}</option>
                  ))}
                </select>
              </div>
              <div className="flex items-center justify-between gap-3">
                <p className="text-xs text-slate-500">
                  {deptId === null && yr === null
                    ? "Choose a department and/or a year."
                    : estimate
                      ? estimate.toAdd === 0
                        ? "Everyone who matches is already in this course."
                        : `${estimate.total} match, ${estimate.toAdd} will be added.`
                      : "All matching students will be added."}
                </p>
                <button
                  disabled={groupDisabled}
                  onClick={() => setConfirmGroup(true)}
                  className="flex items-center gap-1 rounded-xl bg-teal-700 px-4 py-2 text-sm font-bold text-white transition hover:bg-teal-800 disabled:opacity-40"
                >
                  <Plus size={16} /> Add them all
                </button>
              </div>
            </div>
          )}

          {result && (
            <div className="mt-3 rounded-xl border border-teal-100 bg-teal-50 p-3 text-sm">
              <div className="flex items-start justify-between gap-2">
                <p className="font-semibold text-teal-900">
                  Added {result.added} · already enrolled {result.alreadyEnrolled}
                  {result.notFound.length > 0 && ` · not found ${result.notFound.length}`}
                </p>
                <button onClick={() => setResult(null)} className="shrink-0 text-teal-700 hover:text-teal-900" aria-label="Dismiss">
                  <X size={16} />
                </button>
              </div>
              {result.notFound.length > 0 && (
                <details className="mt-2">
                  <summary className="cursor-pointer text-xs font-semibold text-amber-700">Show the ones that were not found</summary>
                  <p className="mt-1 max-h-24 overflow-y-auto break-words text-xs text-slate-600" dir="ltr">{result.notFound.join(", ")}</p>
                </details>
              )}
            </div>
          )}

          {/* students already in the course */}
          <div className="mt-5 border-t border-slate-100 pt-4">
            <div className="mb-2 flex items-center justify-between gap-3">
              <p className="text-sm font-bold text-slate-800">In this course ({enrolled.length})</p>
              <button
                disabled={removeSel.size === 0 || remove.isPending}
                onClick={() => setConfirmRemove(true)}
                className="rounded-lg px-3 py-1.5 text-xs font-bold text-red-600 transition hover:bg-red-50 disabled:opacity-40"
              >
                Remove {removeSel.size || ""} selected
              </button>
            </div>
            <PickList
              items={enrolled}
              selected={removeSel}
              setSelected={setRemoveSel}
              placeholder="Search students in this course..."
              emptyText="No students in this course yet."
            />
          </div>
        </>
      )}

      <Confirm
        open={confirmGroup}
        title="Add students"
        confirmText="Add"
        variant="success"
        text={`Add every student in ${groupLabel} to "${course.name}"?`}
        loading={enroll.isPending}
        onCancel={() => setConfirmGroup(false)}
        onConfirm={() =>
          enroll.mutate({
            ...(deptId !== null ? { departmentId: deptId } : {}),
            ...(yr !== null ? { year: yr } : {}),
          })
        }
      />

      <Confirm
        open={confirmRemove}
        title="Remove students"
        text={`Remove ${removeSel.size} ${removeSel.size === 1 ? "student" : "students"} from "${course.name}"? They will no longer see its exams.`}
        loading={remove.isPending}
        onCancel={() => setConfirmRemove(false)}
        onConfirm={() => remove.mutate(Array.from(removeSel))}
      />
    </div>
  );
}

function ManageCourse({ course }: { course: Course }) {
  const qc = useQueryClient();
  const doctors = useQuery({ queryKey: ["course-doctors", course.id], queryFn: () => adminApi.courseDoctors(course.id) });
  const allDoctors = useQuery({ queryKey: ["doctors"], queryFn: adminApi.doctorList });

  const addDoc = useMutation({
    mutationFn: (id: number) => adminApi.assignDoctor(course.id, id),
    onSuccess: () => {
      toast.success("Doctor assigned");
      qc.invalidateQueries({ queryKey: ["course-doctors", course.id] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const rmDoc = useMutation({
    mutationFn: (id: number) => adminApi.removeDoctor(course.id, id),
    onSuccess: () => {
      toast.success("Doctor removed");
      qc.invalidateQueries({ queryKey: ["course-doctors", course.id] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.7fr)]">
      <Assignment
        title="Doctors"
        icon={GraduationCap}
        assigned={doctors.data ?? []}
        candidates={allDoctors.data ?? []}
        loading={doctors.isLoading}
        onAdd={addDoc.mutate}
        onRemove={rmDoc.mutate}
        busy={addDoc.isPending || rmDoc.isPending}
      />
      <StudentsPanel course={course} />
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