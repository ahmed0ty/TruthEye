namespace TruthEye.DTOs
{
    public class CommonMistakeDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public double ErrorRate { get; set; }
        public string CorrectAnswer { get; set; }
        public List<string> CommonWrongAnswers { get; set; }
    }
}
