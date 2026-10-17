import { api } from "./client";
import type { ActiveSession, AiLog } from "@/types/proctor";

export const proctorApi = {
  active: () => api<ActiveSession[]>("/api/ExamSession/active"),
  sessionLogs: (sessionId: number) => api<AiLog[]>(`/api/AILog/session/${sessionId}`),
  flagged: (threshold = 0.7) => api<AiLog[]>("/api/AILog/flagged", { query: { threshold } }),
};