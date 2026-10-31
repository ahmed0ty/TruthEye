import { cn } from "@/lib/utils";

export type IntegrityFilterValue = "all" | "flagged" | "clean";

const OPTIONS: { id: IntegrityFilterValue; label: string; active: string }[] = [
  { id: "all", label: "All", active: "bg-[#0A2540] text-white" },
  { id: "flagged", label: "Flagged", active: "bg-red-600 text-white" },
  { id: "clean", label: "Clean", active: "bg-emerald-600 text-white" },
];

export function IntegrityFilter({
  value,
  onChange,
  counts,
}: {
  value: IntegrityFilterValue;
  onChange: (v: IntegrityFilterValue) => void;
  counts: Record<IntegrityFilterValue, number>;
}) {
  return (
    <div className="flex flex-wrap gap-2" role="group" aria-label="Filter by integrity">
      {OPTIONS.map((o) => (
        <button
          key={o.id}
          onClick={() => onChange(o.id)}
          aria-pressed={value === o.id}
          className={cn(
            "rounded-full px-4 py-2 text-sm font-semibold transition",
            value === o.id ? o.active : "bg-white text-slate-600 ring-1 ring-slate-200 hover:bg-slate-50"
          )}
        >
          {o.label} <span className="opacity-80">({counts[o.id]})</span>
        </button>
      ))}
    </div>
  );
}