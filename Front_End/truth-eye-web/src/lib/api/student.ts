import { api } from "./client";
import type { Exam, ExamResult, ExamSession } from "@/types/exam";

export const studentApi = {
  availableExams: () => api<Exam[]>("/api/Exam/available"),
  history: () => api<ExamSession[]>("/api/ExamSession/history"),
  start: (examId: number) =>
    api<ExamSession>("/api/ExamSession/start", { method: "POST", body: { examId } }),
  result: (sessionId: number) => api<ExamResult>(`/api/ExamSession/${sessionId}/result`),
};