"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { ArrowLeft, ArrowRight, Loader2, ShieldCheck } from "lucide-react";
import { Field } from "@/components/auth/field";
import { authApi } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";

const schema = z.object({
  code: z.string().trim().min(6, "Enter the 6-digit code or a recovery code").max(16, "Code is too long"),
});
type Values = z.infer<typeof schema>;

export function TwoFactorForm({
  challengeToken,
  onVerified,
  onBack,
}: {
  challengeToken: string;
  onVerified: (session: { token: string; mustChangePassword: boolean }) => void;
  onBack: () => void;
}) {
  const [error, setError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<Values>({ resolver: zodResolver(schema) });

  const onSubmit = async ({ code }: Values) => {
    setError(null);
    try {
      const res = await authApi.loginWithTwoFactor({ challengeToken, code });
      onVerified({ token: res.token, mustChangePassword: !!res.mustChangePassword });
    } catch (e) {
      const message = e instanceof Error ? e.message : "Verification failed";
      if (e instanceof ApiError && e.errorCode === "AUTH_CHALLENGE_EXPIRED") {
        toast.error(message);
        onBack();
        return;
      }
      setError(message);
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="animate-fade-up space-y-4" noValidate>
      <div>
        <span className="inline-flex items-center rounded-full bg-teal-50 px-2.5 py-0.5 text-[11px] font-semibold uppercase tracking-wider text-teal-700 ring-1 ring-teal-600/15">
          Two-step verification
        </span>
        <h2 className="mt-3 text-2xl font-bold tracking-tight text-[#0A2540]">Enter your code</h2>
        <p className="mt-1.5 text-sm text-slate-500">
          Open your authenticator app and enter the 6-digit code, or use one of your recovery codes.
        </p>
      </div>

      <Field
        label="Verification code"
        icon={<ShieldCheck size={18} />}
        dir="ltr"
        autoComplete="one-time-code"
        placeholder="123456"
        error={errors.code?.message ?? error ?? undefined}
        {...register("code")}
      />

      <button
        type="submit"
        disabled={isSubmitting}
        className="flex w-full items-center justify-center gap-2 rounded-xl bg-gradient-to-r from-teal-700 to-teal-600 py-3 text-sm font-semibold text-white shadow-lg shadow-teal-700/25 transition hover:-translate-y-0.5 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isSubmitting ? (
          <>
            <Loader2 size={18} className="animate-spin motion-reduce:animate-none" />
            Verifying...
          </>
        ) : (
          <>
            Verify
            <ArrowRight size={18} />
          </>
        )}
      </button>

      <button
        type="button"
        onClick={onBack}
        className="flex w-full items-center justify-center gap-2 text-sm font-medium text-slate-500 transition hover:text-slate-800"
      >
        <ArrowLeft size={16} />
        Back to sign in
      </button>
    </form>
  );
}
