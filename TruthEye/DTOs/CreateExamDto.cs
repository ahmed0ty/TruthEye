using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class CreateExamDto
    {
        [Required, StringLength(200)] public string Title { get; set; }
        [Range(1, 1440)] public int Duration { get; set; }
        public int MaxAttempts { get; set; } = 1;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Type { get; set; } = "Standard";
        public bool RandomizeQuestions { get; set; }
        public bool RandomizeOptions { get; set; }
        public int PassingScore { get; set; } = 50;
        public int TotalMarks { get; set; } = 100;
        public string Instructions { get; set; }
        [Range(1, int.MaxValue)] public int CourseId { get; set; }
    }
}
