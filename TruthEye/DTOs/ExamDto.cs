namespace TruthEye.DTOs
{
    public class ExamDto
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public int Duration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public required string Status { get; set; }
        public required string Type { get; set; }
        public bool RandomizeQuestions { get; set; }
        public bool RandomizeOptions { get; set; }
        public int PassingScore { get; set; }
        public int TotalMarks { get; set; }
        public required string Instructions { get; set; }
        public int CourseId { get; set; }
        public int DoctorId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}