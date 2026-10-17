// using System.Text;
// using System.Text.Json;
// using TruthEye.Entities;

// namespace TruthEye.Services
// {
//     public class AIEvaluationService : IAIEvaluationService
//     {
//         private readonly HttpClient _httpClient;
//         private readonly ILogger<AIEvaluationService> _logger;
//         private readonly ICacheService _cacheService;
//         private readonly string _sbertServiceUrl;

//         public AIEvaluationService(
//             HttpClient httpClient,
//             ILogger<AIEvaluationService> logger,
//             ICacheService cacheService,
//             IConfiguration configuration)
//         {
//             _httpClient = httpClient;
//             _logger = logger;
//             _cacheService = cacheService;
//             _sbertServiceUrl = configuration["AI:SBERTServiceUrl"] ?? "http://localhost:8000";
//         }

//         public async Task<AIEvaluationResult> EvaluateWrittenAnswerAsync(
//             string question, string modelAnswer, string studentAnswer, double threshold)
//         {
//             try
//             {
//                 var semanticScore = await CalculateSimilarityAsync(studentAnswer, modelAnswer);
//                 var keywordScore = CalculateKeywordSimilarity(modelAnswer, studentAnswer);
//                 var lengthScore = CalculateLengthScore(studentAnswer, modelAnswer);

//                 var hybridScore = (semanticScore * 0.6) + (keywordScore * 0.3) + (lengthScore * 0.1);

//                 var result = new AIEvaluationResult
//                 {
//                     SimilarityScore = hybridScore,
//                     IsCorrect = hybridScore >= threshold,
//                     MarksObtained = hybridScore,
//                     Feedback = GenerateDetailedFeedback(hybridScore, studentAnswer, modelAnswer, question),
//                     KeyPoints = ExtractKeyPoints(modelAnswer)
//                 };

//                 _logger.LogInformation("Free AI Evaluation - Score: {Score}, IsCorrect: {IsCorrect}",
//                     hybridScore, result.IsCorrect);

//                 return result;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error in free AI evaluation");
//                 return GetFallbackEvaluation(studentAnswer, modelAnswer, threshold);
//             }
//         }

//         public async Task<double> CalculateSimilarityAsync(string text1, string text2)
//         {
//             var cacheKey = $"sbert_similarity_{GetStringHash(text1 + text2)}";

//             return await _cacheService.GetOrSetAsync(cacheKey, async () =>
//             {
//                 try
//                 {
//                     return await CalculateWithSentenceBERT(text1, text2);
//                 }
//                 catch (Exception ex)
//                 {
//                     _logger.LogWarning(ex, "Sentence-BERT failed, using Hugging Face API");
//                     try
//                     {
//                         return await CalculateWithHuggingFace(text1, text2);
//                     }
//                     catch
//                     {
//                         return CalculateAdvancedSimilarity(text1, text2);
//                     }
//                 }
//             }, TimeSpan.FromHours(24));
//         }

//         private async Task<double> CalculateWithSentenceBERT(string text1, string text2)
//         {
//             try
//             {
//                 var request = new
//                 {
//                     texts = new[] { text1, text2 },
//                     model = "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2"
//                 };

//                 var response = await _httpClient.PostAsJsonAsync($"{_sbertServiceUrl}/embed", request);
//                 response.EnsureSuccessStatusCode();

//                 var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
//                 if (result?.Embeddings?.Count == 2)
//                 {
//                     return CalculateCosineSimilarity(result.Embeddings[0], result.Embeddings[1]);
//                 }

//                 throw new Exception("Invalid response from Sentence-BERT service");
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Sentence-BERT service failed");
//                 throw;
//             }
//         }

//         private async Task<double> CalculateWithHuggingFace(string text1, string text2)
//         {
//             try
//             {
//                 var request = new
//                 {
//                     inputs = new
//                     {
//                         source_sentence = text1,
//                         sentences = new[] { text2 }
//                     }
//                 };

//                 var response = await _httpClient.PostAsJsonAsync(
//                     "https://api-inference.huggingface.co/models/sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2",
//                     request);

//                 if (response.IsSuccessStatusCode)
//                 {
//                     var result = await response.Content.ReadFromJsonAsync<double[]>();
//                     return result?.FirstOrDefault() ?? 0.0;
//                 }

