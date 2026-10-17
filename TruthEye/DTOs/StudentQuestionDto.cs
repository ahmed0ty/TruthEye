// جديد 

namespace TruthEye.DTOs
{
    public class StudentOptionDto
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = string.Empty;
    }

    public class StudentQuestionDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public int Marks { get; set; }
        public List<StudentOptionDto>? Options { get; set; }
    }

    public class ExamTakeDto
    {
        public int SessionId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public int RemainingSeconds { get; set; }
        public DateTime ServerTime { get; set; }
        public List<StudentQuestionDto> Questions { get; set; } = new();
        public List<StudentAnswerDto> Answers { get; set; } = new();
    }
}