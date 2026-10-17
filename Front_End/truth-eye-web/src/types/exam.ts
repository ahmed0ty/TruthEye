export interface Exam {
  id: number;
  title: string;
  duration: number; // minutes
  maxAttempts?: number;
  startTime: string;
  endTime: string;
  type?: string | null;
  passingScore?: number;
  totalMarks?: number;
  instructions?: string | null;
  courseId?: number;
  courseName?: string | null;
}

export type SessionStatus = "InProgress" | "Completed" | "TimedOut";

export interface ExamSession {
  id: number;
  studentId: number;
  examId: number;
  startTime: string;
  endTime?: string | null;
  status: SessionStatus;
  attemptNumber: number;
  totalScore: number;
  totalMarks: number;
  percentage: number;
  isPassed: boolean;
  examTitle?: string | null;
}

export interface ExamResult {
  sessionId: number;
  examTitle: string;
  totalScore: number;
  totalMarks: number;
  percentage: number;
  isPassed: boolean;
  correctAnswers: number;
  totalQuestions: number;
  duration: string; // .NET TimeSpan
  aiAlertsCount: number;
  submittedAt: string;
}

export type ExamState = "upcoming" | "open" | "closed";

export function getExamState(exam: Pick<Exam, "startTime" | "endTime">, now = Date.now()): ExamState {
  const start = Date.parse(/([zZ]|[+-]\d{2}:?\d{2})$/.test(exam.startTime) ? exam.startTime : `${exam.startTime}Z`);
  const end = Date.parse(/([zZ]|[+-]\d{2}:?\d{2})$/.test(exam.endTime) ? exam.endTime : `${exam.endTime}Z`);
  if (now < start) return "upcoming";
  if (now > end) return "closed";
  return "open";
}