//                 throw new Exception($"Hugging Face API failed: {response.StatusCode}");
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Hugging Face API failed");
//                 throw;
//             }
//         }

//         private double CalculateAdvancedSimilarity(string text1, string text2)
//         {
//             if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
//                 return 0.0;

//             var normalized1 = NormalizeText(text1);
//             var normalized2 = NormalizeText(text2);

//             var sentenceSimilarity = CalculateSentenceSimilarity(normalized1, normalized2);
//             var keywordSimilarity = CalculateKeywordSimilarity(normalized1, normalized2);
//             var orderSimilarity = CalculateOrderSimilarity(normalized1, normalized2);

//             return (sentenceSimilarity * 0.5) + (keywordSimilarity * 0.3) + (orderSimilarity * 0.2);
//         }

//         private string NormalizeText(string text)
//         {
//             var normalized = text.ToLower()
//                 .Replace("أ", "ا")
//                 .Replace("إ", "ا")
//                 .Replace("آ", "ا")
//                 .Replace("ة", "ه");

//             return System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ").Trim();
//         }

//         private double CalculateSentenceSimilarity(string text1, string text2)
//         {
//             var sentences1 = SplitIntoSentences(text1);
//             var sentences2 = SplitIntoSentences(text2);

//             if (!sentences1.Any() || !sentences2.Any())
//                 return 0.0;

//             double totalSimilarity = 0;
//             int comparisons = 0;

//             foreach (var sent1 in sentences1)
//             {
//                 foreach (var sent2 in sentences2)
//                 {
//                     var similarity = CalculateJaccardSimilarity(sent1, sent2);
//                     totalSimilarity += similarity;
//                     comparisons++;
//                 }
//             }

//             return comparisons > 0 ? totalSimilarity / comparisons : 0.0;
//         }

//         private List<string> SplitIntoSentences(string text)
//         {
//             return text.Split(new[] { '.', '!', '؟', '?' }, StringSplitOptions.RemoveEmptyEntries)
//                       .Select(s => s.Trim())
//                       .Where(s => s.Length > 5)
//                       .ToList();
//         }

//         private double CalculateJaccardSimilarity(string text1, string text2)
//         {
//             var words1 = text1.Split(' ').Distinct().ToArray();
//             var words2 = text2.Split(' ').Distinct().ToArray();

//             var intersection = words1.Intersect(words2).Count();
//             var union = words1.Union(words2).Count();

//             return union == 0 ? 0 : (double)intersection / union;
//         }

//         private double CalculateKeywordSimilarity(string modelAnswer, string studentAnswer)
//         {
//             var modelKeywords = ExtractImportantWords(modelAnswer);
//             var studentKeywords = ExtractImportantWords(studentAnswer);

//             if (!modelKeywords.Any() || !studentKeywords.Any())
//                 return 0.0;

//             var intersection = modelKeywords.Intersect(studentKeywords).Count();
//             var union = modelKeywords.Union(studentKeywords).Count();

//             return union > 0 ? (double)intersection / union : 0.0;
//         }

//         private HashSet<string> ExtractImportantWords(string text)
//         {
//             var stopWords = new HashSet<string>
//             {
//                 "the", "a", "an", "in", "on", "at", "and", "or", "but", "of", "to",
//                 "for", "with", "by", "is", "are", "was", "were", "this", "that",
//                 "هو", "هي", "في", "على", "من", "إلى", "عن", "مع", "كان", "يكون"
//             };

//             return text.Split(new[] { ' ', '.', ',', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
//                       .Where(word => word.Length > 2 && !stopWords.Contains(word.ToLower()))
//                       .Select(word => word.ToLower())
//                       .ToHashSet();
//         }

//         private double CalculateOrderSimilarity(string text1, string text2)
//         {
//             var words1 = text1.Split(' ').Where(w => w.Length > 2).ToArray();
//             var words2 = text2.Split(' ').Where(w => w.Length > 2).ToArray();

//             if (words1.Length < 2 || words2.Length < 2)
//                 return 0.0;

//             var commonWords = words1.Intersect(words2).ToArray();
//             if (commonWords.Length < 2)
//                 return 0.0;

//             int orderMatches = 0;
//             for (int i = 0; i < commonWords.Length - 1; i++)
//             {
//                 var word1 = commonWords[i];
//                 var word2 = commonWords[i + 1];

