namespace TruthEye.Entities
{
    public class ProctorSession
    {
        public int Id { get; set; }
        public int ProctorId { get; set; }
        public int ExamId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int StudentsMonitored { get; set; }
        public int AlertsReviewed { get; set; }
        public string Notes { get; set; }

        public virtual Proctor Proctor { get; set; }
        public virtual Exam Exam { get; set; }
    }
}
