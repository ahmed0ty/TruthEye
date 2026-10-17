using TruthEye.Data.Enum;

namespace TruthEye.Entities
{
   
        public class ExamAttempt
        {
            public int Id { get; set; }
            public int ExamId { get; set; }
            public required string StudentId { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public DateTime? SubmittedAt { get; set; }
            public decimal? Score { get; set; }
            public ExamAttemptStatus Status { get; set; }
            public int TimeSpentSeconds { get; set; }

            public required Exam Exam { get; set; }
            public required ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
        }
    }