//                 var pos1 = Array.IndexOf(words1, word1);
//                 var pos2 = Array.IndexOf(words1, word2);
//                 var pos3 = Array.IndexOf(words2, word1);
//                 var pos4 = Array.IndexOf(words2, word2);

//                 if (pos1 < pos2 && pos3 < pos4)
//                     orderMatches++;
//             }

//             return (double)orderMatches / Math.Max(1, commonWords.Length - 1);
//         }

//         private double CalculateLengthScore(string studentAnswer, string modelAnswer)
//         {
//             if (string.IsNullOrEmpty(modelAnswer))
//                 return 0.5;

//             var modelLength = modelAnswer.Length;
//             var studentLength = studentAnswer.Length;

//             if (modelLength == 0)
//                 return 0.5;

//             var ratio = (double)studentLength / modelLength;

//             if (ratio < 0.3) return 0.2;
//             if (ratio > 2.0) return 0.3;
//             if (ratio < 0.6) return 0.5;
//             if (ratio > 1.5) return 0.6;

//             return 0.8;
//         }

//         private double CalculateCosineSimilarity(double[] vector1, double[] vector2)
//         {
//             if (vector1.Length != vector2.Length || vector1.Length == 0)
//                 return 0.0;

//             double dotProduct = 0.0, magnitude1 = 0.0, magnitude2 = 0.0;

//             for (int i = 0; i < vector1.Length; i++)
//             {
//                 dotProduct += vector1[i] * vector2[i];
//                 magnitude1 += Math.Pow(vector1[i], 2);
//                 magnitude2 += Math.Pow(vector2[i], 2);
//             }

//             magnitude1 = Math.Sqrt(magnitude1);
//             magnitude2 = Math.Sqrt(magnitude2);

//             return magnitude1 > 0 && magnitude2 > 0 ?
//                    Math.Max(0, Math.Min(1, dotProduct / (magnitude1 * magnitude2))) : 0.0;
//         }

//         private string GenerateDetailedFeedback(double score, string studentAnswer, string modelAnswer, string question)
//         {
//             var feedback = new StringBuilder();

//             if (score >= 0.8)
//             {
//                 feedback.AppendLine(" Excellent Answer!");
//                 feedback.AppendLine(" Covered all main points perfectly");
//                 feedback.AppendLine(" Logical organization and structure");
//                 feedback.AppendLine(" Good language and expression");
//             }
//             else if (score >= 0.6)
//             {
//                 feedback.AppendLine(" Good Answer");
//                 feedback.AppendLine(" Covered most main points");
//                 feedback.AppendLine(" Organization can be improved");
//                 feedback.AppendLine(" Add more specific details");
//             }
//             else if (score >= 0.4)
//             {
//                 feedback.AppendLine(" Acceptable Answer");
//                 feedback.AppendLine(" Covered some basic points");
//                 feedback.AppendLine(" Needs better organization");
//                 feedback.AppendLine(" Focus on main question points");
//             }
//             else
//             {
//                 feedback.AppendLine(" Needs Improvement");
//                 feedback.AppendLine(" Missed key concepts");
//                 feedback.AppendLine(" Poor organization");
//                 feedback.AppendLine(" Review fundamental concepts");
//             }

//             var studentKeywords = ExtractImportantWords(studentAnswer);
//             var modelKeywords = ExtractImportantWords(modelAnswer);
//             var missingKeywords = modelKeywords.Except(studentKeywords).Take(3);

//             if (missingKeywords.Any())
//             {
//                 feedback.AppendLine();
//                 feedback.AppendLine("💡 Improvement Tips:");
//                 foreach (var keyword in missingKeywords)
//                 {
//                     feedback.AppendLine($" Focus on: {keyword}");
//                 }
//             }

//             return feedback.ToString();
//         }

//         private List<string> ExtractKeyPoints(string text)
//         {
//             return SplitIntoSentences(text)
//                   .Take(3)
//                   .Select(s => s.Trim())
//                   .Where(s => !string.IsNullOrEmpty(s))
//                   .ToList();
//         }

//         private AIEvaluationResult GetFallbackEvaluation(string studentAnswer, string modelAnswer, double threshold)
//         {
//             var similarity = CalculateAdvancedSimilarity(studentAnswer, modelAnswer);

