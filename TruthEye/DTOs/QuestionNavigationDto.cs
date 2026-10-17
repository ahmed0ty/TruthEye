namespace TruthEye.DTOs
{
    public class QuestionNavigationDto
    {
        public QuestionDto? NextQuestion { get; set; }
        public bool HasNext { get; set; }
        public int CurrentQuestionNumber { get; set; }
        public int TotalQuestions { get; set; }
    }
}
