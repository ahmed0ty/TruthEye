namespace TruthEye.DTOs
{
    public class ExamSessionDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; } 
        public int ExamId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public int AttemptNumber { get; set; }
        public double TotalScore { get; set; }
        public int TotalMarks { get; set; }
        public double Percentage { get; set; }
        public bool IsPassed { get; set; }
        public string? ExamTitle { get; set; }
        public string? StudentName { get; set; }
        public List<StudentAnswerDto> CurrentAnswers { get; set; } = new();

    }
}