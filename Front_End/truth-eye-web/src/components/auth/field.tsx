
import { forwardRef, useId } from "react";
import type { InputHTMLAttributes, ReactNode } from "react";
import { AlertCircle } from "lucide-react";

type FieldProps = InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  icon?: ReactNode;
  trailing?: ReactNode;
  error?: string;
};

export const Field = forwardRef<HTMLInputElement, FieldProps>(
  function Field(
    {
      label,
      icon,
      trailing,
      error,
      className = "",
      id,
      ...props
    },
    ref
  ) {
    const autoId = useId();
    const inputId = id ?? autoId;
    const errorId = `${inputId}-error`;

    return (
      <div>
        <label
          htmlFor={inputId}
          className="mb-1.5 block text-sm font-medium text-slate-700"
        >
          {label}
        </label>

        <div className="relative">
          {icon && (
            <span className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400 [&>svg]:block">
              {icon}
            </span>
          )}

          <input
            id={inputId}
            ref={ref}
            aria-invalid={!!error}
            aria-describedby={error ? errorId : undefined}
            className={`w-full rounded-xl border bg-white py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:ring-4 ${
              icon ? "pl-10" : "pl-4"
            } ${trailing ? "pr-11" : "pr-4"} ${
              error
                ? "border-red-400 focus:border-red-500 focus:ring-red-100"
                : "border-slate-300 hover:border-slate-400 focus:border-teal-600 focus:ring-teal-600/15"
            } ${className}`}
            {...props}
          />

          {trailing && (
            <span className="absolute inset-y-0 right-1.5 my-1.5 flex w-9 items-center justify-center rounded-lg transition hover:bg-slate-100 [&>button]:flex [&>button]:h-full [&>button]:w-full [&>button]:items-center [&>button]:justify-center [&>button]:rounded-lg [&>button]:focus-visible:outline-none [&>button]:focus-visible:ring-2 [&>button]:focus-visible:ring-teal-600">
              {trailing}
            </span>
          )}
        </div>

        {error && (
          <p
            id={errorId}
            role="alert"
            className="mt-1.5 flex items-center gap-1.5 text-xs text-red-600"
          >
            <AlertCircle size={13} />
            {error}
          </p>
        )}
      </div>
    );
  }
);

Field.displayName = "Field";