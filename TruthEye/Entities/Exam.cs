namespace TruthEye.Entities
{
    public class Exam : BaseEntity
    {
        public required string Title { get; set; }
        public int MaxAttempts { get; set; } = 1;
        public int Duration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public required string Status { get; set; }
        public required string Type { get; set; }
        public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool RandomizeQuestions { get; set; }
        public bool RandomizeOptions { get; set; }
        public int PassingScore { get; set; }
        public int TotalMarks { get; set; }
        public required string Instructions { get; set; }
        public int CourseId { get; set; }
        public int DoctorId { get; set; }
        public virtual required Course Course { get; set; }
        public virtual required Doctor Doctor { get; set; }
        public virtual required ICollection<Question> Questions { get; set; } = new List<Question>();
        public virtual required ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();
        public virtual required ICollection<Proctor> Proctors { get; set; } = new List<Proctor>();
    }
}