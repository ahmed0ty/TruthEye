namespace TruthEye.Entities
{
    public class MCQOption
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public required string OptionText { get; set; }
        public bool IsCorrect { get; set; }
        public virtual MCQQuestion Question { get; set; }
    }
}