namespace TruthEye.DTOs
{
    public class CreateResumeDto { public int ExamSessionId { get; set; } public string Reason { get; set; } = ""; }
    public class ResumeDecisionDto { public string Decision { get; set; } = ""; public string? Note { get; set; } }

    public class ResumeRowDto
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string StudentName { get; set; } = "";
        public string ExamTitle { get; set; } = "";
        public string Reason { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public string? Note { get; set; }
        public DateTime DisconnectedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int SecondsLeftToDecide { get; set; }
    }

    public class ResumeStatusDto
    {
        public bool CanRequest { get; set; }
        public string? Status { get; set; }
        public string? Note { get; set; }
        public string? Why { get; set; }          // سبب عدم الإمكانية
    }
}