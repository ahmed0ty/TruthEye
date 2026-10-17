namespace TruthEye.DTOs
{
    public class SubmitAnswerDto
    {
        public int QuestionId { get; set; }
        public string AnswerText { get; set; } 
        public int? SelectedOptionId { get; set; } 
    }
}
