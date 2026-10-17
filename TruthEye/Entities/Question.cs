namespace TruthEye.Entities
{
    public class Question
    {
        public int Id { get; set; }
        public required string QuestionText { get; set; }
        public required string QuestionType { get; set; }
        public int Marks { get; set; }
        public int ExamId { get; set; }
        public virtual required Exam Exam { get; set; }
    }
}