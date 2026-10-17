import { forwardRef, type InputHTMLAttributes, type SelectHTMLAttributes } from "react";
import { cn } from "@/lib/utils";

const base =
  "w-full rounded-xl border bg-white px-4 py-2.5 text-sm outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-4 focus:ring-teal-600/15 disabled:bg-slate-50 disabled:text-slate-400";

function Wrap({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="mb-1.5 block text-sm font-semibold text-slate-700">{label}</label>
      {children}
      {error && <p className="mt-1 text-xs font-medium text-red-600">{error}</p>}
    </div>
  );
}

export const TextField = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement> & { label: string; error?: string }>(
  function TextField({ label, error, className, ...props }, ref) {
    return (
      <Wrap label={label} error={error}>
        <input ref={ref} {...props} className={cn(base, error ? "border-red-400" : "border-slate-300", className)} />
      </Wrap>
    );
  }
);

export const SelectField = forwardRef<
  HTMLSelectElement,
  SelectHTMLAttributes<HTMLSelectElement> & {
    label: string;
    error?: string;
    options: { value: string | number; label: string }[];
    placeholder?: string;
  }
>(function SelectField({ label, error, options, placeholder, className, ...props }, ref) {
  return (
    <Wrap label={label} error={error}>
      <select ref={ref} {...props} className={cn(base, error ? "border-red-400" : "border-slate-300", className)}>
        {placeholder && <option value="">{placeholder}</option>}
        {options.map((o) => (
          <option key={o.value} value={o.value}>{o.label}</option>
        ))}
      </select>
    </Wrap>
  );
});

export const TextAreaField = forwardRef<
  HTMLTextAreaElement,
  React.TextareaHTMLAttributes<HTMLTextAreaElement> & { label: string; error?: string }
>(function TextAreaField({ label, error, className, ...props }, ref) {
  return (
    <Wrap label={label} error={error}>
      <textarea ref={ref} rows={3} {...props} className={cn(base, "resize-none", error ? "border-red-400" : "border-slate-300", className)} />
    </Wrap>
  );
});

export function SubmitButton({ loading, children }: { loading?: boolean; children: React.ReactNode }) {
  return (
    <button
      disabled={loading}
      className="flex items-center justify-center gap-2 rounded-xl bg-teal-700 hover:bg-teal-800 px-6 py-2.5 text-sm font-bold text-white shadow-lg shadow-teal-700/30 transition hover:shadow-xl active:scale-[0.98] disabled:opacity-60"
    >
      {loading && <span className="h-4 w-4 animate-spin rounded-full border-2 border-white/40 border-t-white" />}
      {children}
    </button>
  );
}