"use client";

import { useEffect, useState, type FormEvent } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMutation } from "@tanstack/react-query";
import { toast } from "sonner";
import { api } from "@/lib/api/client";

const input =
  "w-full rounded-xl border border-slate-300 bg-white px-4 py-3 text-sm outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-4 focus:ring-teal-100";

export default function ResetPasswordPage() {
  const router = useRouter();
  const [token, setToken] = useState("");
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    setToken(new URLSearchParams(window.location.search).get("token") ?? "");
  }, []);

  const save = useMutation({
    mutationFn: () =>
      api("/api/Auth/password-reset/confirm", {
        method: "POST",
        body: { token, newPassword: password },
      }),
    onSuccess: () => {
      toast.success("Password updated. Sign in with your new password.");
      router.replace("/login");
    },
    onError: (e: Error) => setError(e.message),
  });

  const submit = (e: FormEvent) => {
    e.preventDefault();
    setError("");
    if (!token) {
      setError("This link is missing its token. Open the link from the email again.");
      return;
    }
    if (password.length < 8) {
      setError("Password must be at least 8 characters.");
      return;
    }
    if (!/[A-Za-z]/.test(password) || !/\d/.test(password)) {
      setError("Password must contain letters and digits.");
      return;
    }
    if (password !== confirm) {
      setError("The passwords don't match.");
      return;
    }
    save.mutate();
  };

  return (
    <main className="flex min-h-screen items-center justify-center bg-[#F6F8FA] p-4">
      <form onSubmit={submit} className="w-full max-w-md space-y-5 rounded-3xl border border-slate-200 bg-white p-8 shadow-xl shadow-slate-200/60">
        <div>
          <h1 className="text-xl font-bold text-[#0A2540]">Choose a new password</h1>
          <p className="mt-1 text-sm text-slate-500">At least 8 characters, with letters and digits.</p>
        </div>

        <input
          type="password"
          autoComplete="new-password"
          placeholder="New password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className={input}
        />
        <input
          type="password"
          autoComplete="new-password"
          placeholder="Confirm new password"
          value={confirm}
          onChange={(e) => setConfirm(e.target.value)}
          className={input}
        />

        {error && <p className="rounded-xl bg-red-50 px-4 py-3 text-sm font-medium text-red-600">{error}</p>}

        <button
          type="submit"
          disabled={save.isPending}
          className="w-full rounded-xl bg-teal-600 py-3 font-semibold text-white transition hover:bg-teal-700 disabled:opacity-60"
        >
          {save.isPending ? "Saving..." : "Save password"}
        </button>

        <Link href="/login" className="block text-center text-sm font-semibold text-teal-700 hover:text-teal-800">
          Back to sign in
        </Link>
      </form>
    </main>
  );
}
