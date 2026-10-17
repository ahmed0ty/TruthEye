// "use client";

// import { useEffect, useState } from "react";
// import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
// import { toast } from "sonner";
// import { UserCheck } from "lucide-react";
// import { DataTable } from "@/components/ui/data-table";
// import { Modal } from "@/components/ui/modal";
// import { PageHeader } from "@/components/ui/page-header";
// import { ticketsApi } from "@/lib/api/tickets";
// import { useAuth } from "@/lib/auth/store";
// import { fmtDateTime } from "@/lib/format";
// import { PRIORITY_STYLE, STATUSES, STATUS_STYLE, statusLabel } from "@/lib/ticket-meta";
// import { cn } from "@/lib/utils";
// import type { Ticket } from "@/types/ticket";

// const control =
//   "w-full rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-sm outline-none transition focus:border-teal-600 focus:ring-4 focus:ring-teal-100";

// function TicketDetail({ ticket, onDone }: { ticket: Ticket; onDone: () => void }) {
//   const qc = useQueryClient();
//   const me = useAuth((s) => s.user);
//   const [status, setStatus] = useState(ticket.status);
//   const [resolution, setResolution] = useState(ticket.resolution ?? "");

//   useEffect(() => {
//     setStatus(ticket.status);
//     setResolution(ticket.resolution ?? "");
//   }, [ticket]);

//   const refresh = () => qc.invalidateQueries({ queryKey: ["tickets"] });

//   const assign = useMutation({
//     mutationFn: () => ticketsApi.assign(ticket.id, me!.id),
//     onSuccess: () => {
//       toast.success("Ticket assigned to you");
//       refresh();
//     },
//     onError: (e: Error) => toast.error(e.message),
//   });

//   const save = useMutation({
//     mutationFn: () => ticketsApi.update(ticket.id, { status, resolution }),
//     onSuccess: () => {
//       toast.success("Ticket updated");
//       refresh();
//       onDone();
//     },
//     onError: (e: Error) => toast.error(e.message),
//   });

//   return (
//     <div className="space-y-4 text-sm">
//       <div className="grid gap-3 sm:grid-cols-2">
//         <p className="text-slate-500">From: <b className="text-[#0A2540]">{ticket.userName ?? "—"}</b></p>
//         <p className="text-slate-500">{ticket.ticketNumber} · {ticket.category}</p>
//       </div>

//       <div className="rounded-xl bg-slate-50 p-4 text-slate-700">
//         <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-400">Description</p>
//         <p className="whitespace-pre-wrap">{ticket.description}</p>
//       </div>

//       <div className="flex items-center justify-between gap-3 rounded-xl border border-slate-200 p-3">
//         <p className="text-slate-500">
//           Assigned to: <b className="text-[#0A2540]">{ticket.assignedToName || "Nobody yet"}</b>
//         </p>
//         <button
//           onClick={() => assign.mutate()}
//           disabled={assign.isPending || !me}
//           className="inline-flex items-center gap-1.5 rounded-lg bg-teal-50 px-3 py-1.5 text-xs font-semibold text-teal-700 transition hover:bg-teal-100 disabled:opacity-50"
//         >
//           <UserCheck size={14} /> Assign to me
//         </button>
//       </div>

//       <div>
//         <label className="mb-1.5 block font-semibold text-slate-700">Status</label>
//         <select value={status} onChange={(e) => setStatus(e.target.value)} className={control}>
//           {STATUSES.map((s) => <option key={s} value={s}>{statusLabel(s)}</option>)}
//         </select>
//       </div>

//       <div>
//         <label className="mb-1.5 block font-semibold text-slate-700">Resolution / reply</label>
//         <textarea rows={4} value={resolution} onChange={(e) => setResolution(e.target.value)} className={cn(control, "resize-none")} placeholder="What was done to solve this?" />
//       </div>