//             return new AIEvaluationResult
//             {
//                 SimilarityScore = similarity,
//                 IsCorrect = similarity >= threshold,
//                 MarksObtained = similarity,
//                 Feedback = "Evaluation completed using fallback method.",
//                 KeyPoints = ExtractKeyPoints(modelAnswer)
//             };
//         }

//         private string GetStringHash(string input)
//         {
//             using var sha256 = System.Security.Cryptography.SHA256.Create();
//             var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty); 
//             var hash = sha256.ComputeHash(bytes);
//             return Convert.ToBase64String(hash);
//         }
//     }
// }
using System.Text;
using System.Text.Json;
using TruthEye.Entities;

namespace TruthEye.Services
{
    public class AIEvaluationService : IAIEvaluationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIEvaluationService> _logger;
        private readonly ICacheService _cacheService;
        private readonly string _sbertServiceUrl;

        public AIEvaluationService(
            HttpClient httpClient,
            ILogger<AIEvaluationService> logger,
            ICacheService cacheService,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cacheService = cacheService;
            _sbertServiceUrl = configuration["AI:SBERTServiceUrl"] ?? "http://localhost:8000";
        }

        public async Task<AIEvaluationResult> EvaluateWrittenAnswerAsync(
            string question, string modelAnswer, string studentAnswer, double threshold)
        {
            try
            {
                studentAnswer ??= string.Empty;
                modelAnswer ??= string.Empty;

                double semanticScore;
                double keywordScore;

                // An exact match (after normalisation) is always a full score
                if (NormalizeText(studentAnswer) == NormalizeText(modelAnswer) && studentAnswer.Length > 0)
                {
                    semanticScore = 1.0;
                    keywordScore = 1.0;
                }
                else
                {
                    semanticScore = await CalculateSimilarityAsync(studentAnswer, modelAnswer);
                    keywordScore = CalculateKeywordSimilarity(modelAnswer, studentAnswer);
                }

                var lengthScore = CalculateLengthScore(studentAnswer, modelAnswer);

                // Semantic similarity carries most of the weight; length is a minor factor
                var hybridScore = Math.Min(1.0, (semanticScore * 0.75) + (keywordScore * 0.2) + (lengthScore * 0.05));

                var result = new AIEvaluationResult
                {
                    SimilarityScore = hybridScore,
                    IsCorrect = hybridScore >= threshold,
                    MarksObtained = hybridScore,
                    Feedback = GenerateDetailedFeedback(hybridScore, studentAnswer, modelAnswer, question),
                    KeyPoints = ExtractKeyPoints(modelAnswer)
                };

                _logger.LogInformation(
                    "AI Evaluation - Semantic: {Semantic}, Keyword: {Keyword}, Length: {Length}, Final: {Score}, IsCorrect: {IsCorrect}",
                    semanticScore, keywordScore, lengthScore, hybridScore, result.IsCorrect);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in free AI evaluation");
                return GetFallbackEvaluation(studentAnswer, modelAnswer, threshold);
            }
        }

