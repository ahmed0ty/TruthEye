namespace TruthEye.Entities
{
    public class AIEvaluationResult
    {
        public double SimilarityScore { get; set; }
        public bool IsCorrect { get; set; }
        public double MarksObtained { get; set; }
        public string Feedback { get; set; }
        public List<string> KeyPoints { get; set; } = new();
    }
}
