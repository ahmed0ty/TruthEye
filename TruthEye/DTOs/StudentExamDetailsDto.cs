namespace TruthEye.DTOs
{
    public class StudentExamDetailsDto
    {
        public int SessionId { get; set; }
        public required string StudentName { get; set; }
        public double TotalScore { get; set; }
        public double Percentage { get; set; }
        public bool IsPassed { get; set; }
        public List<QuestionWithAnswerDto> Answers { get; set; } = new();
        public DateTime SubmittedAt { get; set; }
    }
}
