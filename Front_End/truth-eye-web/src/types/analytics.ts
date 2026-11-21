export interface Bucket { label: string; count: number }
export interface QuestionStat {
  questionId: number; text: string; type: string; maxMarks: number;
  answered: number; skipped: number; avgMarks: number; scoreRate: number;
  difficulty: "Easy" | "Medium" | "Hard";
}
export interface ExamAnalytics {
  examId: number; title: string; students: number; avgPercentage: number; passRate: number;
  highest: number; lowest: number; flagged: number; buckets: Bucket[]; questions: QuestionStat[];
}
export interface CourseStat {
  courseId: number; name: string; exams: number; sessions: number;
  avgPercentage: number; passRate: number; flagged: number;
}
export interface FacultyAnalytics {
  from: string; to: string; totalSessions: number; avgPercentage: number; passRate: number;
  flaggedSessions: number; flaggedRate: number; courses: CourseStat[];
  hours: { hour: number; count: number }[];
  daily: { date: string; sessions: number; flagged: number }[];
}