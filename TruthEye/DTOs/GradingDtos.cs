namespace TruthEye.DTOs
{
    public class GradingAnswerDto
    {
        public int AnswerId { get; set; }
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = "";
        public string QuestionType { get; set; } = "";
        public double MaxMarks { get; set; }
        public double MarksObtained { get; set; }
        public string? StudentAnswer { get; set; }
        public string? SelectedOption { get; set; }
        public string? ModelAnswer { get; set; }
        public double? AiSimilarity { get; set; }
        public string? AiFeedback { get; set; }
        public bool Adjusted { get; set; }
        public string? LastReason { get; set; }
    }

    public class GradingSessionDto
    {
        public int SessionId { get; set; }
        public string StudentName { get; set; } = "";
        public string ExamTitle { get; set; } = "";
        public double TotalScore { get; set; }
        public int TotalMarks { get; set; }
        public double Percentage { get; set; }
        public bool IsPassed { get; set; }
        public int PassingScore { get; set; }
        public List<GradingAnswerDto> Answers { get; set; } = new();
    }

    public class AdjustGradeDto
    {
        public double Marks { get; set; }
        public string Reason { get; set; } = "";
    }

    public class CreateAppealDto
    {
        public int ExamSessionId { get; set; }
        public string Message { get; set; } = "";
    }

    public class AppealResponseDto
    {
        public string Decision { get; set; } = "";   // Accepted | Rejected
        public string Response { get; set; } = "";
    }

    public class AppealRowDto
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string StudentName { get; set; } = "";
        public string ExamTitle { get; set; } = "";
        public double Percentage { get; set; }
        public string Message { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public string? Response { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    public class StudentAppealItemDto
    {
        public int SessionId { get; set; }
        public string ExamTitle { get; set; } = "";
        public DateTime FinishedAt { get; set; }
        public double Percentage { get; set; }
        public bool IsPassed { get; set; }
        public bool CanAppeal { get; set; }
        public DateTime? AppealDeadline { get; set; }
        public int? AppealId { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? Response { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}