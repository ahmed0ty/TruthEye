export interface DoctorCourse {
  id: number;
  name: string;
  description?: string | null;
}

export interface DoctorExam {
  id: number;
  title: string;
  duration: number;
  maxAttempts?: number;
  startTime: string;
  endTime: string;
  type?: string | null;
  randomizeQuestions?: boolean;
  randomizeOptions?: boolean;
  passingScore?: number;
  totalMarks?: number;
  instructions?: string | null;
  courseId: number;
  courseName?: string | null;
  status?: string | null;
  questionsCount: number;
}

export interface QuestionOption {
  id?: number;
  optionText: string;
  isCorrect: boolean;
}

export interface Question {
  id: number;
  questionText: string;
  questionType: string;
  marks: number;
  examId: number;
  options?: QuestionOption[] | null;
  modelAnswer?: string | null;
  aiThreshold?: number | null;
  evaluationModel?: string | null;
}
export interface StudentResultRow {
  sessionId: number;
  studentId: number;
  studentName: string;
  totalScore: number;
  percentage: number;
  isPassed: boolean;
  correctAnswers: number;
  totalQuestions: number;
  submittedAt: string;
  status?: string;
}

export interface ExamAnalysis {
  examId: number;
  examTitle: string;
  totalStudents: number;
  averageScore: number;
  passRate: number;
}

export interface AnswerDetail {
  questionId: number;
  questionText: string;
  questionType: string;
  marks: number;
  isCorrect: boolean;
  marksObtained: number;
  studentAnswer?: string | null;
  aiSimilarityScore?: number | null;
  aiFeedback?: string | null;
  options?: { id: number; optionText: string; isCorrect: boolean }[] | null;
  correctOptionId?: number | null;
  studentSelectedOptionId?: number | null;
  modelAnswer?: string | null;
}

export interface StudentExamDetails {
  sessionId: number;
  studentName: string;
  totalScore: number;
  percentage: number;
  isPassed: boolean;
  answers: AnswerDetail[];
  submittedAt: string;
}
/**
 * ⚠️ قيم questionType بتتحدد في ExamService.AddQuestionAsync.
 * لو الباك بيتوقع قيم مختلفة (مثلاً "MultipleChoice") عدّلها هنا بس.
 */
export const QUESTION_TYPES = { mcq: "MCQ", written: "Written" } as const;

export const EXAM_TYPES = ["Quiz", "Midterm", "Final", "Practice"];

export const isMcq = (q: Question) =>
  q.questionType?.toLowerCase().includes("mcq") ||
  q.questionType?.toLowerCase().includes("choice") ||
  (q.options?.length ?? 0) > 0;