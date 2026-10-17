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
};