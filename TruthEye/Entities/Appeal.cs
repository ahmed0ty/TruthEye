namespace TruthEye.Entities
{
    public class Appeal
    {
        public int Id { get; set; }
        public int ExamSessionId { get; set; }
        public virtual ExamSession ExamSession { get; set; } = null!;
        public int StudentId { get; set; }
        public string Message { get; set; } = "";
        public string Status { get; set; } = "Pending";   // Pending | Accepted | Rejected
        public string? Response { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public int? RespondedBy { get; set; }
    }
}