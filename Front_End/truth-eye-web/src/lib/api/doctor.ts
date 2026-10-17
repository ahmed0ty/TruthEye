import { api } from "./client";
import type { DoctorCourse, DoctorExam, ExamAnalysis, Question, StudentExamDetails, StudentResultRow } from "@/types/doctor";
export interface ExamPayload {
  title: string;
  duration: number;
  maxAttempts: number;
  startTime: string;
  endTime: string;
  type?: string;
  randomizeQuestions: boolean;
  randomizeOptions: boolean;
  passingScore: number;
  totalMarks: number;
  instructions?: string;
  courseId: number;
}

export interface QuestionPayload {
  examId: number;
  questionText: string;
  questionType: string;
  marks: number;
  options?: { optionText: string; isCorrect: boolean }[];
  modelAnswer?: string;
  aiThreshold?: number;
  evaluationModel?: string;
}

export const doctorApi = {
  courses: () => api<DoctorCourse[]>("/api/DoctorPortal/my-courses"),
  exams: () => api<DoctorExam[]>("/api/DoctorPortal/my-exams"),
  exam: (id: number) => api<DoctorExam>(`/api/Exam/${id}`),
  createExam: (d: ExamPayload) => api("/api/Exam", { method: "POST", body: d }),
  updateExam: (id: number, d: ExamPayload) => api(`/api/Exam/${id}`, { method: "PUT", body: d }),
  setStatus: (id: number, status: "Active" | "Draft") =>
    api(`/api/Exam/${id}/status`, { method: "PUT", query: { status } }),
  deleteExam: (id: number) => api(`/api/Exam/${id}`, { method: "DELETE" }),

  questions: (examId: number) => api<Question[]>(`/api/Exam/${examId}/questions`),
  addQuestion: (d: QuestionPayload) => api("/api/Exam/questions", { method: "POST", body: d }),
  deleteQuestion: (id: number) => api(`/api/Exam/questions/${id}`, { method: "DELETE" }),
    results: (examId: number) => api<StudentResultRow[]>(`/api/Exam/my-exams/${examId}/results`),
  analysis: (examId: number) => api<ExamAnalysis>(`/api/Exam/my-exams/${examId}/analysis`),
  sessionDetails: (sessionId: number) =>
    api<StudentExamDetails>(`/api/Exam/sessions/${sessionId}/details`),
};