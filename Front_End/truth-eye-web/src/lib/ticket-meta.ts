export const STATUSES = ["Open", "InProgress", "Resolved", "Closed"];
export const CATEGORIES = ["Technical", "Exam", "Account", "Other"];
export const PRIORITIES = ["Low", "Medium", "High"];

export const STATUS_STYLE: Record<string, string> = {
  Open: "bg-amber-50 text-amber-700",
  InProgress: "bg-sky-50 text-sky-700",
  Resolved: "bg-emerald-50 text-emerald-700",
  Closed: "bg-slate-100 text-slate-500",
};

export const PRIORITY_STYLE: Record<string, string> = {
  Low: "bg-slate-100 text-slate-600",
  Medium: "bg-amber-50 text-amber-700",
  High: "bg-red-50 text-red-700",
};

export const statusLabel = (s: string) => (s === "InProgress" ? "In progress" : s);