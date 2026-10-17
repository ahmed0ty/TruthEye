/** Backend dates are UTC but often come without a "Z". Treat them as UTC. */
export function parseDate(v?: string | null): Date | null {
  if (!v) return null;
  const hasTz = /([zZ]|[+-]\d{2}:?\d{2})$/.test(v);
  const d = new Date(hasTz ? v : `${v}Z`);
  return Number.isNaN(+d) ? null : d;
}

export function fmtDateTime(v?: string | null) {
  const d = parseDate(v);
  return d ? d.toLocaleString("en-US", { dateStyle: "medium", timeStyle: "short" }) : "—";
}

/** .NET TimeSpan ("hh:mm:ss.fffffff" or "d.hh:mm:ss") -> seconds */
export function parseTimeSpan(v?: string | null): number {
  if (!v) return 0;
  const m = /^(?:(\d+)\.)?(\d{1,2}):(\d{2}):(\d{2})/.exec(v);
  if (!m) return 0;
  const [, d, h, min, s] = m;
  return (Number(d ?? 0) * 24 + Number(h)) * 3600 + Number(min) * 60 + Number(s);
}

export function fmtSeconds(total: number) {
  const h = Math.floor(total / 3600);
  const m = Math.floor((total % 3600) / 60);
  const s = total % 60;
  if (h) return `${h}h ${String(m).padStart(2, "0")}m`;
  if (m) return `${m}m ${String(s).padStart(2, "0")}s`;
  return `${s}s`;
}
/** UTC string from backend -> value for <input type="datetime-local"> (local time) */
export function toInputValue(v?: string | null): string {
  const d = parseDate(v);
  if (!d) return "";
  const p = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}T${p(d.getHours())}:${p(d.getMinutes())}`;
}

/** datetime-local value (local time) -> ISO UTC string for the backend */
export function fromInputValue(v: string): string {
  return new Date(v).toISOString();
}