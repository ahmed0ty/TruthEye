using TruthEye.DTOs;

namespace TruthEye.Entities
{
    public class ExamSession
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public required string Status { get; set; }
        public int AttemptNumber { get; set; }
        public double? AIScore { get; set; }
        public double TotalScore { get; set; }
        public int TotalMarks { get; set; }
        public double Percentage { get; set; }
        public bool IsPassed { get; set; }
        public int StudentId { get; set; }
        public int ExamId { get; set; }

        public virtual Student Student { get; set; }
        public virtual Exam Exam { get; set; }
        public virtual ICollection<AILog> AILogs { get; set; } = new List<AILog>();
        public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public DateTime? SubmittedAt { get; internal set; }
    }
}