import { api } from "./client";
import type { ExamTake, QuestionNav } from "@/types/exam-take";

export const examTakeApi = {
  take: (sessionId: number) => api<ExamTake>(`/api/ExamSession/${sessionId}/take`),

  // answerText must always be a string: the backend rejects null for it
  next: (
    sessionId: number,
    currentQuestionId: number,
    answer: { questionId: number; answerText: string; selectedOptionId: number | null }
  ) =>
    api<QuestionNav>(`/api/ExamSession/${sessionId}/next`, {
      method: "POST",
      body: { currentQuestionId, answer },
    }),

  submit: (sessionId: number) => api(`/api/ExamSession/${sessionId}/submit`, { method: "POST" }),

  logEvent: (sessionId: number, eventType: string, confidence: number) =>
    api("/api/AILog", { method: "POST", query: { sessionId, eventType, confidence } }),
};