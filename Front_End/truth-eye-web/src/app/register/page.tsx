"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { ArrowRight, Eye, EyeOff, IdCard, Loader2, Lock, Mail, Phone, ShieldPlus, User } from "lucide-react";
import { AuthShell } from "@/components/auth/auth-shell";
import { Field } from "@/components/auth/field";
import { authApi } from "@/lib/api/auth";

const schema = z
  .object({
    firstName: z.string().trim().min(2, "First name is required").max(50),
    lastName: z.string().trim().min(2, "Last name is required").max(50),
    email: z.string().trim().email("Invalid email address"),
    nationalId: z.string().regex(/^\d{14}$/, "National ID must be 14 digits"),
    phoneNumber: z.string().trim().min(8, "Phone number is required"),
    password: z.string().min(6, "Password must be at least 6 characters"),
    confirm: z.string(),
  })
  .refine((d) => d.password === d.confirm, { path: ["confirm"], message: "Passwords do not match" });

type FormValues = z.infer<typeof schema>;

export default function RegisterPage() {
  const router = useRouter();
  const [show, setShow] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const onSubmit = async ({ confirm: _c, ...data }: FormValues) => {
    try {
      await authApi.registerSuperAdmin(data);
      toast.success("System admin account created, sign in now");
      router.replace("/login");
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Failed to create account");
    }
  };

  return (
    <AuthShell>
      <div className="rounded-3xl border border-slate-200 bg-white p-8 shadow-xl shadow-slate-200/60">
        <div className="mb-6 flex items-start gap-3">
          <div className="rounded-xl bg-[#0A2540] p-2.5 text-teal-300">
            <ShieldPlus size={24} />
          </div>
          <div>
            <h1 className="text-xl font-bold text-[#0A2540]">System admin setup</h1>
            <p className="mt-0.5 text-sm text-slate-500">Create the first account (Super Admin)</p>
          </div>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <div className="grid grid-cols-2 gap-3">
            <Field label="First name" icon={<User size={18} />} error={errors.firstName?.message} {...register("firstName")} />
            <Field label="Last name" icon={<User size={18} />} error={errors.lastName?.message} {...register("lastName")} />
          </div>

          <Field label="Email" icon={<Mail size={18} />} dir="ltr" type="email" placeholder="admin@example.com" error={errors.email?.message} {...register("email")} />
          <Field label="National ID" icon={<IdCard size={18} />} dir="ltr" inputMode="numeric" maxLength={14} error={errors.nationalId?.message} {...register("nationalId")} />
          <Field label="Phone number" icon={<Phone size={18} />} dir="ltr" placeholder="+201000000000" error={errors.phoneNumber?.message} {...register("phoneNumber")} />

          <Field
            label="Password"
            icon={<Lock size={18} />}
            type={show ? "text" : "password"}
            dir="ltr"
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
          <Field label="Confirm password" icon={<Lock size={18} />} type={show ? "text" : "password"} dir="ltr" error={errors.confirm?.message} {...register("confirm")} />

          <button
            type="submit"
            disabled={isSubmitting}
            className="group flex w-full items-center justify-center gap-2 rounded-xl bg-teal-700 py-3.5 text-sm font-semibold text-white shadow-sm transition hover:bg-teal-800 focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-teal-600/30 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? (
              <>
                <Loader2 size={18} className="animate-spin motion-reduce:animate-none" />
                Creating account...
              </>
            ) : (
              <>
                Create account
                <ArrowRight
                  size={18}
                  className="transition group-hover:translate-x-0.5 motion-reduce:transition-none"
                />
              </>
            )}
          </button>
        </form>

        <div className="mt-5 border-t border-slate-100 pt-4 text-center text-sm text-slate-500">
          Already have an account?{" "}
          <Link href="/login" className="font-semibold text-teal-700 transition hover:text-teal-800 hover:underline">
            Sign in
          </Link>
        </div>
      </div>
    </AuthShell>
  );
}