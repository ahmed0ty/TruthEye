using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using TruthEye.API.Hubs;
using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Services
{
    public class AILogService : IAILogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<ExamMonitoringHub> _monitoringHub;
        private readonly INotificationService _notificationService;
        private readonly ISettingsService _settings;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AILogService> _logger;

        public AILogService(
            IUnitOfWork unitOfWork,
            IHubContext<ExamMonitoringHub> monitoringHub,
            INotificationService notificationService,
            ISettingsService settings,
            IMemoryCache cache,
            ILogger<AILogService> logger)
        {
            _unitOfWork = unitOfWork;
            _monitoringHub = monitoringHub;
            _notificationService = notificationService;
            _settings = settings;
            _cache = cache;
            _logger = logger;
        }

        public async Task<AILog> LogEventAsync(int sessionId, string eventType, double confidence, string screenshotPath)
        {
            var log = new AILog
            {
                ExamSessionId = sessionId,
                EventType = eventType,
                Confidence = confidence,
                ScreenshotPath = screenshotPath ?? "",
                Timestamp = DateTime.UtcNow
            };

            await _unitOfWork.Repository<AILog>().AddAsync(log);
            await _unitOfWork.SaveChangesAsync();

            var session = await _unitOfWork.Repository<ExamSession>().GetByIdAsync(sessionId, s => s.Exam!);
            if (session == null) return log;

            // Live feed for proctors watching this exam
            await _monitoringHub.Clients.Group($"exam-monitor:{session.ExamId}")
                .SendAsync("AIAlertReceived", new
                {
                    logId = log.Id,
                    sessionId,
                    examId = session.ExamId,
                    studentId = session.StudentId,
                    eventType,
                    confidence,
                    screenshotPath,
                    timestamp = log.Timestamp
                });

            // Stored notifications for proctors (they survive a refresh and show in the bell)
            try
            {
                await RaiseProctorAlertsAsync(session, eventType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Couldn't raise proctor alerts for session {SessionId}", sessionId);
            }

            _logger.LogWarning("AI Event logged: {EventType} - Confidence: {Confidence}", eventType, confidence);
            return log;
        }

        private async Task RaiseProctorAlertsAsync(ExamSession session, string eventType)
        {
            var total = await _unitOfWork.Repository<AILog>().CountAsync(l => l.ExamSessionId == session.Id);
            var max = await _settings.GetIntAsync(SettingKeys.MaxViolationsBeforeFlag, 8, 0, 1000);
            var title = session.Exam?.Title ?? $"exam #{session.ExamId}";

            string? message = null;
            if (max > 0 && total == max && Once($"flag:{session.Id}", TimeSpan.FromHours(12)))
                message = $"Session #{session.Id} in '{title}' reached {max} recorded violations";
            else if (eventType == "MultipleFaces" && Once($"multi:{session.Id}", TimeSpan.FromMinutes(2)))
                message = $"More than one face detected in session #{session.Id} ('{title}')";

            if (message == null) return;

            var proctors = await _unitOfWork.Repository<Proctor>().FindAsync(p => p.IsActive);
            foreach (var p in proctors)
                await _notificationService.SendNotificationAsync(p.Id, message, "AIAlert");
        }

        // True the first time a key is seen within its window, so the same alert isn't sent repeatedly
        private bool Once(string key, TimeSpan window)
        {
            if (_cache.TryGetValue("alert:" + key, out _)) return false;
            _cache.Set("alert:" + key, true, window);
            return true;
        }

        public async Task<IEnumerable<AILog>> GetSessionAILogsAsync(int sessionId)
        {
            return await _unitOfWork.Repository<AILog>()
                .FindAsync(log => log.ExamSessionId == sessionId);
        }

        public async Task<IEnumerable<AILog>> GetFlaggedLogsAsync(double threshold)
        {
            return await _unitOfWork.Repository<AILog>()
                .FindAsync(log => log.Confidence >= threshold);
        }
    }
}