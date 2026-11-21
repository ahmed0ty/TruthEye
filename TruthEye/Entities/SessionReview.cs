namespace TruthEye.Entities
{
    public class SessionReview
    {
        public int Id { get; set; }
        public int ExamSessionId { get; set; }
        public string Decision { get; set; } = "Cleared";   // Cleared | Confirmed
        public string? Note { get; set; }
        public int RiskScore { get; set; }                  // الدرجة وقت القرار
        public int ReviewedBy { get; set; }
        public DateTime ReviewedAt { get; set; }
    }
}