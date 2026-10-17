"use client";

import { useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { Eye, EyeOff, IdCard, KeyRound, Mail, Phone, ShieldCheck } from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";
import { SubmitButton, TextField } from "@/components/ui/form-fields";
import { authApi } from "@/lib/api/auth";
import { useAuth } from "@/lib/auth/store";
import { fmtDateTime } from "@/lib/format";
import { ROLE_LABEL } from "@/types/api";
import { AvatarUploader } from "@/components/profile/avatar-uploader";

const schema = z
  .object({
    currentPassword: z.string().min(1, "Required"),
    newPassword: z.string().min(6, "At least 6 characters").max(100),
    confirmNewPassword: z.string().min(1, "Required"),
  })
  .refine((d) => d.newPassword === d.confirmNewPassword, { path: ["confirmNewPassword"], message: "Passwords don’t match" })
  .refine((d) => d.newPassword !== d.currentPassword, { path: ["newPassword"], message: "Must differ from the current password" });

type Values = z.infer<typeof schema>;

export default function ProfilePage() {
  const user = useAuth((s) => s.user);
  const logout = useAuth((s) => s.logout);
  const [show, setShow] = useState(false);
  const profile = useQuery({ queryKey: ["profile"], queryFn: authApi.profile });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<Values>({ resolver: zodResolver(schema) });

  const change = useMutation({
    mutationFn: (v: Values) => authApi.changePassword(v),
    onSuccess: () => {
      reset();
      toast.success("Password changed. Please sign in again.");
      // The server revokes all sessions after a password change
      setTimeout(logout, 1500);
    },
    onError: (e: Error) => toast.error(e.message),
  });

  if (!user) return null;
  const p = profile.data;
  const name = p ? `${p.firstName} ${p.lastName}` : user.name;

  const info = [
    { icon: Mail, label: "Email", value: p?.email ?? user.email, ltr: true },
    { icon: IdCard, label: "National ID", value: p?.nationalId, ltr: true },
    { icon: Phone, label: "Phone", value: p?.phoneNumber, ltr: true },
    { icon: ShieldCheck, label: "Role", value: ROLE_LABEL[user.role] },
  ];

  const eye = (
    <button type="button" onClick={() => setShow((v) => !v)} className="text-slate-400 hover:text-slate-600" aria-label="Toggle password visibility">
      {show ? <EyeOff size={18} /> : <Eye size={18} />}
    </button>
  );

  return (
    <div className="space-y-6">
      <PageHeader title="My Profile" subtitle="Your account details and security." />

      <div className="grid gap-6 lg:grid-cols-2">
        <section className="animate-fade-up rounded-2xl border border-slate-200 bg-white p-6">
          <div className="mb-6">
            <AvatarUploader name={name} image={p?.profileImage} />
            <h2 className="mt-4 text-lg font-bold text-[#0A2540]">{name}</h2>
            <p className="text-sm text-slate-500">{ROLE_LABEL[user.role]}</p>
          </div>

          {profile.isLoading ? (
            <div className="space-y-3">{[0, 1, 2].map((i) => <div key={i} className="h-12 animate-pulse rounded-xl bg-slate-100" />)}</div>
          ) : (
            <dl className="space-y-3">
              {info.map(({ icon: Icon, label, value, ltr }) => (
                <div key={label} className="flex items-center gap-3 rounded-xl bg-slate-50 px-4 py-3">
                  <Icon size={18} className="shrink-0 text-teal-600" />
                  <div className="min-w-0">
                    <dt className="text-xs text-slate-400">{label}</dt>
                    <dd className="truncate text-sm font-semibold text-[#0A2540]" dir={ltr ? "ltr" : undefined}>{value || "—"}</dd>
                  </div>
                </div>
              ))}
              {p?.lastLogin && <p className="pt-1 text-xs text-slate-400">Last login: {fmtDateTime(p.lastLogin)}</p>}
            </dl>
          )}
        </section>

        <section className="animate-fade-up rounded-2xl border border-slate-200 bg-white p-6" style={{ animationDelay: "0.08s" }}>
          <div className="mb-5 flex items-center gap-2">
            <KeyRound size={20} className="text-teal-600" />
            <h3 className="font-bold text-[#0A2540]">Change password</h3>
          </div>

          <form onSubmit={handleSubmit((v) => change.mutate(v))} className="space-y-4" noValidate>
            <div className="relative">
              <TextField label="Current password" type={show ? "text" : "password"} dir="ltr" autoComplete="current-password" error={errors.currentPassword?.message} {...register("currentPassword")} />
              <span className="absolute end-3 top-[2.35rem]">{eye}</span>
            </div>
            <TextField label="New password" type={show ? "text" : "password"} dir="ltr" autoComplete="new-password" error={errors.newPassword?.message} {...register("newPassword")} />
            <TextField label="Confirm new password" type={show ? "text" : "password"} dir="ltr" autoComplete="new-password" error={errors.confirmNewPassword?.message} {...register("confirmNewPassword")} />
            <div className="flex justify-end pt-2">
              <SubmitButton loading={change.isPending}>Update password</SubmitButton>
            </div>
          </form>
        </section>
      </div>
    </div>
  );
}