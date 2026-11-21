"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Copy, ExternalLink, ShieldCheck } from "lucide-react";
import { SubmitButton, TextField } from "@/components/ui/form-fields";
import { authApi } from "@/lib/api/auth";

export function TwoFactorCard() {
  const qc = useQueryClient();
  const status = useQuery({ queryKey: ["2fa-status"], queryFn: authApi.twoFactorStatus });
  const enabled = status.data?.enabled ?? false;

  const [setup, setSetup] = useState<{ secret: string; otpAuthUri: string } | null>(null);
  const [recoveryCodes, setRecoveryCodes] = useState<string[] | null>(null);
  const [showDisable, setShowDisable] = useState(false);
  const [code, setCode] = useState("");
  const [password, setPassword] = useState("");

  const refresh = () => qc.invalidateQueries({ queryKey: ["2fa-status"] });

  const begin = useMutation({
    mutationFn: () => authApi.beginTwoFactorSetup(),
    onSuccess: (s) => {
      setSetup(s);
      setCode("");
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const enable = useMutation({
    mutationFn: () => authApi.enableTwoFactor(code),
    onSuccess: (r) => {
      setSetup(null);
      setCode("");
      setRecoveryCodes(r.recoveryCodes);
      refresh();
      toast.success("Two-factor sign-in is on");
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const disable = useMutation({
    mutationFn: () => authApi.disableTwoFactor({ password, code }),
    onSuccess: () => {
      setShowDisable(false);
      setPassword("");
      setCode("");
      refresh();
      toast.success("Two-factor sign-in is off");
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const copy = async (text: string, what: string) => {
    try {
      await navigator.clipboard.writeText(text);
      toast.success(`${what} copied`);
    } catch {
      toast.error("Couldn't copy. Select the text and copy it manually.");
    }
  };

  return (
    <section className="animate-fade-up rounded-2xl border border-slate-200 bg-white p-6">
      <div className="mb-5 flex items-center gap-2">
        <ShieldCheck size={20} className="text-teal-600" />
        <h3 className="font-bold text-[#0A2540]">Two-factor sign-in</h3>
      </div>

      {status.isLoading ? (
        <div className="h-12 animate-pulse rounded-xl bg-slate-100" />
      ) : recoveryCodes ? (
        <div className="space-y-4">
          <p className="text-sm text-slate-600">
            Save these recovery codes somewhere safe. Each one works once if you lose your phone. They won&apos;t be shown again.
          </p>
          <div className="grid grid-cols-2 gap-2" dir="ltr">
            {recoveryCodes.map((c) => (
              <code key={c} className="rounded-lg bg-slate-50 px-3 py-2 text-center font-mono text-sm text-[#0A2540]">{c}</code>
            ))}
          </div>
          <div className="flex flex-wrap justify-end gap-2">
            <button type="button" onClick={() => copy(recoveryCodes.join("\n"), "Recovery codes")} className="inline-flex items-center gap-2 rounded-xl border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50">
              <Copy size={16} /> Copy all
            </button>
            <button type="button" onClick={() => setRecoveryCodes(null)} className="rounded-xl bg-teal-700 px-4 py-2 text-sm font-semibold text-white hover:bg-teal-800">
              I saved them
            </button>
          </div>
        </div>
      ) : enabled ? (
        <div className="space-y-4">
          <p className="text-sm text-slate-600">
            On. Signing in asks for a code from your authenticator app after your password.
          </p>
          {!showDisable ? (
            <div className="flex justify-end">
              <button type="button" onClick={() => setShowDisable(true)} className="rounded-xl border border-red-200 px-4 py-2 text-sm font-semibold text-red-600 hover:bg-red-50">
                Turn off
              </button>
            </div>
          ) : (
            <form onSubmit={(e) => { e.preventDefault(); disable.mutate(); }} className="space-y-4" noValidate>
              <TextField label="Current password" type="password" dir="ltr" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} />
              <TextField label="Code from your app" dir="ltr" autoComplete="one-time-code" value={code} onChange={(e) => setCode(e.target.value)} />
              <div className="flex justify-end gap-2">
                <button type="button" onClick={() => { setShowDisable(false); setPassword(""); setCode(""); }} className="rounded-xl px-4 py-2 text-sm font-semibold text-slate-500 hover:text-slate-800">
                  Cancel
                </button>
                <SubmitButton loading={disable.isPending}>Turn off</SubmitButton>
              </div>
            </form>
          )}
        </div>
      ) : setup ? (
        <div className="space-y-4">
          <p className="text-sm text-slate-600">
            In Google Authenticator or Microsoft Authenticator, add an account with this key, then enter the 6-digit code it shows.
          </p>
          <div className="flex items-center gap-2 rounded-xl bg-slate-50 px-4 py-3">
            <code dir="ltr" className="min-w-0 flex-1 break-all font-mono text-sm text-[#0A2540]">{setup.secret}</code>
            <button type="button" onClick={() => copy(setup.secret, "Key")} aria-label="Copy key" className="text-slate-400 hover:text-teal-700">
              <Copy size={16} />
            </button>
          </div>
          <a href={setup.otpAuthUri} className="inline-flex items-center gap-2 text-sm font-semibold text-teal-700 hover:underline">
            <ExternalLink size={16} /> Open in authenticator app
          </a>
          <form onSubmit={(e) => { e.preventDefault(); enable.mutate(); }} className="space-y-4" noValidate>
            <TextField label="6-digit code" dir="ltr" inputMode="numeric" autoComplete="one-time-code" value={code} onChange={(e) => setCode(e.target.value)} />
            <div className="flex justify-end gap-2">
              <button type="button" onClick={() => { setSetup(null); setCode(""); }} className="rounded-xl px-4 py-2 text-sm font-semibold text-slate-500 hover:text-slate-800">
                Cancel
              </button>
              <SubmitButton loading={enable.isPending}>Turn on</SubmitButton>
            </div>
          </form>
        </div>
      ) : (
        <div className="space-y-4">
          <p className="text-sm text-slate-600">
            Add a second step to sign-in: a code from your phone, after your password.
          </p>
          <div className="flex justify-end">
            <button type="button" onClick={() => begin.mutate()} disabled={begin.isPending} className="rounded-xl bg-teal-700 px-4 py-2 text-sm font-semibold text-white hover:bg-teal-800 disabled:opacity-60">
              Set up
            </button>
          </div>
        </div>
      )}
    </section>
  );
}
