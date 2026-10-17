namespace TruthEye.Entities
{
    public class WrittenQuestion : Question
    {
        public required string ModelAnswer { get; set; }
        public double AIThreshold { get; set; }
        public required string EvaluationModel { get; set; } = "Default";
    }
}