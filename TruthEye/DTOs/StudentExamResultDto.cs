namespace TruthEye.DTOs
{
    public class StudentExamResultDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public double TotalScore { get; set; }
        public double Percentage { get; set; }
        public bool IsPassed { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime SubmittedAt { get; set; }
        public int SessionId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
