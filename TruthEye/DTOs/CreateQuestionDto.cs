using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class CreateQuestionDto
    {
        [Required, StringLength(4000)]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        public string QuestionType { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Marks { get; set; }

        [Range(1, int.MaxValue)]
        public int ExamId { get; set; }

        // MCQ only
        public List<MCQOptionDto>? Options { get; set; }

        // Written only
        public string? ModelAnswer { get; set; }
        public double AIThreshold { get; set; } = 0.7;
        public string? EvaluationModel { get; set; } = "Default";
    }
}