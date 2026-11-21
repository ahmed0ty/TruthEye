namespace TruthEye.Entities
{
    public class GradeAdjustment
    {
        public int Id { get; set; }
        public int AnswerId { get; set; }
        public int ExamSessionId { get; set; }
        public double OldMarks { get; set; }
        public double NewMarks { get; set; }
        public string Reason { get; set; } = "";
        public int ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}