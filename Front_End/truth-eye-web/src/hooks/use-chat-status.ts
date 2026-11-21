"use client";

import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/store";

export function useChatStatus() {
  const user = useAuth((s) => s.user);
  return useQuery({
    queryKey: ["chat-status"],
    queryFn: () => api<{ locked: boolean; until?: string | null }>("/api/Contacts/status"),
    enabled: !!user,
    refetchInterval: 30_000,
    staleTime: 0,
  });
}