export interface Department {
  id: number;
  name: string;
  description: string;
  facultyId: number;
  facultyName: string;
  universityName: string;
  coursesCount: number;
}

export interface Course {
  id: number;
  name: string;
  description: string;
  departmentId: number;
  departmentName?: string | null;
}

export interface Student {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  nationalId: string;
  phoneNumber?: string | null;
  isActive: boolean;
  createdAt: string;
  departmentId: number;
  departmentName: string;
  year: number;
}

export interface Doctor {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  nationalId: string;
  phoneNumber?: string | null;
  isActive: boolean;
  createdAt: string;
  departmentId: number;
  departmentName: string;
  specialization?: string | null;
  academicDegree: number;
  yearsOfExperience: number;
}

export interface Proctor {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  nationalId: string;
  phoneNumber?: string | null;
  isActive: boolean;
  createdAt: string;
  department?: string | null;
  isOnDuty: boolean;
}
export interface University {
  id: number;
  name: string;
  location: string;
  city: string;
  country: string;
  createdAt: string;
  facultiesCount: number;
}

export interface Faculty {
  id: number;
  name: string;
  universityId: number;
  universityName: string;
  departmentsCount: number;
}

export interface FacultyAdmin {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  nationalId: string;
  phoneNumber?: string | null;
  isActive: boolean;
  createdAt: string;
  universityId: number;
  universityName?: string | null;
  facultyId: number;
  facultyName?: string | null;
}

export interface SupportMember {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  nationalId: string;
  phoneNumber?: string | null;
  isActive: boolean;
  createdAt: string;
  department?: string | null;
  specialization?: string | null;
  shiftTiming?: string | null;
  isOnline: boolean;
  openTickets: number;
}
export interface AiConfig {
  id: number;
  parameterName: string | null;
  value: string | null;
  threshold: number;
  createdAt: string;
  updatedAt?: string | null;
}
export interface StaffMember {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  nationalId: string;
  phoneNumber: string;
  role: "FacultySecretary" | "TechnicalStaff";
  isActive: boolean;
  facultyId: number;
  facultyName: string;
  lastLogin?: string | null;
  createdAt: string;
  canManage: boolean;
}

export interface ActivityLogItem {
  id: number;
  timestamp: string;
  actorId?: number | null;
  actorName: string;
  actorRole?: string | null;
  action: string;
  entityType: string;
  entityId?: number | null;
  description: string;
  oldValues?: string | null;
  newValues?: string | null;
  isReverted: boolean;
  revertedAt?: string | null;
  canRevert: boolean;
}

export interface ActivityLogPage {
  items: ActivityLogItem[];
  total: number;
  page: number;
  pageSize: number;
}