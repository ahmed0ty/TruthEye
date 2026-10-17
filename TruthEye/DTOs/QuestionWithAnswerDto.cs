namespace TruthEye.DTOs
{
    public class QuestionWithAnswerDto
    {
        public int QuestionId { get; set; }
        public required string QuestionText { get; set; }
        public required string QuestionType { get; set; }
        public int Marks { get; set; }
        public bool IsCorrect { get; set; }
        public double MarksObtained { get; set; }

        public List<MCQOptionDto>? Options { get; set; }
        public int? CorrectOptionId { get; set; }
        public int? StudentSelectedOptionId { get; set; }

        public string? ModelAnswer { get; set; }
        public string? StudentAnswer { get; set; }
        public double? AISimilarityScore { get; set; }
        public string? AIFeedback { get; set; }
    }
}