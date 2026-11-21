namespace TruthEye.Entities
{
    public class ResumeRequest
    {
        public int Id { get; set; }
        public int ExamSessionId { get; set; }
        public int StudentId { get; set; }
        public string Reason { get; set; } = "";          // ما يكتبه الطالب
        public string Status { get; set; } = "Pending";   // Pending | Approved | Rejected
        public string? Note { get; set; }                 // رد المراجع
        public DateTime CreatedAt { get; set; }
        public DateTime? DecidedAt { get; set; }
        public int? DecidedBy { get; set; }
    }
}