//       <div className="flex justify-end">
//         <button
//           onClick={() => save.mutate()}
//           disabled={save.isPending}
//           className="rounded-xl bg-teal-600 px-6 py-2.5 font-semibold text-white transition hover:bg-teal-700 disabled:opacity-60"
//         >
//           {save.isPending ? "Saving..." : "Save changes"}
//         </button>
//       </div>
//     </div>
//   );
// }

// export default function SupportTicketsPage() {
//   const [tab, setTab] = useState<"all" | "assigned">("all");
//   const [status, setStatus] = useState("all");
//   const [open, setOpen] = useState<Ticket | null>(null);

//   const all = useQuery({ queryKey: ["tickets", "all"], queryFn: ticketsApi.all, enabled: tab === "all" });
//   const assigned = useQuery({ queryKey: ["tickets", "assigned"], queryFn: ticketsApi.assigned, enabled: tab === "assigned" });
//   const q = tab === "all" ? all : assigned;

//   const rows = [...(q.data ?? [])]
//     .filter((t) => status === "all" || t.status === status)
//     .sort((a, b) => b.id - a.id);

//   return (
//     <div className="space-y-6">
//       <PageHeader title="Tickets" subtitle="Requests from students and doctors." />

//       <div className="flex flex-wrap items-center gap-2">
//         {(["all", "assigned"] as const).map((t) => (
//           <button
//             key={t}
//             onClick={() => setTab(t)}
//             className={cn(
//               "rounded-full px-4 py-2 text-sm font-semibold transition",
//               tab === t ? "bg-[#0A2540] text-white" : "bg-white text-slate-600 ring-1 ring-slate-200 hover:bg-slate-50"
//             )}
//           >
//             {t === "all" ? "All tickets" : "Assigned to me"}
//           </button>
//         ))}
//         <select value={status} onChange={(e) => setStatus(e.target.value)} className="ms-auto rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-teal-600">
//           <option value="all">All statuses</option>
//           {STATUSES.map((s) => <option key={s} value={s}>{statusLabel(s)}</option>)}
//         </select>
//       </div>

//       <DataTable
//         rows={rows}
//         loading={q.isLoading}
//         error={q.error ? (q.error as Error).message : undefined}
//         searchText={(r) => `${r.ticketNumber} ${r.subject} ${r.userName ?? ""}`}
//         placeholder="Search tickets..."
//         columns={[
//           {
//             header: "Ticket",
//             cell: (r) => (
//               <div>
//                 <p className="font-semibold text-[#0A2540]">{r.subject}</p>
//                 <p className="text-xs text-slate-400">{r.ticketNumber} · {r.category}</p>
//               </div>
//             ),
//           },
//           { header: "From", cell: (r) => r.userName ?? "—" },
//           { header: "Priority", cell: (r) => <span className={cn("rounded-full px-2.5 py-1 text-xs font-semibold", PRIORITY_STYLE[r.priority] ?? "bg-slate-100")}>{r.priority}</span> },
//           { header: "Status", cell: (r) => <span className={cn("rounded-full px-2.5 py-1 text-xs font-semibold", STATUS_STYLE[r.status] ?? "bg-slate-100")}>{statusLabel(r.status)}</span> },
//           { header: "Assigned", cell: (r) => r.assignedToName || <span className="text-slate-400">—</span> },
//           { header: "Created", cell: (r) => <span className="text-slate-500">{r.createdAt ? fmtDateTime(r.createdAt) : "—"}</span> },
//         ]}
//         actions={(r) => (
//           <button onClick={() => setOpen(r)} className="rounded-lg bg-teal-50 px-3 py-1.5 text-xs font-semibold text-teal-700 transition hover:bg-teal-100">
//             Open
//           </button>
//         )}
//       />

//       <Modal open={!!open} title={open?.subject ?? ""} onClose={() => setOpen(null)} wide>
//         {open && <TicketDetail ticket={open} onDone={() => setOpen(null)} />}
//       </Modal>
//     </div>
//   );
// }