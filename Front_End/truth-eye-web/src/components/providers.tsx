"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { Toaster } from "sonner";
import { useAuth } from "@/lib/auth/store";
import { api } from "@/lib/api/client";

export function Providers({ children }: { children: React.ReactNode }) {
  const [client] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: { staleTime: 30_000, retry: 1, refetchOnWindowFocus: false },
        },
      })
  );
  const hydrate = useAuth((s) => s.hydrate);
  const user = useAuth((s) => s.user);

  useEffect(() => hydrate(), [hydrate]);

  // نبض عام كل 45 ثانية عشان الجلسة تفضل "حية" حتى لو مفيش طلبات تانية
  useEffect(() => {
    if (!user) return;
    const t = setInterval(() => {
      api("/api/Auth/profile").catch(() => {});
    }, 45_000);
    return () => clearInterval(t);
  }, [user]);

  return (
    <QueryClientProvider client={client}>
      {children}
      <Toaster richColors position="top-center" />
    </QueryClientProvider>
  );
}