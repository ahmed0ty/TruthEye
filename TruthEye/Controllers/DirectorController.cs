using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TruthEye.Entities;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   [Authorize(Roles = "Admin,Proctor")]

    public class DirectorController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DirectorController> _logger;

        public DirectorController(IUnitOfWork unitOfWork, ILogger<DirectorController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpGet("active-sessions")]
        public async Task<IActionResult> GetAllActiveSessions()
        {
            try
            {
                var sessions = await _unitOfWork.Repository<ExamSession>()
                    .FindAsync(es => es.Status == "InProgress");

                var sessionDetails = sessions.Select(s => new
                {
                    s.Id,
                    StudentName = $"{s.Student.FirstName} {s.Student.LastName}",
                    ExamTitle = s.Exam.Title,
                    s.StartTime,
                    Duration = DateTime.UtcNow - s.StartTime,
                    s.AIScore,
                    AlertsCount = s.AILogs.Count
                });

                return Ok(sessionDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve active sessions");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("ai-alerts")]
        public async Task<IActionResult> GetAIAlerts([FromQuery] double? minConfidence = null, [FromQuery] string eventType = null)
        {
            try
            {
                var logs = await _unitOfWork.Repository<AILog>().GetAllAsync();

                if (minConfidence.HasValue)
                    logs = logs.Where(l => l.Confidence >= minConfidence.Value);

                if (!string.IsNullOrEmpty(eventType))
                    logs = logs.Where(l => l.EventType == eventType);

                var alertsGrouped = logs.GroupBy(l => l.EventType)
                    .Select(g => new
                    {
                        EventType = g.Key,
                        Count = g.Count(),
                        AverageConfidence = g.Average(l => l.Confidence),
                        RecentAlerts = g.OrderByDescending(l => l.Timestamp).Take(10)
                    });

                return Ok(alertsGrouped);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve AI alerts");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                var exams = await _unitOfWork.Repository<Exam>()
                    .FindAsync(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate);

                var sessions = await _unitOfWork.Repository<ExamSession>()
                    .FindAsync(es => es.StartTime >= startDate && es.StartTime <= endDate);

                var aiLogs = await _unitOfWork.Repository<AILog>()
                    .FindAsync(ai => ai.Timestamp >= startDate && ai.Timestamp <= endDate);

                var analytics = new
                {
                    TotalExams = exams.Count(),
                    TotalSessions = sessions.Count(),
                    CompletedSessions = sessions.Count(s => s.Status == "Completed"),
                    AverageAIScore = sessions.Where(s => s.AIScore.HasValue).Average(s => s.AIScore),
                    TotalAIAlerts = aiLogs.Count(),
                    HighRiskSessions = sessions.Count(s => s.AIScore < 50),
                    AlertsByType = aiLogs.GroupBy(l => l.EventType)
                        .Select(g => new { Type = g.Key, Count = g.Count() }),
                    SessionsByHour = sessions.GroupBy(s => s.StartTime.Hour)
                        .Select(g => new { Hour = g.Key, Count = g.Count() })
                };

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve analytics");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("performance-reports")]
        public async Task<IActionResult> GetPerformanceReports([FromQuery] int? courseId = null)
        {
            try
            {
                var reports = courseId.HasValue
                    ? await _unitOfWork.Repository<PerformanceReport>()
                        .FindAsync(r => r.Exam.CourseId == courseId.Value)
                    : await _unitOfWork.Repository<PerformanceReport>().GetAllAsync();

                var summary = new
                {
                    TotalReports = reports.Count(),
                    AverageScore = reports.Average(r => r.Score),
                    AverageAIAlerts = reports.Average(r => r.AIAlertsCount),
                    TopPerformers = reports.OrderByDescending(r => r.Score).Take(10),
                    AtRiskStudents = reports.Where(r => r.AIAlertsCount > 5 || r.Score < 50).ToList()
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve performance reports");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("assign-proctor")]
        public async Task<IActionResult> AssignProctor([FromQuery] int examId, [FromQuery] int proctorId)
        {
            try
            {
                var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
                var proctor = await _unitOfWork.Repository<Proctor>().GetByIdAsync(proctorId);

                if (exam == null || proctor == null)
                    return NotFound(new { Error = "Exam or Proctor not found" });

                proctor.AssignedExams.Add(exam);
                await _unitOfWork.Repository<Proctor>().UpdateAsync(proctor);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Proctor {ProctorId} assigned to exam {ExamId}", proctorId, examId);
                return Ok(new { Message = "Proctor assigned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign proctor");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("export-analytics")]
        public async Task<IActionResult> ExportAnalytics([FromQuery] string format = "json")
        {
            try
            {
                var analytics = await GetAnalyticsData();

                if (format.ToLower() == "csv")
                {
                    return Ok("CSV export would be implemented here");
                }

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export analytics");
                return BadRequest(new { Error = ex.Message });
            }
        }

        private async Task<object> GetAnalyticsData()
        {
            var sessions = await _unitOfWork.Repository<ExamSession>().GetAllAsync();
            var aiLogs = await _unitOfWork.Repository<AILog>().GetAllAsync();

            return new
            {
                TotalSessions = sessions.Count(),
                TotalAlerts = aiLogs.Count(),
                CompletionRate = sessions.Count(s => s.Status == "Completed") * 100.0 / sessions.Count()
            };
        }
    }
}
