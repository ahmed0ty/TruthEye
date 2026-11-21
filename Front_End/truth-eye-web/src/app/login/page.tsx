"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { ArrowRight, Eye, EyeOff, Loader2, Lock, User } from "lucide-react";
import { AuthShell } from "@/components/auth/auth-shell";
import { Field } from "@/components/auth/field";
import type { EyeMode } from "@/components/auth/eye-guardian";
import { authApi } from "@/lib/api/auth";
import { useAuth } from "@/lib/auth/store";
import { ROLE_HOME } from "@/types/api";
import { IntroGate } from "@/components/auth/intro-gate";

const schema = z.object({
  identifier: z.string().trim().min(1, "Enter your email or national ID"),
  password: z.string().min(1, "Enter your password"),
});
type FormValues = z.infer<typeof schema>;

export default function LoginPage() {
  const router = useRouter();
  const setSession = useAuth((s) => s.setSession);
  const [show, setShow] = useState(false);
  const [eye, setEye] = useState<EyeMode>("idle");

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const identifierField = register("identifier");
  const passwordField = register("password");

  // Leaving a field returns the eye to idle, but keeps an error or success on screen
  const settle = () => setEye((m) => (m === "error" || m === "success" ? m : "idle"));

  const onSubmit = async ({ identifier, password }: FormValues) => {
    setEye("watching");
    try {
      const isEmail = identifier.includes("@");
      const { token, mustChangePassword } = await authApi.login({
        email: isEmail ? identifier : undefined,
        nationalId: isEmail ? undefined : identifier,
        password,
      });

      const user = setSession(token);
      if (!user) throw new Error("Invalid token");

      setEye("success");
      toast.success(`Welcome ${user.name}`);
      router.replace(mustChangePassword ? "/change-password" : (ROLE_HOME[user.role] ?? "/login"));
      router.refresh();
    } catch (e) {
      setEye("error");
      toast.error(e instanceof Error ? e.message : "Login failed");
    }
  };

  return (
    <IntroGate>
      <AuthShell eye={eye}>
        <div className="animate-fade-up">
          <span className="inline-flex items-center rounded-full bg-teal-50 px-2.5 py-0.5 text-[11px] font-semibold uppercase tracking-wider text-teal-700 ring-1 ring-teal-600/15">
            Secure sign-in
          </span>
          <h1 className="mt-3 text-2xl font-bold tracking-tight text-[#0A2540]">Welcome back</h1>
          <p className="mt-1.5 text-sm text-slate-500">
            Sign in with your email or national ID to continue.
          </p>
        </div>

        <form
          onSubmit={handleSubmit(onSubmit)}
          className="animate-fade-up mt-6 space-y-4"
          style={{ animationDelay: "120ms" }}
          noValidate
        >
          <Field
            label="Email or National ID"
            icon={<User size={18} />}
            dir="ltr"
            autoComplete="username"
            placeholder="name@example.com"
            error={errors.identifier?.message}
            {...identifierField}
            onFocus={() => setEye("watching")}
            onBlur={(e) => {
              identifierField.onBlur(e);
              settle();
            }}
          />

          <Field
            label="Password"
            icon={<Lock size={18} />}
            type={show ? "text" : "password"}
            dir="ltr"
            autoComplete="current-password"
            placeholder="••••••••"
            error={errors.password?.message}
            trailing={
              <button
                type="button"
                onClick={() => setShow((v) => !v)}
                className="text-slate-400 transition hover:text-slate-700"
                aria-label={show ? "Hide password" : "Show password"}
              >
                {show ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            }
            {...passwordField}
            onFocus={() => setEye("private")}
            onBlur={(e) => {
              passwordField.onBlur(e);
              settle();
            }}
          />

          <button
            type="submit"
            disabled={isSubmitting}
            className="group relative mt-2 flex w-full items-center justify-center gap-2 overflow-hidden rounded-xl bg-gradient-to-r from-teal-700 to-teal-600 py-3 text-sm font-semibold text-white shadow-lg shadow-teal-700/25 transition duration-300 hover:-translate-y-0.5 hover:shadow-xl hover:shadow-teal-700/35 active:translate-y-0 active:scale-[0.99] focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-teal-600/30 disabled:cursor-not-allowed disabled:opacity-60 disabled:hover:translate-y-0 disabled:hover:shadow-lg"
          >
            <span
              aria-hidden
              className="pointer-events-none absolute inset-0 -translate-x-full bg-gradient-to-r from-transparent via-white/25 to-transparent transition-transform duration-700 group-hover:translate-x-full"
            />
            {isSubmitting ? (
              <>
                <Loader2 size={18} className="animate-spin motion-reduce:animate-none" />
                Signing in...
              </>
            ) : (
              <>
                Sign in
                <ArrowRight
                  size={18}
                  className="transition duration-300 group-hover:translate-x-1 motion-reduce:transition-none"
                />
              </>
            )}
          </button>
        </form>

        <div
          className="animate-fade-up mt-6 border-t border-slate-200 pt-5"
          style={{ animationDelay: "240ms" }}
        >
          <p className="text-center text-sm text-slate-500">First time running the system?</p>
          <Link
            href="/register"
            className="mt-3 flex w-full items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white py-2.5 text-sm font-semibold text-[#0A2540] transition hover:border-teal-600/40 hover:bg-teal-50/60 hover:text-teal-800"
          >
            Set up the system admin
          </Link>
        </div>

        <p
          className="animate-fade-up mt-4 text-center text-xs text-slate-400"
          style={{ animationDelay: "300ms" }}
        >
          Student and doctor accounts are added by the faculty administration
        </p>
      </AuthShell>
    </IntroGate>
  );
}
