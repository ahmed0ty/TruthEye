import { api } from "./client";
import type { Paged } from "@/types/admin";
import type { AppealRow, GradingSession, StudentAppealItem } from "@/types/grading";

export const gradingApi = {
  session: (id: number) => api<GradingSession>(`/api/Grading/session/${id}`),
  adjust: (answerId: number, d: { marks: number; reason: string }) =>
    api<GradingSession>(`/api/Grading/answers/${answerId}`, { method: "PUT", body: d }),
};

export const appealApi = {
  mine: () => api<StudentAppealItem[]>("/api/Appeal/mine"),
  create: (d: { examSessionId: number; message: string }) =>
    api<StudentAppealItem>("/api/Appeal", { method: "POST", body: d }),
  list: (q: Record<string, string | number | undefined>) => api<Paged<AppealRow>>("/api/Appeal", { query: q }),
  respond: (id: number, d: { decision: "Accepted" | "Rejected"; response: string }) =>
    api<AppealRow>(`/api/Appeal/${id}/respond`, { method: "POST", body: d }),
};