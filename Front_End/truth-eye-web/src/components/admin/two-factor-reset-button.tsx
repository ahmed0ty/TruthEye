"use client";

import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { toast } from "sonner";
import { ShieldOff } from "lucide-react";
import { Confirm } from "@/components/ui/confirm";
import { authApi } from "@/lib/api/auth";
import { useAuth } from "@/lib/auth/store";

/** يظهر للعميد ورئيس الجامعة بس. السيرفر هو اللي بيحدد النطاق (الكلية للعميد). */
export function TwoFactorResetButton({ userId, name }: { userId: number; name: string }) {
  const role = useAuth((s) => s.user?.role);
  const [open, setOpen] = useState(false);

  const reset = useMutation({
    mutationFn: () => authApi.resetTwoFactor(userId),
    onSuccess: () => {
      toast.success(`Two-factor sign-in reset for ${name}`);
      setOpen(false);
    },
    onError: (e: Error) => toast.error(e.message),
  });

  if (role !== "SuperAdmin" && role !== "Admin") return null;

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        title="Reset two-factor sign-in"
        className="rounded-lg p-2 text-slate-400 transition hover:bg-slate-100 hover:text-slate-700"
      >
        <ShieldOff size={17} />
      </button>
      <Confirm
        open={open}
        title="Reset two-factor sign-in?"
        text={`${name} will sign in with just a password until they turn two-factor back on.`}
        loading={reset.isPending}
        confirmText="Reset"
        variant="danger"
        onCancel={() => setOpen(false)}
        onConfirm={() => reset.mutate()}
      />
    </>
  );
}
