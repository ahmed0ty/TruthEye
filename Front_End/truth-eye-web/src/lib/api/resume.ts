import { api } from "./client";

export interface ResumeStatus { canRequest: boolean; status?: string | null; note?: string | null; why?: string | null }
export interface ResumeRow {
  id: number; sessionId: number; studentName: string; examTitle: string; reason: string;
  disconnectedAt: string; createdAt: string; secondsLeftToDecide: number;
}

export const resumeApi = {
  status: (sessionId: number) => api<ResumeStatus>(`/api/Resume/status/${sessionId}`),
  request: (examSessionId: number, reason: string) => api<ResumeStatus>("/api/Resume", { method: "POST", body: { examSessionId, reason } }),
  pending: () => api<ResumeRow[]>("/api/Resume/pending"),
  decide: (id: number, decision: "Approved" | "Rejected", note?: string) =>
    api<ResumeRow>(`/api/Resume/${id}/decision`, { method: "POST", body: { decision, note } }),
};