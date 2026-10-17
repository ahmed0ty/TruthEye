"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
} from "@microsoft/signalr";
import { ensureFreshToken } from "@/lib/auth/refresh";
import { metaOf } from "./events";
import type { LiveAlert } from "@/types/proctor";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5210";

export type HubStatus = "connecting" | "live" | "offline";

/** examIds must be a memoized array (useMemo) so we only re-join when it really changes. */
export function useLiveMonitor(examIds: number[]) {
  const [status, setStatus] = useState<HubStatus>("connecting");
  const [alerts, setAlerts] = useState<LiveAlert[]>([]);
  const connRef = useRef<HubConnection | null>(null);
  const joined = useRef(new Set<number>());
  const idsRef = useRef<number[]>([]);

  const joinAll = useCallback(async () => {
    const c = connRef.current;
    if (!c || c.state !== HubConnectionState.Connected) return;
    for (const id of idsRef.current) {
      if (joined.current.has(id)) continue;
      try {
        await c.invoke("MonitorExam", String(id));
        joined.current.add(id);
      } catch {
        /* retried on next change / reconnect */
      }
    }
  }, []);

  useEffect(() => {
    idsRef.current = examIds;
    joinAll();
  }, [examIds, joinAll]);

  useEffect(() => {
    let cancelled = false;

    const conn = new HubConnectionBuilder()
      .withUrl(`${BASE_URL}/hubs/exam-monitoring`, {
        accessTokenFactory: async () => (await ensureFreshToken()) ?? "",
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.None)
      .build();
    connRef.current = conn;

    conn.on("AIAlertReceived", (a: Record<string, unknown>) => {
      const alert: LiveAlert = {
        key: crypto.randomUUID(),
        sessionId: Number(a.sessionId),
        examId: Number(a.examId),
        eventType: String(a.eventType),
        confidence: Number(a.confidence),
        timestamp: String(a.timestamp),
      };
      setAlerts((prev) => [alert, ...prev].slice(0, 200));
      if (metaOf(alert.eventType).severity === "high") {
        toast.error(`${metaOf(alert.eventType).label} (session #${alert.sessionId})`);
      }
    });

    conn.onreconnecting(() => !cancelled && setStatus("connecting"));
    conn.onreconnected(() => {
      if (cancelled) return;
      joined.current.clear();
      setStatus("live");
      joinAll();
    });
    conn.onclose(() => !cancelled && setStatus("offline"));

    conn
      .start()
      .then(() => {
        if (cancelled) return;
        setStatus("live");
        joinAll();
      })
      .catch(() => !cancelled && setStatus("offline"));

    return () => {
      cancelled = true;
      connRef.current = null;
      joined.current.clear();
      conn.stop().catch(() => {});
    };
  }, [joinAll]);

  const clear = useCallback(() => setAlerts([]), []);

  return { status, alerts, clear };
}