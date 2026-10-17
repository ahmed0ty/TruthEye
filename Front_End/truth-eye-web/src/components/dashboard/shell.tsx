"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { ChevronDown, Loader2, LogOut, Menu, User as UserIcon, X } from "lucide-react";
import { Logo } from "@/components/logo";
import { NAV } from "@/lib/nav";
import { useAuth } from "@/lib/auth/store";
import { authApi } from "@/lib/api/auth";
import { cn } from "@/lib/utils";
import { ROLE_LABEL } from "@/types/api";
import { Avatar } from "@/components/ui/avatar";
import { NotificationBell } from "@/components/dashboard/notification-bell";
import { ThemeToggle } from "@/components/theme-toggle";

export function DashboardShell({ children }: { children: React.ReactNode }) {
  const { user, ready, logout } = useAuth();
  const profile = useQuery({ queryKey: ["profile"], queryFn: authApi.profile, enabled: !!user });
  const pathname = usePathname();
  const [drawer, setDrawer] = useState(false);
  const [menu, setMenu] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    setDrawer(false);
  }, [pathname]);

  useEffect(() => {
    const close = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) setMenu(false);
    };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, []);

  if (!ready || !user) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-[#F6F8FA]">
        <Loader2 className="animate-spin text-teal-700" size={32} />
      </div>
    );
  }

  const items = NAV[user.role] ?? [];
  const current = items.find((i) => i.href === pathname);

  const sidebar = (
    <div className="relative flex h-full flex-col overflow-hidden bg-[#0A2540] text-slate-300">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-[0.07]"
        style={{
          backgroundImage:
            "linear-gradient(#fff 1px, transparent 1px), linear-gradient(90deg, #fff 1px, transparent 1px)",
          backgroundSize: "44px 44px",
        }}
      />
      <div className="pointer-events-none absolute -top-20 -start-20 h-64 w-64 rounded-full bg-teal-500/20 blur-3xl" />

      <div className="relative flex h-20 items-center justify-between px-6">
        <Logo light />
        <button
          onClick={() => setDrawer(false)}
          className="rounded-lg p-1.5 text-slate-400 hover:bg-white/10 lg:hidden"
          aria-label="Close"
        >
          <X size={20} />
        </button>
      </div>

      <nav className="relative flex-1 space-y-1 overflow-y-auto px-4 py-4">
        {items.map(({ label, href, icon: Icon, soon }) => {
          const active =
            pathname === href ||
            (!soon && href.split("/").length > 2 && pathname.startsWith(href + "/"));
          const base =
            "group flex items-center gap-3 rounded-xl px-4 py-3 text-sm font-medium transition";

          if (soon) {
            return (
              <div key={href} className={cn(base, "cursor-not-allowed text-slate-500")}>
                <Icon size={19} />
                <span className="flex-1">{label}</span>
                <span className="rounded-full bg-white/5 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-slate-400">
                  Soon
                </span>
              </div>
            );
          }
          return (
            <Link
              key={href}
              href={href}
              className={cn(
                base,
                active
                  ? "bg-teal-600 text-white shadow-lg shadow-black/20"
                  : "hover:bg-white/5 hover:text-white"
              )}
            >
              <Icon size={19} className={cn(!active && "transition group-hover:scale-110")} />
              {label}
            </Link>
          );
        })}
      </nav>

      <div className="relative border-t border-white/10 p-4">
        <button
          onClick={logout}
          className="flex w-full items-center gap-3 rounded-xl px-4 py-3 text-sm font-medium text-slate-400 transition hover:bg-red-500/10 hover:text-red-400"
        >
          <LogOut size={19} />
          Sign out
        </button>
      </div>
    </div>
  );

  return (
    <div className="min-h-screen bg-[#F6F8FA]">
      {/* Fixed sidebar for large screens */}
      <aside className="fixed inset-y-0 start-0 z-30 hidden w-72 lg:block">{sidebar}</aside>

      {/* Drawer for mobile */}
      <div className={cn("fixed inset-0 z-40 lg:hidden", drawer ? "visible" : "invisible")}>
        <div
          onClick={() => setDrawer(false)}
          className={cn(
            "absolute inset-0 bg-[#0A2540]/60 backdrop-blur-sm transition-opacity",
            drawer ? "opacity-100" : "opacity-0"
          )}
        />
        <aside
          className={cn(
            "absolute inset-y-0 start-0 w-72 transition-transform duration-300",
            drawer ? "translate-x-0" : "-translate-x-full rtl:translate-x-full"
          )}
        >
          {sidebar}
        </aside>
      </div>

      <div className="lg:ps-72">
        <header className="sticky top-0 z-20 flex h-16 items-center justify-between border-b border-slate-200/70 bg-white/80 px-4 backdrop-blur-xl sm:h-20 sm:px-8">
          <div className="flex items-center gap-3">
            <button
              onClick={() => setDrawer(true)}
              className="rounded-xl border border-slate-200 p-2.5 text-slate-600 hover:bg-slate-50 lg:hidden"
              aria-label="Menu"
            >
              <Menu size={20} />
            </button>
            <h2 className="text-lg font-bold text-[#0A2540]">{current?.label ?? "Home"}</h2>
          </div>

          <div className="flex items-center gap-3">
            <ThemeToggle />
            <NotificationBell />

            <div className="relative" ref={menuRef}>
              <button
                onClick={() => setMenu((v) => !v)}
                className="flex items-center gap-3 rounded-xl border border-slate-200 py-1.5 ps-3 pe-1.5 transition hover:bg-slate-50"
              >
                <ChevronDown size={16} className={cn("text-slate-400 transition", menu && "rotate-180")} />
                <div className="hidden text-start sm:block">
                  <p className="text-sm font-semibold leading-tight text-[#0A2540]">{user.name}</p>
                  <p className="text-xs text-slate-500">{ROLE_LABEL[user.role]}</p>
                </div>
                <Avatar name={user.name} src={profile.data?.profileImage} size={40} className="rounded-lg" />
              </button>

              {menu && (
                <div className="animate-pop absolute end-0 top-14 w-60 rounded-2xl border border-slate-200 bg-white p-2 shadow-xl shadow-slate-200/70">
                  <div className="border-b border-slate-100 px-3 py-3">
                    <p className="truncate text-sm font-semibold text-[#0A2540]">{user.name}</p>
                    <p className="truncate text-xs text-slate-500" dir="ltr">{user.email}</p>
                  </div>
                  <Link
                    href="/profile"
                    onClick={() => setMenu(false)}
                    className="mt-1 flex w-full items-center gap-2 rounded-xl px-3 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50"
                  >
                    <UserIcon size={17} />
                    My profile
                  </Link>
                  <button
                    onClick={logout}
                    className="flex w-full items-center gap-2 rounded-xl px-3 py-2.5 text-sm font-semibold text-red-600 transition hover:bg-red-50"
                  >
                    <LogOut size={17} />
                    Sign out
                  </button>
                </div>
              )}
            </div>
          </div>
        </header>

        <main className="mx-auto max-w-7xl p-4 sm:p-8">{children}</main>
      </div>
    </div>
  );
}
