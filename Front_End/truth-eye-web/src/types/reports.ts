export interface PerformanceReport {
  id: number;
  score: number;
  aiAlertsCount: number;
  duration: string;
  generatedAt: string;
  examId: number;
  exam?: { title?: string } | null;
}