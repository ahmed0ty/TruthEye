"use client";

import { useState } from "react";
import { useForm, type FieldValues, type Resolver } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { SubmitButton, TextField } from "@/components/ui/form-fields";
import { authApi } from "@/lib/api/auth";
import { useAuth } from "@/lib/auth/store";

const schema = z
  .object({
    currentPassword: z.string().min(1, "Required"),
    newPassword: z
      .string()
      .min(8, "At least 8 characters")
      .regex(/[A-Za-z]/, "Must include a letter")
      .regex(/\d/, "Must include a number"),
    confirmNewPassword: z.string().min(1, "Required"),
  })
  .refine((v) => v.newPassword === v.confirmNewPassword, {
    path: ["confirmNewPassword"],
    message: "Passwords don't match",
  });

export default function ChangePasswordPage() {
  const logout = useAuth((s) => s.logout);
  const [loading, setLoading] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FieldValues>({ resolver: zodResolver(schema) as unknown as Resolver<FieldValues> });

  const err = (k: string) => (errors as Record<string, { message?: string }>)[k]?.message;

  const onSubmit = async (v: FieldValues) => {
    setLoading(true);
    try {
      await authApi.changePassword({
        currentPassword: v.currentPassword,
        newPassword: v.newPassword,
        confirmNewPassword: v.confirmNewPassword,
      });
      toast.success("Password changed. Please sign in again.");
      setTimeout(() => logout(), 800);
    } catch (e) {
      toast.error((e as Error).message);
      setLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 p-4">
      <form
        onSubmit={handleSubmit(onSubmit)}
        noValidate
        className="w-full max-w-md space-y-4 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm"
      >
        <div>
          <h1 className="text-xl font-bold text-slate-900">Change your password</h1>
          <p className="mt-1 text-sm text-slate-500">
            Your account was created by an administrator. Choose your own password to continue.
          </p>
        </div>
        <TextField label="Current password" type="password" dir="ltr" error={err("currentPassword")} {...register("currentPassword")} />
        <TextField label="New password" type="password" dir="ltr" error={err("newPassword")} {...register("newPassword")} />
        <TextField label="Confirm new password" type="password" dir="ltr" error={err("confirmNewPassword")} {...register("confirmNewPassword")} />
        <div className="flex justify-end pt-2">
          <SubmitButton loading={loading}>Change password</SubmitButton>
        </div>
      </form>
    </div>
  );
}