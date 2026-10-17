namespace TruthEye.Entities
{
    public class Report
    {
        public int Id { get; set; }
        public string FilePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public int GeneratedBy { get; set; }
        public int ExamId { get; set; }
        public virtual Exam Exam { get; set; }
    }
}
