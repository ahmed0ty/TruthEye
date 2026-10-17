using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class CreateExamDto
    {
        [Required] public string Title { get; set; }
        [Required] public int Duration { get; set; }
        public int MaxAttempts { get; set; } = 1;
        [Required] public DateTime StartTime { get; set; }
        [Required] public DateTime EndTime { get; set; }
        public string Type { get; set; } = "Standard";
        public bool RandomizeQuestions { get; set; }
        public bool RandomizeOptions { get; set; }
        public int PassingScore { get; set; } = 50;
        public int TotalMarks { get; set; } = 100;
        public string Instructions { get; set; }
        [Required] public int CourseId { get; set; }
    }
}
