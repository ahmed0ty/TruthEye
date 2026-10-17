import type { LucideIcon } from "lucide-react";

export function EmptyState({
  icon: Icon,
  title,
  text,
}: {
  icon: LucideIcon;
  title: string;
  text?: string;
}) {
  return (
    <div className="animate-fade-up flex flex-col items-center rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-16 text-center">
      <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-slate-100 text-slate-400">
        <Icon size={30} />
      </div>
      <p className="font-bold text-[#0A2540]">{title}</p>
      {text && <p className="mt-1 max-w-sm text-sm text-slate-500">{text}</p>}
    </div>
  );
}