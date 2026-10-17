"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { ArrowLeft, MessageSquare, RefreshCw, Search, Send } from "lucide-react";
import { chatApi } from "@/lib/api/chat";
import { useAuth } from "@/lib/auth/store";
import { useChatHub } from "@/hooks/use-chat-hub";
import { fmtDateTime, parseDate } from "@/lib/format";
import { cn } from "@/lib/utils";
import type { Contact } from "@/types/chat";
import { Avatar } from "@/components/ui/avatar";

function time(v: string) {
  const d = parseDate(v);
  return d ? d.toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit" }) : "";
}

export default function ChatPage() {
  const user = useAuth((s) => s.user);
  const qc = useQueryClient();
  const hub = useChatHub(user?.id);

  const [activeId, setActiveId] = useState<number | null>(null);
  const [search, setSearch] = useState("");
  const [text, setText] = useState("");
  const [sending, setSending] = useState(false);
  const bottomRef = useRef<HTMLDivElement>(null);
  const lastTyping = useRef(0);

  const contacts = useQuery({ queryKey: ["chat", "contacts"], queryFn: chatApi.contacts });
  const recent = useQuery({ queryKey: ["chat", "recent"], queryFn: chatApi.recent, refetchInterval: 30000 });
  const conv = useQuery({
    queryKey: ["chat", "conv", activeId],
    queryFn: () => chatApi.conversation(activeId!),
    enabled: activeId != null,
    staleTime: Infinity,
  });

  const byId = useMemo(() => new Map((contacts.data ?? []).map((c) => [c.id, c])), [contacts.data]);
  const recentById = useMemo(() => new Map((recent.data ?? []).map((r) => [r.userId, r])), [recent.data]);

  // Sidebar list: contacts, those with recent messages first
  const list = useMemo(() => {
    const q = search.trim().toLowerCase();
    return [...(contacts.data ?? [])]
      .filter((c) => !q || c.name.toLowerCase().includes(q))
      .sort((a, b) => {
        const ta = recentById.get(a.id)?.lastMessage.sentAt ?? "";
        const tb = recentById.get(b.id)?.lastMessage.sentAt ?? "";
        return tb.localeCompare(ta) || a.name.localeCompare(b.name);
      });
  }, [contacts.data, recentById, search]);

  const active: Contact | undefined = activeId != null ? byId.get(activeId) : undefined;
  const messages = conv.data ?? [];

  // Mark as read when opening / receiving
  useEffect(() => {
    if (activeId == null) return;
    const unread = messages.some((m) => !m.isMine && !m.isRead);
    if (unread || recentById.get(activeId)?.unreadCount) {
      chatApi.markRead(activeId).then(() => qc.invalidateQueries({ queryKey: ["chat", "recent"] })).catch(() => {});
    }
  }, [activeId, messages.length]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages.length, activeId]);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const body = text.trim();
    if (!body || activeId == null || sending) return;
    setSending(true);
    try {
      await hub.send(activeId, body);
      setText("");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Couldn’t send the message");
    } finally {
      setSending(false);
    }
  };

  const onType = (v: string) => {
    setText(v);
    if (activeId != null && Date.now() - lastTyping.current > 1500) {
      lastTyping.current = Date.now();
      hub.typing(activeId);
    }
  };

  return (
    <div className="animate-fade-up flex h-[calc(100dvh-9rem)] min-h-[480px] overflow-hidden rounded-2xl border border-slate-200 bg-white">
      {/* Contacts */}
      <aside className={cn("flex w-full flex-col border-slate-200 md:w-80 md:border-e", activeId != null && "hidden md:flex")}>
        <div className="border-b border-slate-100 p-4">
          <h2 className="mb-3 font-bold text-[#0A2540]">Messages</h2>
          <div className="relative">
            <Search size={16} className="pointer-events-none absolute inset-y-0 start-3 my-auto text-slate-400" />
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search people..."
              className="w-full rounded-xl border border-slate-200 bg-slate-50 py-2 ps-9 pe-3 text-sm outline-none focus:border-teal-600 focus:bg-white focus:ring-4 focus:ring-teal-100"
            />
          </div>
        </div>

        <ul className="flex-1 overflow-y-auto">
          {contacts.isLoading ? (
            [0, 1, 2, 3].map((i) => <li key={i} className="m-3 h-14 animate-pulse rounded-xl bg-slate-100" />)
          ) : contacts.isError ? (
            <li className="p-6 text-center">
              <p className="text-sm text-red-500">{(contacts.error as Error).message}</p>
              <button
                onClick={() => contacts.refetch()}
                className="mt-3 inline-flex items-center gap-1.5 rounded-lg bg-teal-600 px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-teal-700"
              >
                <RefreshCw size={13} /> Try again
              </button>
            </li>
          ) : list.length === 0 ? (
            <li className="p-8 text-center text-sm text-slate-400">
              {search ? "No one matches your search." : "No contacts yet. You’ll see people from your courses here."}
            </li>
          ) : (
            list.map((c) => {
              const r = recentById.get(c.id);
              return (
                <li key={c.id}>
                  <button
                    onClick={() => setActiveId(c.id)}
                    className={cn(
                      "flex w-full items-center gap-3 px-4 py-3 text-start transition hover:bg-slate-50",
                      activeId === c.id && "bg-teal-50"
                    )}
                  >
                    <Avatar name={c.name} src={c.profileImage} size={44} />
                    <span className="min-w-0 flex-1">
                      <span className="flex items-center justify-between gap-2">
                        <span className="truncate text-sm font-semibold text-[#0A2540]">{c.name}</span>
                        {r && <span className="shrink-0 text-[11px] text-slate-400">{time(r.lastMessage.sentAt)}</span>}
                      </span>
                      <span className="flex items-center justify-between gap-2">
                        <span className="truncate text-xs text-slate-500">{r ? r.lastMessage.messageText : c.role}</span>
                        {!!r?.unreadCount && (
                          <span className="flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full bg-teal-600 px-1 text-[10px] font-bold text-white">
                            {r.unreadCount}
                          </span>
                        )}
                      </span>
                    </span>
                  </button>
                </li>
              );
            })
          )}
        </ul>
      </aside>

      {/* Conversation */}
      <section className={cn("min-w-0 flex-1 flex-col", activeId == null ? "hidden md:flex" : "flex")}>
        {active ? (
          <>
            <header className="flex items-center gap-3 border-b border-slate-100 px-4 py-3">
              <button onClick={() => setActiveId(null)} className="rounded-lg p-2 text-slate-500 hover:bg-slate-100 md:hidden" aria-label="Back">
                <ArrowLeft size={18} />
              </button>
              <Avatar name={active.name} src={active.profileImage} size={40} />
              <div className="min-w-0">
                <p className="truncate font-bold text-[#0A2540]">{active.name}</p>
                <p className="h-4 text-xs text-teal-600">{hub.typingFrom ? "typing..." : <span className="text-slate-400">{active.role}</span>}</p>
              </div>
            </header>

            <div className="flex-1 space-y-2 overflow-y-auto bg-[#F6F8FA] p-4">
              {conv.isLoading ? (
                <div className="h-16 animate-pulse rounded-2xl bg-slate-100" />
              ) : messages.length === 0 ? (
                <p className="py-16 text-center text-sm text-slate-400">No messages yet. Say hello 👋</p>
              ) : (
                messages.map((m) => (
                  <div key={m.id} className={cn("flex", m.isMine ? "justify-end" : "justify-start")}>
                    <div
                      title={fmtDateTime(m.sentAt)}
                      className={cn(
                        "max-w-[78%] rounded-2xl px-4 py-2 text-sm",
                        m.isMine ? "rounded-ee-md bg-teal-600 text-white" : "rounded-es-md bg-white text-slate-800 shadow-sm"
                      )}
                    >
                      <p className="whitespace-pre-wrap break-words">{m.messageText}</p>
                      <p className={cn("mt-1 text-end text-[10px]", m.isMine ? "text-teal-100" : "text-slate-400")}>{time(m.sentAt)}</p>
                    </div>
                  </div>
                ))
              )}
              <div ref={bottomRef} />
            </div>

            <form onSubmit={submit} className="flex items-center gap-2 border-t border-slate-100 p-3">
              <input
                value={text}
                onChange={(e) => onType(e.target.value)}
                placeholder="Type a message..."
                maxLength={1000}
                className="min-w-0 flex-1 rounded-xl border border-slate-300 px-4 py-2.5 text-sm outline-none focus:border-teal-600 focus:ring-4 focus:ring-teal-100"
              />
              <button
                disabled={!text.trim() || sending}
                className="flex h-11 w-11 items-center justify-center rounded-xl bg-teal-600 text-white transition hover:bg-teal-700 disabled:opacity-40"
                aria-label="Send"
              >
                <Send size={18} />
              </button>
            </form>
          </>
        ) : (
          <div className="flex flex-1 flex-col items-center justify-center text-center">
            <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-slate-100 text-slate-400">
              <MessageSquare size={30} />
            </div>
            <p className="font-bold text-[#0A2540]">Select a conversation</p>
            <p className="mt-1 text-sm text-slate-500">Choose someone from the list to start chatting.</p>
          </div>
        )}
      </section>
    </div>
  );
}