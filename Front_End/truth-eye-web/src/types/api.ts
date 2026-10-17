export type Role =
  | "SuperAdmin"
  | "Admin"
  | "Doctor"
  | "Student"
  | "Proctor"
  | "Support";

export interface LoginRequest {
  email?: string;
  nationalId?: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
}

export interface UserProfile {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  nationalId: string;
  phoneNumber?: string | null;
  profileImage?: string | null;
  role?: string | null;
  userType?: string | null;
  isActive: boolean;
  lastLogin?: string | null;
}

export interface SessionUser {
  id: number;
  email: string;
  name: string;
  role: Role;
}

export const ROLE_HOME: Record<Role, string> = {
  SuperAdmin: "/admin",
  Admin: "/admin",
  Doctor: "/doctor",
  Student: "/student",
  Proctor: "/proctor",
  Support: "/support",
};

export interface RegisterSuperAdminRequest {
  firstName: string;
  lastName: string;
  email: string;
  nationalId: string;
  phoneNumber: string;
  password: string;
}

export interface PersonRow {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  createdAt: string;
  isActive: boolean;
  departmentName?: string | null;
  facultyName?: string | null;
}

export const ROLE_LABEL: Record<Role, string> = {
  SuperAdmin: "System Admin",
  Admin: "Faculty Admin",
  Doctor: "Doctor",
  Student: "Student",
  Proctor: "Proctor",
  Support: "Support",
};