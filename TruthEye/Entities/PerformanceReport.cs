namespace TruthEye.Entities
{
    public class PerformanceReport
    {
        public int Id { get; set; }
        public double Score { get; set; }
        public int AIAlertsCount { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime GeneratedAt { get; set; }
        public int StudentId { get; set; }
        public int ExamId { get; set; }
        public virtual Student Student { get; set; }
        public virtual Exam Exam { get; set; }
    }
}
