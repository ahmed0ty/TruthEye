import { api } from "./client";
import type {
  LoginRequest,
  LoginResponse,
  RegisterSuperAdminRequest,
  UserProfile,
} from "@/types/api";

export const authApi = {
  login: (data: LoginRequest) =>
    api<LoginResponse>("/api/Auth/login", { method: "POST", body: data }),

  // امسح الدالتين دول:
logout: (refreshToken: string) =>
  api<boolean>("/api/Auth/logout", { method: "POST", body: { refreshToken } }),

  profile: () => api<UserProfile>("/api/Auth/profile"),

  registerSuperAdmin: (data: RegisterSuperAdminRequest) =>
    api("/api/Auth/register/super-admin", { method: "POST", body: data }),

  changePassword: (d: {
    currentPassword: string;
    newPassword: string;
    confirmNewPassword: string;
  }) => api("/api/Auth/change-password", { method: "POST", body: d }),

  // ---------- Two-factor sign-in ----------
  loginWithTwoFactor: (d: { challengeToken: string; code: string }) =>
    api<LoginResponse>("/api/Auth/login/2fa", { method: "POST", body: d }),

  twoFactorStatus: () => api<{ enabled: boolean }>("/api/Auth/2fa"),

  beginTwoFactorSetup: () =>
    api<{ secret: string; otpAuthUri: string }>("/api/Auth/2fa/setup", { method: "POST" }),

  enableTwoFactor: (code: string) =>
    api<{ recoveryCodes: string[] }>("/api/Auth/2fa/enable", { method: "POST", body: { code } }),

  disableTwoFactor: (d: { password: string; code: string }) =>
    api("/api/Auth/2fa/disable", { method: "POST", body: d }),

  resetTwoFactor: (userId: number) =>
    api(`/api/Auth/users/${userId}/2fa/reset`, { method: "POST" }),

  setupStatus: () =>
    api<{ superAdminExists: boolean; registrationOpen: boolean }>("/api/Auth/setup-status"),
};