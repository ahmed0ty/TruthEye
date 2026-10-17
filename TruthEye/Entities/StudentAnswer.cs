namespace TruthEye.Entities
{
    public class StudentAnswer
    {
        public int Id { get; set; }
        public int ExamAttemptId { get; set; }
        public int QuestionId { get; set; }
        public string AnswerText { get; set; } 
        public int? SelectedOptionId { get; set; }
        public DateTime AnsweredAt { get; set; }
        public bool IsReviewed { get; set; }
        public decimal? PointsAwarded { get; set; }
        public string AiFeedback { get; set; }

        public ExamAttempt ExamAttempt { get; set; }
        public Question Question { get; set; }
        public AnswerOption SelectedOption { get; set; }
    }
}
