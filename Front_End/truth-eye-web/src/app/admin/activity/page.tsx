"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Undo2 } from "lucide-react";
import { Confirm } from "@/components/ui/confirm";
import { FilterSelect } from "@/components/ui/filter-select";
import { PageHeader } from "@/components/ui/page-header";
import { adminApi } from "@/lib/api/admin";
import { useAuth } from "@/lib/auth/store";
import type { ActivityLogItem } from "@/types/admin";

const ENTITIES = ["Student", "Doctor", "Proctor", "Staff", "Course", "CourseStudent", "CourseDoctor", "Department"];
const ACTIONS = ["Create", "Update", "Disable", "Enable", "Delete", "Assign", "Unassign", "BulkAssign", "Revert"];

const TONE: Record<string, string> = {
  Create: "bg-emerald-50 text-emerald-700",
  Update: "bg-blue-50 text-blue-700",
  Disable: "bg-amber-50 text-amber-700",
  Enable: "bg-emerald-50 text-emerald-700",
  Delete: "bg-red-50 text-red-700",
  Revert: "bg-purple-50 text-purple-700",
};

export default function ActivityPage() {
const user = useAuth((s) => s.user);
  const canRevert = user?.role === "Admin" || user?.role === "FacultySecretary";
  const qc = useQueryClient();

  const [f, setF] = useState({ entity: "all", action: "all" });
  const [page, setPage] = useState(1);
  const [target, setTarget] = useState<ActivityLogItem | null>(null);

  const { data, isLoading, error } = useQuery({
    queryKey: ["activity", f, page],
    queryFn: () =>
      adminApi.activityLog({
        page,
        pageSize: 25,
        entityType: f.entity === "all" ? undefined : f.entity,
        action: f.action === "all" ? undefined : f.action,
      }),
  });

  const revert = useMutation({
    mutationFn: (id: number) => adminApi.revertLog(id),
    onSuccess: (msg) => {
      toast.success(typeof msg === "string" ? msg : "Action undone");
      qc.invalidateQueries();   // التراجع بيغيّر طلاب وكورسات وغيره
      setTarget(null);
    },
    onError: (e: Error) => {
      toast.error(e.message);
      setTarget(null);
    },
  });

  const items = data?.items ?? [];
  const pages = data ? Math.max(1, Math.ceil(data.total / data.pageSize)) : 1;

  return (
    <div className="space-y-6">
      <PageHeader title="Activity Log" subtitle="Who added, changed or removed what, and when." />

      <div className="flex flex-wrap gap-2">
        <FilterSelect
          label="Type"
          value={f.entity}
          onChange={(v) => { setPage(1); setF({ ...f, entity: v }); }}
          options={ENTITIES.map((e) => ({ value: e, label: e }))}
        />
        <FilterSelect
          label="Action"
          value={f.action}
          onChange={(v) => { setPage(1); setF({ ...f, action: v }); }}
          options={ACTIONS.map((a) => ({ value: a, label: a }))}
        />
      </div>

      <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-slate-200 bg-slate-50 text-xs uppercase text-slate-500">
            <tr>
              <th className="px-4 py-3">When</th>
              <th className="px-4 py-3">Who</th>
              <th className="px-4 py-3">Action</th>
              <th className="px-4 py-3">Details</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr><td colSpan={5} className="px-4 py-8 text-center text-slate-500">Loading...</td></tr>
            )}
            {error && (
              <tr><td colSpan={5} className="px-4 py-8 text-center text-red-600">{(error as Error).message}</td></tr>
            )}
            {!isLoading && !error && items.length === 0 && (
              <tr><td colSpan={5} className="px-4 py-8 text-center text-slate-500">No activity yet.</td></tr>
            )}
            {items.map((l) => (
              <tr key={l.id} className="border-b border-slate-100 last:border-0">
                <td className="whitespace-nowrap px-4 py-3 text-slate-600">{new Date(l.timestamp).toLocaleString()}</td>
                <td className="px-4 py-3">
                  <div className="font-medium text-slate-800">{l.actorName || "—"}</div>
                  <div className="text-xs text-slate-500">{l.actorRole}</div>
                </td>
                <td className="px-4 py-3">
                  <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${TONE[l.action] ?? "bg-slate-100 text-slate-700"}`}>
                    {l.action}
                  </span>
                  <div className="mt-1 text-xs text-slate-500">{l.entityType}</div>
                </td>
                <td className="px-4 py-3 text-slate-700">
                  {l.description}
                  {l.isReverted && <span className="ml-2 text-xs font-semibold text-purple-600">(undone)</span>}
                </td>
                <td className="px-4 py-3 text-right">
                  {canRevert && l.canRevert && (
                    <button
                      type="button"
                      onClick={() => setTarget(l)}
                      className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50"
                    >
                      <Undo2 size={14} /> Undo
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="flex items-center justify-between text-sm text-slate-600">
        <span>{data?.total ?? 0} entries</span>
        <div className="flex items-center gap-2">
          <button
            type="button"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
            className="rounded-lg border border-slate-200 px-3 py-1.5 disabled:opacity-40"
          >
            Previous
          </button>
          <span>Page {page} of {pages}</span>
          <button
            type="button"
            disabled={page >= pages}
            onClick={() => setPage((p) => p + 1)}
            className="rounded-lg border border-slate-200 px-3 py-1.5 disabled:opacity-40"
          >
            Next
          </button>
        </div>
      </div>

      <Confirm
        open={!!target}
        title="Undo this action"
        text={target ? `Undo: ${target.description}` : ""}
        loading={revert.isPending}
        onCancel={() => setTarget(null)}
        onConfirm={() => target && revert.mutate(target.id)}
      />
    </div>
  );
}