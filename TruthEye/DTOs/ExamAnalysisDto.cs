namespace TruthEye.DTOs
{
    public class ExamAnalysisDto
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; }
        public int TotalStudents { get; set; }
        public double AverageScore { get; set; }
        public double PassRate { get; set; }
        public List<CommonMistakeDto> CommonMistakes { get; set; }
    }
}
