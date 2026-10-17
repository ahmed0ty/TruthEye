"use client";

import { useEffect } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { notificationsApi } from "@/lib/api/notifications";
import { ensureFreshToken } from "@/lib/auth/refresh";
import type { AppNotification } from "@/types/notification";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5210";
const KEY = ["notifications"];

export function useNotifications(enabled: boolean) {
  const qc = useQueryClient();

  const query = useQuery({
    queryKey: KEY,
    queryFn: notificationsApi.list,
    enabled,
    refetchInterval: 60_000,
  });

  useEffect(() => {
    if (!enabled) return;

    const conn = new HubConnectionBuilder()
      .withUrl(`${BASE_URL}/hubs/notification`, {
        accessTokenFactory: async () => (await ensureFreshToken()) ?? "",
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.None)
      .build();

    conn.on("ReceiveNotification", (n: Partial<AppNotification>) => {
      toast.info(n.message ?? "New notification");
      qc.invalidateQueries({ queryKey: KEY });
    });

    conn.start().catch(() => {});

    return () => {
      conn.stop().catch(() => {});
    };
  }, [enabled, qc]);

  const markRead = useMutation({
    mutationFn: (id: number) => notificationsApi.markRead(id),
    onMutate: async (id) => {
      await qc.cancelQueries({ queryKey: KEY });
      const prev = qc.getQueryData<AppNotification[]>(KEY);
      qc.setQueryData<AppNotification[]>(KEY, (old) =>
        old?.map((n) => (n.id === id ? { ...n, isRead: true } : n))
      );
      return { prev };
    },
    onError: (_e, _id, ctx) => ctx?.prev && qc.setQueryData(KEY, ctx.prev),
  });

  const list = [...(query.data ?? [])].sort((a, b) => b.id - a.id);
  const unread = list.filter((n) => !n.isRead).length;
  const markAll = () => list.filter((n) => !n.isRead).forEach((n) => markRead.mutate(n.id));

  return { list, unread, isLoading: query.isLoading, markRead: markRead.mutate, markAll };
}