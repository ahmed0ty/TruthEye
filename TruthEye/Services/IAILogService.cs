using TruthEye.Entities;

namespace TruthEye.Services
{
    public interface IAILogService
    {
        Task<AILog> LogEventAsync(int sessionId, string eventType, double confidence, string screenshotPath);
        Task<IEnumerable<AILog>> GetSessionAILogsAsync(int sessionId);
        Task<IEnumerable<AILog>> GetFlaggedLogsAsync(double threshold);
    }
}
