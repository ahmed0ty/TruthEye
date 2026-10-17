namespace TruthEye.DTOs
{
    public class ExamResultDto
    {
        public int SessionId { get; set; }
        public string ExamTitle { get; set; }
        public double TotalScore { get; set; }
        public int TotalMarks { get; set; }
        public double Percentage { get; set; }
        public bool IsPassed { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public TimeSpan Duration { get; set; }
        public int AIAlertsCount { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}
