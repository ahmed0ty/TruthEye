import { api } from "./client";
import type {
  Course, Department, Doctor, FacultyAdmin, Faculty, Proctor, Student, SupportMember,
  University, AiConfig, StaffMember, ActivityLogPage,
} from "@/types/admin";
import type { PersonRow } from "@/types/api";

export interface BulkEnrollRequest {
  studentIds?: number[];
  identifiers?: string[];
  departmentId?: number;
  year?: number;
}

export interface BulkEnrollResult {
  matched: number;
  added: number;
  alreadyEnrolled: number;
  notFound: string[];
}
const list = <T = unknown>(path: string) => api<T[]>(path);

export const adminApi = {
  // قوائم للإحصائيات (الداشبورد)
  students: () => list<PersonRow>("/api/Auth/students"),
  doctors: () => list<PersonRow>("/api/Auth/doctors"),
  proctors: () => list<PersonRow>("/api/Auth/proctors"),
  admins: () => list<PersonRow>("/api/Auth/admins"),
  supports: () => list<PersonRow>("/api/Auth/supports"),
  universities: () => list("/api/University"),
  faculties: () => list("/api/Faculty"),
  aiConfigs: () => list<AiConfig>("/api/Admin/ai-config"),
  saveAiConfig: (d: { parameterName: string; value: string; threshold: number }) =>
    api("/api/Admin/ai-config", { method: "POST", body: d }),

  // الأقسام
  departments: () => list<Department>("/api/Department"),
  createDepartment: (d: { name: string; description?: string; facultyId: number }) =>
    api<Department>("/api/Department", { method: "POST", body: d }),
  updateDepartment: (id: number, d: { name: string; description?: string; facultyId: number }) =>
    api<Department>(`/api/Department/${id}`, { method: "PUT", body: d }),
  deleteDepartment: (id: number) => api(`/api/Department/${id}`, { method: "DELETE" }),

  // المقررات
  courses: () => list<Course>("/api/Courses"),
  createCourse: (d: { name: string; description: string; departmentId: number }) =>
    api<Course>("/api/Courses", { method: "POST", body: d }),
  updateCourse: (id: number, d: { name: string; description: string; departmentId: number }) =>
    api<Course>(`/api/Courses/${id}`, { method: "PUT", body: d }),
  deleteCourse: (id: number) => api(`/api/Courses/${id}`, { method: "DELETE" }),
  courseDoctors: (id: number) => list<Doctor>(`/api/Courses/${id}/doctors`),
  courseStudents: (id: number) => list<Student>(`/api/Courses/${id}/students`),
  assignDoctor: (c: number, d: number) => api(`/api/Courses/${c}/doctors/${d}`, { method: "POST" }),
  removeDoctor: (c: number, d: number) => api(`/api/Courses/${c}/doctors/${d}`, { method: "DELETE" }),
  assignStudent: (c: number, s: number) => api(`/api/Courses/${c}/students/${s}`, { method: "POST" }),
  removeStudent: (c: number, s: number) => api(`/api/Courses/${c}/students/${s}`, { method: "DELETE" }),
  enrollStudents: (c: number, d: BulkEnrollRequest) =>
    api<BulkEnrollResult>(`/api/Courses/${c}/students/bulk`, { method: "POST", body: d }),
  removeStudents: (c: number, ids: number[]) =>
    api<{ removed: number }>(`/api/Courses/${c}/students/bulk-remove`, { method: "POST", body: ids }),

  // الطلاب
  studentList: () => list<Student>("/api/Auth/students?includeDisabled=true"),
  createStudent: (d: Record<string, unknown>) => api("/api/Auth/register/student", { method: "POST", body: d }),
  updateStudent: (id: number, d: Record<string, unknown>) => api(`/api/Auth/students/${id}`, { method: "PUT", body: d }),
  deleteStudent: (id: number) => api(`/api/Auth/students/${id}`, { method: "DELETE" }),

  // الدكاترة
  doctorList: () => list<Doctor>("/api/Auth/doctors?includeDisabled=true"),
  createDoctor: (d: Record<string, unknown>) => api("/api/Auth/register/doctor", { method: "POST", body: d }),
  updateDoctor: (id: number, d: Record<string, unknown>) => api(`/api/Auth/doctors/${id}`, { method: "PUT", body: d }),
  deleteDoctor: (id: number) => api(`/api/Auth/doctors/${id}`, { method: "DELETE" }),

  // المراقبون
  proctorList: () => list<Proctor>("/api/Auth/proctors?includeDisabled=true"),
  createProctor: (d: Record<string, unknown>) => api("/api/Auth/register/proctor", { method: "POST", body: d }),
  updateProctor: (id: number, d: Record<string, unknown>) => api(`/api/Auth/proctors/${id}`, { method: "PUT", body: d }),
  deleteProctor: (id: number) => api(`/api/Auth/proctors/${id}`, { method: "DELETE" }),

  // الجامعات
  universityList: () => list<University>("/api/University"),
  createUniversity: (d: Record<string, unknown>) => api("/api/University", { method: "POST", body: d }),
  updateUniversity: (id: number, d: Record<string, unknown>) => api(`/api/University/${id}`, { method: "PUT", body: d }),
  deleteUniversity: (id: number) => api(`/api/University/${id}`, { method: "DELETE" }),

  // الكليات
  facultyList: () => list<Faculty>("/api/Faculty"),
  createFaculty: (d: Record<string, unknown>) => api("/api/Faculty", { method: "POST", body: d }),
  updateFaculty: (id: number, d: Record<string, unknown>) => api(`/api/Faculty/${id}`, { method: "PUT", body: d }),
  deleteFaculty: (id: number) => api(`/api/Faculty/${id}`, { method: "DELETE" }),

  // مديرو الكليات
  adminList: () => list<FacultyAdmin>("/api/Auth/admins"),
  createAdmin: (d: Record<string, unknown>) => api("/api/Auth/register/admin", { method: "POST", body: d }),
  updateAdmin: (id: number, d: Record<string, unknown>) => api(`/api/Auth/admins/${id}`, { method: "PUT", body: d }),
  deleteAdmin: (id: number) => api(`/api/Auth/admins/${id}`, { method: "DELETE" }),

  // فريق الدعم
  supportList: () => list<SupportMember>("/api/Auth/supports"),
  createSupport: (d: Record<string, unknown>) => api("/api/Auth/register/support", { method: "POST", body: d }),
  updateSupport: (id: number, d: Record<string, unknown>) => api(`/api/Auth/supports/${id}`, { method: "PUT", body: d }),
  deleteSupport: (id: number) => api(`/api/Auth/supports/${id}`, { method: "DELETE" }),

  // Staff (سكرتارية + تيم فني)
  staffList: () => api<StaffMember[]>("/api/Staff?includeDisabled=true"),
  createStaff: (d: Record<string, unknown>) => api<StaffMember>("/api/Staff", { method: "POST", body: d }),
  updateStaff: (id: number, d: Record<string, unknown>) => api<StaffMember>(`/api/Staff/${id}`, { method: "PUT", body: d }),
  disableStaff: (id: number) => api<boolean>(`/api/Staff/${id}/disable`, { method: "POST" }),
  enableStaff: (id: number) => api<boolean>(`/api/Staff/${id}/enable`, { method: "POST" }),
  deleteStaff: (id: number) => api<boolean>(`/api/Staff/${id}`, { method: "DELETE" }),

  // تعطيل / تفعيل الأعضاء
  disableStudent: (id: number) => api<boolean>(`/api/Auth/students/${id}/disable`, { method: "POST" }),
  enableStudent: (id: number) => api<boolean>(`/api/Auth/students/${id}/enable`, { method: "POST" }),
  disableDoctor: (id: number) => api<boolean>(`/api/Auth/doctors/${id}/disable`, { method: "POST" }),
  enableDoctor: (id: number) => api<boolean>(`/api/Auth/doctors/${id}/enable`, { method: "POST" }),
  disableProctor: (id: number) => api<boolean>(`/api/Auth/proctors/${id}/disable`, { method: "POST" }),
  enableProctor: (id: number) => api<boolean>(`/api/Auth/proctors/${id}/enable`, { method: "POST" }),

  // سجل النشاط
  activityLog: (q: Record<string, string | number | undefined>) => {
    const p = new URLSearchParams();
    Object.entries(q).forEach(([k, v]) => v !== undefined && v !== "" && p.set(k, String(v)));
    return api<ActivityLogPage>(`/api/ActivityLog?${p}`);
  },
  revertLog: (id: number) => api<string>(`/api/ActivityLog/${id}/revert`, { method: "POST" }),
};