export interface ActiveSession {
  id: number;
  studentId: number;
  examId: number;
  startTime: string;
  status: string;
  examTitle?: string | null;
  studentName?: string | null;
}

export interface AiLog {
  id: number;
  eventType: string;
  timestamp: string;
  confidence: number;
  screenshotPath?: string | null;
  examSessionId: number;
}

export interface LiveAlert {
  key: string;
  sessionId: number;
  examId: number;
  eventType: string;
  confidence: number;
  timestamp: string;
}