using TruthEye.Entities;

namespace TruthEye.Services
{
    public interface IAIEvaluationService
    {
        Task<AIEvaluationResult> EvaluateWrittenAnswerAsync(string question, string modelAnswer, string studentAnswer, double threshold);
        Task<double> CalculateSimilarityAsync(string text1, string text2);
    }
}
