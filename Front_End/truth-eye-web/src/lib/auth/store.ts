"use client";

import { create } from "zustand";
import { decodeToken, isExpired } from "./jwt";
import { refreshTokens } from "./refresh";
import { clearToken, getRefresh, getToken, saveTokens } from "./storage";
import type { SessionUser } from "@/types/api";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5210";

interface AuthState {
  user: SessionUser | null;
  ready: boolean;
  hydrate: () => void;
  setSession: (token: string, refreshToken: string) => SessionUser | null;
  logout: () => void;
}

export const useAuth = create<AuthState>((set) => ({
  user: null,
  ready: false,

  hydrate: () => {
    const token = getToken();
    const decoded = token ? decodeToken(token) : null;
    if (!decoded || (isExpired(decoded.exp) && !getRefresh())) {
      clearToken();
      set({ user: null, ready: true });
      return;
    }
    const { exp, ...user } = decoded;
    set({ user, ready: true });
    if (isExpired(exp)) refreshTokens(); // renew in the background
  },

  setSession: (token, refreshToken) => {
    const decoded = decodeToken(token);
    if (!decoded) return null;
    saveTokens(token, refreshToken);
    const { exp, ...user } = decoded;
    void exp;
    set({ user, ready: true });
    return user;
  },

  logout: () => {
    const rt = getRefresh();
    if (rt) {
      fetch(`${BASE_URL}/api/Auth/logout`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken: rt }),
        keepalive: true,
      }).catch(() => {});
    }
    clearToken();
    set({ user: null });
    window.location.href = "/login";
  },
}));