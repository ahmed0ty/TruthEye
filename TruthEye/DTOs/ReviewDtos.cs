namespace TruthEye.DTOs
{
    public class ReviewFilter
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Decision { get; set; }     // Pending | Cleared | Confirmed
        public string? Level { get; set; }        // Low | Medium | High
        public int? ExamId { get; set; }
        public bool IncludeClean { get; set; }
    }

    public class ReviewRowDto
    {
        public int SessionId { get; set; }
        public string StudentName { get; set; } = "";
        public string ExamTitle { get; set; } = "";
        public string SessionStatus { get; set; } = "";
        public int RiskScore { get; set; }
        public string RiskLevel { get; set; } = "None";
        public int ViolationCount { get; set; }
        public int EvidenceCount { get; set; }
        public string Decision { get; set; } = "Pending";
        public DateTime SubmittedAt { get; set; }
    }

    public class ReviewEventDto
    {
        public int Id { get; set; }
        public string EventType { get; set; } = "";
        public double Confidence { get; set; }
        public int Weight { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Evidence { get; set; }
    }

    public class ReviewDetailDto : ReviewRowDto
    {
        public double Percentage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Note { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public bool CanDecide { get; set; }
        public List<ReviewEventDto> Events { get; set; } = new();
    }

    public class ReviewDecisionDto
    {
        public string Decision { get; set; } = "";   // Cleared | Confirmed
        public string? Note { get; set; }
    }
}