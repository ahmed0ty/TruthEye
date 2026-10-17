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