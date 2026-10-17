"use client";

import { useEffect, useRef, useState } from "react";
import { Bell, BellOff, CheckCheck } from "lucide-react";
import { useNotifications } from "@/hooks/use-notifications";
import { fmtDateTime } from "@/lib/format";
import { cn } from "@/lib/utils";

export function NotificationBell() {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const { list, unread, isLoading, markRead, markAll } = useNotifications(true);

  useEffect(() => {
    const close = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, []);

  return (
    <div className="relative" ref={ref}>
      <button
        onClick={() => setOpen((v) => !v)}
        className="relative rounded-xl border border-slate-200 p-2.5 text-slate-500 transition hover:border-teal-200 hover:bg-teal-50 hover:text-teal-700"
        aria-label="Notifications"
      >
        <Bell size={19} />
        {unread > 0 && (
          <span className="absolute -end-1.5 -top-1.5 flex h-5 min-w-5 items-center justify-center rounded-full bg-red-500 px-1 text-[10px] font-bold text-white">
            {unread > 9 ? "9+" : unread}
          </span>
        )}
      </button>

      {open && (
        <div className="animate-pop absolute end-0 top-14 z-40 w-80 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-xl shadow-slate-200/70 sm:w-96">
          <div className="flex items-center justify-between border-b border-slate-100 px-4 py-3">
            <h3 className="font-bold text-[#0A2540]">Notifications</h3>
            {unread > 0 && (
              <button onClick={markAll} className="flex items-center gap-1 text-xs font-semibold text-teal-700 hover:text-teal-800">
                <CheckCheck size={14} /> Mark all read
              </button>
            )}
          </div>

          {isLoading ? (
            <div className="space-y-2 p-4">{[0, 1, 2].map((i) => <div key={i} className="h-12 animate-pulse rounded-xl bg-slate-100" />)}</div>
          ) : list.length === 0 ? (
            <div className="flex flex-col items-center px-4 py-10 text-center">
              <BellOff size={26} className="mb-2 text-slate-300" />
              <p className="text-sm text-slate-400">You’re all caught up.</p>
            </div>
          ) : (
            <ul className="max-h-96 divide-y divide-slate-100 overflow-y-auto">
              {list.map((n) => {
                const when = n.createdAt ?? n.timestamp;
                return (
                  <li key={n.id}>
                    <button
                      onClick={() => !n.isRead && markRead(n.id)}
                      className={cn("flex w-full items-start gap-3 px-4 py-3 text-start transition hover:bg-slate-50", !n.isRead && "bg-teal-50/50")}
                    >
                      <span className={cn("mt-1.5 h-2 w-2 shrink-0 rounded-full", n.isRead ? "bg-transparent" : "bg-teal-500")} />
                      <div className="min-w-0">
                        <p className={cn("text-sm", n.isRead ? "text-slate-600" : "font-semibold text-[#0A2540]")}>{n.message}</p>
                        {when && !when.startsWith("0001") && <p className="mt-0.5 text-xs text-slate-400">{fmtDateTime(when)}</p>}
                      </div>
                    </button>
                  </li>
                );
              })}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}