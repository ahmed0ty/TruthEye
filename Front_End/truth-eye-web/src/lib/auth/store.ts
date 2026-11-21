"use client";

import { create } from "zustand";
import { decodeToken, isExpired } from "./jwt";
import { refreshTokens } from "./refresh";
import { clearToken, getToken, hasSession, saveTokens } from "./storage";
import type { SessionUser } from "@/types/api";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5210";

interface AuthState {
  user: SessionUser | null;
  ready: boolean;
  hydrate: () => void;
  setSession: (token: string) => SessionUser | null;
  logout: () => void;
}

export const useAuth = create<AuthState>((set) => ({
  user: null,
  ready: false,

  hydrate: () => {
    const token = getToken();
    const decoded = token ? decodeToken(token) : null;
    if (!decoded || (isExpired(decoded.exp) && !hasSession())) {
      clearToken();
      set({ user: null, ready: true });
      return;
    }
    const { exp, ...user } = decoded;
    set({ user, ready: true });
    if (isExpired(exp)) refreshTokens(); // renew in the background
  },

  setSession: (token) => {
    const decoded = decodeToken(token);
    if (!decoded) return null;
    saveTokens(token);
    const { exp, ...user } = decoded;
    void exp;
    set({ user, ready: true });
    return user;
  },

  logout: () => {
    if (hasSession()) {
      fetch(`${BASE_URL}/api/Auth/logout`, {
        method: "POST",
        credentials: "include",
        headers: { "X-Requested-With": "fetch" },
        keepalive: true,
      }).catch(() => {});
    }
    clearToken();
    set({ user: null });
    window.location.href = "/login";
  },
}));