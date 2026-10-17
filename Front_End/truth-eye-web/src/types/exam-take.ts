export interface TakeOption {
  id: number;
  optionText: string;
}

export interface TakeQuestion {
  id: number;
  questionText: string;
  questionType: string;
  marks: number;
  options?: TakeOption[] | null;
}

export interface SavedAnswer {
  questionId: number;
  answerText?: string | null;
  selectedOptionId?: number | null;
}

export interface ExamTake {
  sessionId: number;
  examId: number;
  examTitle: string;
  instructions?: string | null;
  remainingSeconds: number;
  serverTime: string;
  questions: TakeQuestion[];
  answers: SavedAnswer[];
}

export interface QuestionNav {
  hasNext: boolean;
  currentQuestionNumber: number;
  totalQuestions: number;
}