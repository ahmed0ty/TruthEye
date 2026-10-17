export interface SupportTicket {
  id: number;
  ticketNumber: string;
  userName?: string | null;
  assignedToName?: string | null;
  assignedToSupportId?: number | null;
  category?: string | null;
  priority?: string | null;
  status: string;
  subject?: string | null;
  description?: string | null;
  resolution?: string | null;
  createdAt: string;
  resolvedAt?: string | null;
}

export const TICKET_STATUSES = ["Open", "In Progress", "Resolved", "Closed"] as const;

export const STATUS_STYLE: Record<string, string> = {
  Open: "bg-amber-50 text-amber-700",
  "In Progress": "bg-sky-50 text-sky-700",
  Resolved: "bg-emerald-50 text-emerald-700",
  Closed: "bg-slate-100 text-slate-500",
};

export const PRIORITY_STYLE: Record<string, string> = {
  High: "bg-red-50 text-red-700",
  Medium: "bg-amber-50 text-amber-700",
  Low: "bg-slate-100 text-slate-600",
};