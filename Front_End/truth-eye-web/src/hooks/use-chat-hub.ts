"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
} from "@microsoft/signalr";
import { ensureFreshToken } from "@/lib/auth/refresh";
import type { ChatMsg } from "@/types/chat";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5210";

export function useChatHub(myId: number | undefined) {
  const qc = useQueryClient();
  const connRef = useRef<HubConnection | null>(null);
  const [typingFrom, setTypingFrom] = useState<string | null>(null);
  const typingTimer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  useEffect(() => {
    if (!myId) return;

    const conn = new HubConnectionBuilder()
      .withUrl(`${BASE_URL}/hubs/chat`, {
        accessTokenFactory: async () => (await ensureFreshToken()) ?? "",
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.None)
      .build();
    connRef.current = conn;

    conn.on(
      "ReceivePrivateMessage",
      (m: { messageId: number; senderId: string; message: string; sentAt: string }) => {
        const senderId = Number(m.senderId);
        const msg: ChatMsg = {
          id: m.messageId,
          messageText: m.message,
          sentAt: m.sentAt,
          isRead: false,
          senderId,
          receiverId: myId,
          isMine: false,
        };
        qc.setQueryData<ChatMsg[]>(["chat", "conv", senderId], (old) =>
          old && !old.some((x) => x.id === msg.id) ? [...old, msg] : old
        );
        qc.invalidateQueries({ queryKey: ["chat", "recent"] });
      }
    );

    conn.on(
      "MessageSent",
      (m: { messageId: number; receiverId: string; message: string; sentAt: string }) => {
        const receiverId = Number(m.receiverId);
        const msg: ChatMsg = {
          id: m.messageId,
          messageText: m.message,
          sentAt: m.sentAt,
          isRead: false,
          senderId: myId,
          receiverId,
          isMine: true,
        };
        qc.setQueryData<ChatMsg[]>(["chat", "conv", receiverId], (old) =>
          old && !old.some((x) => x.id === msg.id) ? [...old, msg] : old
        );
        qc.invalidateQueries({ queryKey: ["chat", "recent"] });
      }
    );

    conn.on("UserTyping", (name: string) => {
      setTypingFrom(name);
      clearTimeout(typingTimer.current);
      typingTimer.current = setTimeout(() => setTypingFrom(null), 2500);
    });

    conn.start().catch(() => {});

    return () => {
      clearTimeout(typingTimer.current);
      connRef.current = null;
      conn.stop().catch(() => {});
    };
  }, [myId, qc]);

  const send = useCallback(async (receiverId: number, text: string) => {
    const c = connRef.current;
    if (!c || c.state !== HubConnectionState.Connected)
      throw new Error("Chat is reconnecting. Try again in a moment.");
    await c.invoke("SendPrivateMessage", String(receiverId), text);
  }, []);

  const typing = useCallback((receiverId: number) => {
    const c = connRef.current;
    if (c?.state === HubConnectionState.Connected)
      c.invoke("Typing", String(receiverId)).catch(() => {});
  }, []);

  return { send, typing, typingFrom };
}