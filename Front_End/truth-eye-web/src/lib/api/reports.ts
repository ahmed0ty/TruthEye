import { api } from "./client";
import type { PerformanceReport } from "@/types/report";

export const reportsApi = {
  mine: () => api<PerformanceReport[]>("/api/Report/student"),
  generate: (studentId: number, examId: number) =>
    api<PerformanceReport>("/api/Report/generate", { method: "POST", query: { studentId, examId } }),
};