       public async Task<double> CalculateSimilarityAsync(string text1, string text2)
{
    var cacheKey = $"sbert_similarity_{GetStringHash(text1 + "|" + text2)}";

    try
    {
        // Only real SBERT results get cached; if SBERT throws, nothing is stored
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            () => CalculateWithSentenceBERT(text1, text2),
            TimeSpan.FromHours(24));
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Sentence-BERT failed, using local fallback (not cached)");
        try
        {
            return await CalculateWithHuggingFace(text1, text2);
        }
        catch
        {
            return CalculateAdvancedSimilarity(text1, text2);
        }
    }
}

        private async Task<double> CalculateWithSentenceBERT(string text1, string text2)
        {
            try
            {
                var request = new
                {
                    texts = new[] { text1, text2 },
                    model = "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2"
                };

                var response = await _httpClient.PostAsJsonAsync($"{_sbertServiceUrl}/embed", request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
                if (result?.Embeddings?.Count == 2)
                {
                    return CalculateCosineSimilarity(result.Embeddings[0], result.Embeddings[1]);
                }

                throw new Exception("Invalid response from Sentence-BERT service");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sentence-BERT service failed");
                throw;
            }
        }

        private async Task<double> CalculateWithHuggingFace(string text1, string text2)
        {
            try
            {
                var request = new
                {
                    inputs = new
                    {
                        source_sentence = text1,
                        sentences = new[] { text2 }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(
                    "https://api-inference.huggingface.co/models/sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2",
                    request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<double[]>();
                    return result?.FirstOrDefault() ?? 0.0;
                }

                throw new Exception($"Hugging Face API failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hugging Face API failed");
                throw;
            }
        }

        private double CalculateAdvancedSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0.0;

            var normalized1 = NormalizeText(text1);
            var normalized2 = NormalizeText(text2);

            if (normalized1 == normalized2)
                return 1.0;

            var sentenceSimilarity = CalculateSentenceSimilarity(normalized1, normalized2);
            var keywordSimilarity = CalculateKeywordSimilarity(normalized1, normalized2);
            var orderSimilarity = CalculateOrderSimilarity(normalized1, normalized2);

            // Whole-text word overlap, so short answers without punctuation still score
            var wholeTextSimilarity = CalculateJaccardSimilarity(normalized1, normalized2);

            var detailed = (sentenceSimilarity * 0.3) + (keywordSimilarity * 0.3) + (orderSimilarity * 0.1);
            return Math.Min(1.0, detailed + (wholeTextSimilarity * 0.3));
        }

        private string NormalizeText(string text)
        {
            var normalized = (text ?? string.Empty).ToLower()
                .Replace("أ", "ا")
                .Replace("إ", "ا")
                .Replace("آ", "ا")
                .Replace("ة", "ه");

            return System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ").Trim();
        }

        private double CalculateSentenceSimilarity(string text1, string text2)
        {
            var sentences1 = SplitIntoSentences(text1);
            var sentences2 = SplitIntoSentences(text2);

            if (!sentences1.Any() || !sentences2.Any())
                return 0.0;

            double totalSimilarity = 0;
            int comparisons = 0;

            foreach (var sent1 in sentences1)
            {
                foreach (var sent2 in sentences2)
                {
                    var similarity = CalculateJaccardSimilarity(sent1, sent2);
                    totalSimilarity += similarity;
                    comparisons++;
                }
            }

            return comparisons > 0 ? totalSimilarity / comparisons : 0.0;
        }

        private List<string> SplitIntoSentences(string text)
        {
            return text.Split(new[] { '.', '!', '؟', '?' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => s.Trim())
                      .Where(s => s.Length > 5)
                      .ToList();
        }

        private double CalculateJaccardSimilarity(string text1, string text2)
        {
            var words1 = text1.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
            var words2 = text2.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();

            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            return union == 0 ? 0 : (double)intersection / union;
        }

        private double CalculateKeywordSimilarity(string modelAnswer, string studentAnswer)
        {
            var modelKeywords = ExtractImportantWords(modelAnswer);
            var studentKeywords = ExtractImportantWords(studentAnswer);

            if (!modelKeywords.Any() || !studentKeywords.Any())
                return 0.0;

            var intersection = modelKeywords.Intersect(studentKeywords).Count();
            var union = modelKeywords.Union(studentKeywords).Count();

            return union > 0 ? (double)intersection / union : 0.0;
        }

        private HashSet<string> ExtractImportantWords(string text)
        {
            var stopWords = new HashSet<string>
            {
                "the", "a", "an", "in", "on", "at", "and", "or", "but", "of", "to",
                "for", "with", "by", "is", "are", "was", "were", "this", "that",
                "هو", "هي", "في", "على", "من", "إلى", "عن", "مع", "كان", "يكون"
            };

            return (text ?? string.Empty)
                      .Split(new[] { ' ', '.', ',', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                      .Where(word => word.Length > 2 && !stopWords.Contains(word.ToLower()))
                      .Select(word => word.ToLower())
                      .ToHashSet();
        }

        private double CalculateOrderSimilarity(string text1, string text2)
        {
            var words1 = text1.Split(' ').Where(w => w.Length > 2).ToArray();
            var words2 = text2.Split(' ').Where(w => w.Length > 2).ToArray();

            if (words1.Length < 2 || words2.Length < 2)
                return 0.0;

            var commonWords = words1.Intersect(words2).ToArray();
            if (commonWords.Length < 2)
                return 0.0;

            int orderMatches = 0;
            for (int i = 0; i < commonWords.Length - 1; i++)
            {
                var word1 = commonWords[i];
                var word2 = commonWords[i + 1];

                var pos1 = Array.IndexOf(words1, word1);
                var pos2 = Array.IndexOf(words1, word2);
                var pos3 = Array.IndexOf(words2, word1);
                var pos4 = Array.IndexOf(words2, word2);

                if (pos1 < pos2 && pos3 < pos4)
                    orderMatches++;
            }

            return (double)orderMatches / Math.Max(1, commonWords.Length - 1);
        }

        private double CalculateLengthScore(string studentAnswer, string modelAnswer)
        {
            if (string.IsNullOrEmpty(modelAnswer))
                return 0.5;

            var modelLength = modelAnswer.Length;
            var studentLength = (studentAnswer ?? string.Empty).Length;

            if (modelLength == 0)
                return 0.5;

            var ratio = (double)studentLength / modelLength;

            if (ratio < 0.3) return 0.2;
            if (ratio > 2.0) return 0.3;
            if (ratio < 0.6) return 0.5;
            if (ratio > 1.5) return 0.6;

            return 1.0;
        }

        private double CalculateCosineSimilarity(double[] vector1, double[] vector2)
        {
            if (vector1.Length != vector2.Length || vector1.Length == 0)
                return 0.0;

            double dotProduct = 0.0, magnitude1 = 0.0, magnitude2 = 0.0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += Math.Pow(vector1[i], 2);
                magnitude2 += Math.Pow(vector2[i], 2);
            }

            magnitude1 = Math.Sqrt(magnitude1);
            magnitude2 = Math.Sqrt(magnitude2);

            return magnitude1 > 0 && magnitude2 > 0 ?
                   Math.Max(0, Math.Min(1, dotProduct / (magnitude1 * magnitude2))) : 0.0;
        }

        private string GenerateDetailedFeedback(double score, string studentAnswer, string modelAnswer, string question)
        {
            var feedback = new StringBuilder();

            if (score >= 0.8)
            {
                feedback.AppendLine(" Excellent Answer!");
                feedback.AppendLine(" Covered all main points perfectly");
                feedback.AppendLine(" Logical organization and structure");
                feedback.AppendLine(" Good language and expression");
            }
            else if (score >= 0.6)
            {
                feedback.AppendLine(" Good Answer");
                feedback.AppendLine(" Covered most main points");
                feedback.AppendLine(" Organization can be improved");
                feedback.AppendLine(" Add more specific details");
            }
            else if (score >= 0.4)
            {
                feedback.AppendLine(" Acceptable Answer");
                feedback.AppendLine(" Covered some basic points");
                feedback.AppendLine(" Needs better organization");
                feedback.AppendLine(" Focus on main question points");
            }
            else
            {
                feedback.AppendLine(" Needs Improvement");
                feedback.AppendLine(" Missed key concepts");
                feedback.AppendLine(" Poor organization");
                feedback.AppendLine(" Review fundamental concepts");
            }

            var studentKeywords = ExtractImportantWords(studentAnswer);
            var modelKeywords = ExtractImportantWords(modelAnswer);
            var missingKeywords = modelKeywords.Except(studentKeywords).Take(3);

            if (missingKeywords.Any())
            {
                feedback.AppendLine();
                feedback.AppendLine("💡 Improvement Tips:");
                foreach (var keyword in missingKeywords)
                {
                    feedback.AppendLine($" Focus on: {keyword}");
                }
            }

            return feedback.ToString();
        }

        private List<string> ExtractKeyPoints(string text)
        {
            return SplitIntoSentences(text)
                  .Take(3)
                  .Select(s => s.Trim())
                  .Where(s => !string.IsNullOrEmpty(s))
                  .ToList();
        }

        private AIEvaluationResult GetFallbackEvaluation(string studentAnswer, string modelAnswer, double threshold)
        {
            var similarity = CalculateAdvancedSimilarity(studentAnswer, modelAnswer);

            return new AIEvaluationResult
            {
                SimilarityScore = similarity,
                IsCorrect = similarity >= threshold,
                MarksObtained = similarity,
                Feedback = "Evaluation completed using fallback method.",
                KeyPoints = ExtractKeyPoints(modelAnswer)
            };
        }

        private string GetStringHash(string input)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}