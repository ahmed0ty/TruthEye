namespace TruthEye.DTOs
{
    public class QuestionDto
    {
         public int Id { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public int Marks { get; set; }
        public int ExamId { get; set; }
        public List<MCQOptionDto> Options { get; set; }
        public string ModelAnswer { get; set; }
        public double AIThreshold { get; set; }
        public string EvaluationModel { get; set; }
    }
}
