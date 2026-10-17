namespace TruthEye.DTOs
{
    public class MCQOptionDto
    {
        public int Id { get; set; } 
        public required string OptionText { get; set; }
        public bool IsCorrect { get; set; }
    }
}