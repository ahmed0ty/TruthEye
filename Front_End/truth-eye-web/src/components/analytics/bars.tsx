export function BarChart({
  data, tone = "bg-teal-500",
}: { data: { label: string; value: number; extra?: number }[]; tone?: string }) {
  const max = Math.max(1, ...data.map((d) => d.value));
  return (
    <div className="flex h-44 items-end gap-1.5">
      {data.map((d) => (
        <div key={d.label} className="flex h-full min-w-0 flex-1 flex-col items-center justify-end gap-1" title={`${d.label}: ${d.value}`}>
          <span className="text-[10px] font-semibold text-slate-500">{d.value || ""}</span>
          <div className="relative flex w-full flex-1 items-end">
            <div className={`w-full rounded-t ${tone}`} style={{ height: `${(d.value / max) * 100}%`, minHeight: d.value ? 3 : 0 }} />
          </div>
          <span className="w-full truncate text-center text-[10px] text-slate-400">{d.label}</span>
        </div>
      ))}
    </div>
  );
}

export function Stat({ label, value, tone = "text-slate-900" }: { label: string; value: string | number; tone?: string }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-5">
      <p className="text-sm font-semibold text-slate-500">{label}</p>
      <p className={`mt-0.5 text-2xl font-extrabold ${tone}`}>{value}</p>
    </div>
  );
}