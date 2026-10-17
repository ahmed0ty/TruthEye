using TruthEye.Entities;

namespace TruthEye.Services
{
    public class RobustAIEvaluationService : IAIEvaluationService
    {
        private const int MaxRetries = 2;

        private readonly IAIEvaluationService _aiEvaluationService;
        private readonly ILogger<RobustAIEvaluationService> _logger;

        public RobustAIEvaluationService(
            IAIEvaluationService aiEvaluationService,
            ILogger<RobustAIEvaluationService> logger)
        {
            _aiEvaluationService = aiEvaluationService;
            _logger = logger;
        }

        public async Task<AIEvaluationResult> EvaluateWrittenAnswerAsync(
            string question, string modelAnswer, string studentAnswer, double threshold)
        {
            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var result = await _aiEvaluationService.EvaluateWrittenAnswerAsync(
                        question, modelAnswer, studentAnswer, threshold);

                    _logger.LogInformation("Evaluation successful on attempt {Attempt}", attempt + 1);
                    return result;
                }
                catch (Exception ex) when (attempt < MaxRetries)
                {
                    _logger.LogWarning(ex, "Evaluation attempt {Attempt} failed, retrying...", attempt + 1);
                    await Task.Delay(1000 * (attempt + 1));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Evaluation failed after {Attempts} attempts", attempt + 1);
                }
            }

            _logger.LogWarning("All evaluation attempts failed, using basic similarity");
            return await GetBasicEvaluation(modelAnswer, studentAnswer, threshold);
        }

        public Task<double> CalculateSimilarityAsync(string text1, string text2)
            => _aiEvaluationService.CalculateSimilarityAsync(text1, text2);

        private async Task<AIEvaluationResult> GetBasicEvaluation(
            string modelAnswer, string studentAnswer, double threshold)
        {
            modelAnswer ??= string.Empty;
            studentAnswer ??= string.Empty;

            double similarity;
            try
            {
                similarity = await CalculateSimilarityAsync(studentAnswer, modelAnswer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Basic similarity failed, scoring 0");
                similarity = 0;
            }

            similarity = Math.Clamp(similarity, 0, 1);

            return new AIEvaluationResult
            {
                SimilarityScore = similarity,
                IsCorrect = similarity >= threshold,
                MarksObtained = similarity,
                Feedback = "Basic evaluation completed. For detailed analysis, try again later.",
                KeyPoints = modelAnswer
                    .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Take(2)
                    .ToList()
            };
        }
    }
}