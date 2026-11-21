export interface GradingAnswer {
  answerId: number;
  questionId: number;
  questionText: string;
  questionType: string;
  maxMarks: number;
  marksObtained: number;
  studentAnswer?: string | null;
  selectedOption?: string | null;
  modelAnswer?: string | null;
  aiSimilarity?: number | null;
  aiFeedback?: string | null;
  adjusted: boolean;
  lastReason?: string | null;
}
export interface GradingSession {
  sessionId: number;
  studentName: string;
  examTitle: string;
  totalScore: number;
  totalMarks: number;
  percentage: number;
  isPassed: boolean;
  passingScore: number;
  answers: GradingAnswer[];
}
export interface AppealRow {
  id: number;
  sessionId: number;
  studentName: string;
  examTitle: string;
  percentage: number;
  message: string;
  status: "Pending" | "Accepted" | "Rejected";
  response?: string | null;
  createdAt: string;
  respondedAt?: string | null;
}
export interface StudentAppealItem {
  sessionId: number;
  examTitle: string;
  finishedAt: string;
  percentage: number;
  isPassed: boolean;
  canAppeal: boolean;
  appealDeadline?: string | null;
  appealId?: number | null;
  status?: "Pending" | "Accepted" | "Rejected" | null;
  message?: string | null;
  response?: string | null;
  respondedAt?: string | null;
}