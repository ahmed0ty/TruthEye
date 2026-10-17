export function PageHeader({
  title,
  subtitle,
  action,
}: {
  title: string;
  subtitle?: string;
  action?: React.ReactNode;
}) {
  return (
    <div className="animate-fade-up flex flex-wrap items-end justify-between gap-4">
      <div>
        <h1 className="flex items-center gap-3 text-2xl font-bold tracking-tight text-[#0A2540]">
          <span aria-hidden className="h-7 w-1.5 rounded-full bg-gradient-to-b from-teal-400 to-teal-700" />
          {title}
        </h1>
        {subtitle && <p className="mt-1 ps-[1.125rem] text-sm text-slate-500">{subtitle}</p>}
      </div>
      {action}
    </div>
  );
}