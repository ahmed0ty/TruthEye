import type { FieldValues, UseFormRegister, UseFormWatch } from "react-hook-form";
import { z } from "zod";
import { TextField } from "@/components/ui/form-fields";

/** قواعد الباسورد اليدوي، بتتضاف على فورم الإنشاء */
export const passwordModeSchema = z
  .object({
    passwordMode: z.enum(["email", "manual"]).optional(),
    password: z.string().optional(),
    confirmPassword: z.string().optional(),
  })
  .superRefine((v, ctx) => {
    if (v.passwordMode !== "manual") return;
    if (!v.password || v.password.length < 8) {
      ctx.addIssue({ code: "custom", path: ["password"], message: "At least 8 characters" });
    } else if (v.password !== v.confirmPassword) {
      ctx.addIssue({ code: "custom", path: ["confirmPassword"], message: "Passwords do not match" });
    }
  });

/** يحوّل قيم الفورم لـ payload الإنشاء: إيميل برابط، أو باسورد يدوي */
export function withPasswordMode(v: Record<string, unknown>) {
  const { passwordMode, password, confirmPassword, ...rest } = v;
  void confirmPassword;
  return passwordMode === "manual"
    ? { ...rest, sendSetupLink: false, password }
    : { ...rest, sendSetupLink: true };
}

export function PasswordModeFields({
  register,
  watch,
  error,
}: {
  register: UseFormRegister<FieldValues>;
  watch: UseFormWatch<FieldValues>;
  error: (k: string) => string | undefined;
}) {
  const mode = watch("passwordMode") ?? "email";
  return (
    <div className="space-y-3 rounded-lg border border-slate-200 p-4">
      <p className="text-sm font-medium text-slate-700">Account password</p>
      <div className="flex flex-wrap gap-6 text-sm text-slate-700">
        <label className="flex items-center gap-2">
          <input type="radio" value="email" defaultChecked {...register("passwordMode")} />
          Send a setup link by email
        </label>
        <label className="flex items-center gap-2">
          <input type="radio" value="manual" {...register("passwordMode")} />
          Set the password myself
        </label>
      </div>
      {mode === "manual" && (
        <div className="grid gap-4 sm:grid-cols-2">
          <TextField label="Password" type="password" dir="ltr" autoComplete="new-password" error={error("password")} {...register("password")} />
          <TextField label="Confirm password" type="password" dir="ltr" autoComplete="new-password" error={error("confirmPassword")} {...register("confirmPassword")} />
        </div>
      )}
    </div>
  );
}
