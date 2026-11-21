import { api } from "./client";
import type { ExamAnalytics, FacultyAnalytics } from "@/types/analytics";

export const analyticsApi = {
  exam: (id: number) => api<ExamAnalytics>(`/api/Analytics/exam/${id}`),
  faculty: (q: { from?: string; to?: string }) => api<FacultyAnalytics>("/api/Analytics/faculty", { query: q }),
};