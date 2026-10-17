namespace TruthEye.Entities
{
    public class Answer
    {
        public int Id { get; set; }
        public int ExamSessionId { get; set; }
        public int QuestionId { get; set; }
        public required string AnswerText { get; set; }
        public int? SelectedOptionId { get; set; }
        public bool IsCorrect { get; set; }
        public double MarksObtained { get; set; }
        public DateTime AnsweredAt { get; set; }

        public double? AISimilarityScore { get; set; }
        public string? AIFeedback { get; set; }

        public virtual  ExamSession ExamSession { get; set; }
        public virtual Question Question { get; set; }
        public virtual MCQOption? SelectedOption { get; set; }
    }
}