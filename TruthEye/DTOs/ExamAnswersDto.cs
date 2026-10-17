namespace TruthEye.DTOs
{
    public class ExamAnswersDto
    {
        public int ExamId { get; set; }
        public required string ExamTitle { get; set; }
        public bool CanViewAnswers { get; set; }
        public string? Message { get; set; }
        public List<QuestionWithAnswerDto>? Questions { get; set; }

        public double StudentScore { get; set; }
        public int TotalMarks { get; set; }
        public double Percentage { get; set; }
        public bool IsPassed { get; set; }
    }
}