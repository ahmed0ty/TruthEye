"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { ArrowLeft, Eye, EyeOff, Loader2, Lock, User } from "lucide-react";
import { AuthShell } from "@/components/auth/auth-shell";
import { Field } from "@/components/auth/field";
import { authApi } from "@/lib/api/auth";
import { useAuth } from "@/lib/auth/store";
import { ROLE_HOME } from "@/types/api";
import { IntroGate } from "@/components/auth/intro-gate";
const schema = z.object({
  identifier: z.string().trim().min(1, "Enter your email or national ID"),
  password: z.string().min(6, "Password must be at least 6 characters"),
});
type FormValues = z.infer<typeof schema>;

export default function LoginPage() {
  const router = useRouter();
  const setSession = useAuth((s) => s.setSession);
  const [show, setShow] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const onSubmit = async ({ identifier, password }: FormValues) => {
    try {
      const isEmail = identifier.includes("@");
      const { token, refreshToken } = await authApi.login({
        email: isEmail ? identifier : undefined,
        nationalId: isEmail ? undefined : identifier,
        password,
      });

      const user = setSession(token, refreshToken);
      if (!user) throw new Error("Invalid token");

      toast.success(`Welcome ${user.name}`);
      router.replace(ROLE_HOME[user.role] ?? "/login");
      router.refresh();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Login failed");
    }
  };

  return (
    <IntroGate>
    <AuthShell>
      <h1 className="text-3xl font-bold text-[#0A2540]">Welcome back 👋</h1>
      <p className="mt-2 text-sm leading-6 text-slate-500">
        Sign in with your email or national ID to continue to your account.
      </p>

      <form onSubmit={handleSubmit(onSubmit)} className="mt-8 space-y-5" noValidate>
        <Field
          label="Email or National ID"
          icon={<User size={18} />}
          dir="ltr"
          autoComplete="username"
          placeholder="name@example.com"
          error={errors.identifier?.message}
          {...register("identifier")}
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
          {...register("password")}
        />

        <button
          type="submit"
          disabled={isSubmitting}
          className="group flex w-full items-center justify-center gap-2 rounded-xl bg-teal-700 py-3.5 text-sm font-semibold text-white shadow-sm transition hover:bg-teal-800 focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-teal-600/30 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {isSubmitting ? (
            <>
              <Loader2 size={18} className="animate-spin motion-reduce:animate-none" />
              Signing in...
            </>
          ) : (
            <>
              Sign in
              <ArrowLeft
                size={18}
                className="transition group-hover:-translate-x-0.5 motion-reduce:transition-none"
              />
            </>
          )}
        </button>
      </form>

      <div className="mt-8 border-t border-slate-200 pt-6 text-center text-sm text-slate-500">
        First time running the system?{" "}
        <Link
          href="/register"
          className="font-semibold text-teal-700 transition hover:text-teal-800 hover:underline"
        >
          Set up the system admin
        </Link>
      </div>

      <p className="mt-5 text-center text-xs text-slate-400">
        Student and doctor accounts are added by the faculty administration
      </p>
    </AuthShell>
    </IntroGate>
  );
}