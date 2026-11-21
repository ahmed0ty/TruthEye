using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.Security;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Proctor," + AppRoles.FacultyStaff)]
    public class DirectorController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DirectorController> _logger;

        public DirectorController(IUnitOfWork unitOfWork, ILogger<DirectorController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private int Me => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        private string Role => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        /// كلية المستخدم الحالي (مراقب أو عميد/أمين/تيم فني). null = مفيش كلية.
        private async Task<int?> MyFacultyAsync()
        {
            if (Role == "Proctor")
            {
                var p = await _unitOfWork.Repository<Proctor>().GetByIdAsync(Me);
                return p is { IsActive: true } ? p.FacultyId : null;
            }
            var a = await _unitOfWork.Repository<Admin>().GetByIdAsync(Me);
            return a is { IsActive: true } ? a.FacultyId : null;
        }

        private IActionResult NoFaculty() =>
            StatusCode(403, new { Error = "No faculty is assigned to your account" });

        [HttpGet("active-sessions")]
        public async Task<IActionResult> GetAllActiveSessions()
        {
            try
            {
                var fid = await MyFacultyAsync();
                if (fid == null) return NoFaculty();

                var sessions = await _unitOfWork.Repository<ExamSession>()
                    .FindAsync(es => es.Status == "InProgress" && es.Student!.FacultyId == fid.Value,
                        es => es.Student!, es => es.Exam!, es => es.AILogs);

                return Ok(sessions.Select(s => new
                {
                    s.Id,
                    StudentName = $"{s.Student!.FirstName} {s.Student.LastName}",
                    ExamTitle = s.Exam!.Title,
                    s.StartTime,
                    Duration = DateTime.UtcNow - s.StartTime,
                    s.AIScore,
                    AlertsCount = s.AILogs?.Count ?? 0
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve active sessions");
                return BadRequest(new { Error = "Couldn't load active sessions" });
            }
        }

        [HttpGet("ai-alerts")]
        public async Task<IActionResult> GetAIAlerts([FromQuery] double? minConfidence = null, [FromQuery] string? eventType = null)
        {
            try
            {
                var fid = await MyFacultyAsync();
                if (fid == null) return NoFaculty();

                var logs = await _unitOfWork.Repository<AILog>()
                    .FindAsync(l => l.ExamSession.Student!.FacultyId == fid.Value
                                    && (minConfidence == null || l.Confidence >= minConfidence)
                                    && (string.IsNullOrEmpty(eventType) || l.EventType == eventType),
                        l => l.ExamSession);

                var grouped = logs.GroupBy(l => l.EventType)
                    .Select(g => new
                    {
                        EventType = g.Key,
                        Count = g.Count(),
                        AverageConfidence = g.Average(l => l.Confidence),
                        RecentAlerts = g.OrderByDescending(l => l.Timestamp).Take(10)
                            .Select(l => new { l.Id, l.ExamSessionId, l.EventType, l.Confidence, l.Timestamp })
                    });

                return Ok(grouped);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve AI alerts");
                return BadRequest(new { Error = "Couldn't load alerts" });
            }
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var fid = await MyFacultyAsync();
                if (fid == null) return NoFaculty();

                var from = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var to = endDate ?? DateTime.UtcNow;

                var exams = await _unitOfWork.Repository<Exam>()
                    .FindAsync(e => e.CreatedAt >= from && e.CreatedAt <= to
                                    && e.Course.Department.FacultyId == fid.Value);

                var sessions = (await _unitOfWork.Repository<ExamSession>()
                    .FindAsync(es => es.StartTime >= from && es.StartTime <= to
                                     && es.Student!.FacultyId == fid.Value)).ToList();

                var aiLogs = (await _unitOfWork.Repository<AILog>()
                    .FindAsync(ai => ai.Timestamp >= from && ai.Timestamp <= to
                                     && ai.ExamSession.Student!.FacultyId == fid.Value)).ToList();

                var scored = sessions.Where(s => s.AIScore.HasValue).ToList();

                return Ok(new
                {
                    TotalExams = exams.Count(),
                    TotalSessions = sessions.Count,
                    CompletedSessions = sessions.Count(s => s.Status == "Completed"),
                    AverageAIScore = scored.Count > 0 ? scored.Average(s => s.AIScore) : null,
                    TotalAIAlerts = aiLogs.Count,
                    HighRiskSessions = sessions.Count(s => s.AIScore < 50),
                    AlertsByType = aiLogs.GroupBy(l => l.EventType)
                        .Select(g => new { Type = g.Key, Count = g.Count() }),
                    SessionsByHour = sessions.GroupBy(s => s.StartTime.Hour)
                        .Select(g => new { Hour = g.Key, Count = g.Count() })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve analytics");
                return BadRequest(new { Error = "Couldn't load analytics" });
            }
        }

        [HttpGet("performance-reports")]
        public async Task<IActionResult> GetPerformanceReports([FromQuery] int? courseId = null)
        {
            try
            {
                var fid = await MyFacultyAsync();
                if (fid == null) return NoFaculty();

                var reports = (await _unitOfWork.Repository<PerformanceReport>()
                    .FindAsync(r => r.Exam.Course.Department.FacultyId == fid.Value
                                    && (courseId == null || r.Exam.CourseId == courseId))).ToList();

                return Ok(new
                {
                    TotalReports = reports.Count,
                    AverageScore = reports.Count > 0 ? reports.Average(r => r.Score) : 0,
                    AverageAIAlerts = reports.Count > 0 ? reports.Average(r => r.AIAlertsCount) : 0,
                    TopPerformers = reports.OrderByDescending(r => r.Score).Take(10),
                    AtRiskStudents = reports.Where(r => r.AIAlertsCount > 5 || r.Score < 50).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve performance reports");
                return BadRequest(new { Error = "Couldn't load reports" });
            }
        }

        /// تعيين مراقب لامتحان: لازم الاتنين في نفس كلية المستخدم. للعميد والأمين بس، التيم الفني لا يعيّن.
        [Authorize(Roles = AppRoles.Managers)]
        [HttpPost("assign-proctor")]
        public async Task<IActionResult> AssignProctor([FromQuery] int examId, [FromQuery] int proctorId)
        {
            try
            {
                var fid = await MyFacultyAsync();
                if (fid == null) return NoFaculty();

                var exam = await _unitOfWork.Repository<Exam>()
                    .GetByIdAsync(examId, e => e.Course, e => e.Course.Department);
                var proctor = await _unitOfWork.Repository<Proctor>()
                    .GetByIdAsync(proctorId, p => p.AssignedExams);

                if (exam == null || proctor == null
                    || exam.Course.Department.FacultyId != fid.Value
                    || proctor.FacultyId != fid.Value)
                    return NotFound(new { Error = "Exam or Proctor not found" });

                if (proctor.AssignedExams.Any(e => e.Id == examId))
                    return BadRequest(new { Error = "Proctor is already assigned to this exam" });

                proctor.AssignedExams.Add(exam);
                await _unitOfWork.Repository<Proctor>().UpdateAsync(proctor);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Proctor {ProctorId} assigned to exam {ExamId} by {ActorId}", proctorId, examId, Me);
                return Ok(new { Message = "Proctor assigned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign proctor");
                return BadRequest(new { Error = "Couldn't assign the proctor" });
            }
        }

        [HttpGet("export-analytics")]
        public async Task<IActionResult> ExportAnalytics([FromQuery] string format = "json")
        {
            try
            {
                var fid = await MyFacultyAsync();
                if (fid == null) return NoFaculty();

                var sessions = (await _unitOfWork.Repository<ExamSession>()
                    .FindAsync(s => s.Student!.FacultyId == fid.Value)).ToList();
                var alerts = await _unitOfWork.Repository<AILog>()
                    .CountAsync(l => l.ExamSession.Student!.FacultyId == fid.Value);

                var completionRate = sessions.Count > 0
                    ? sessions.Count(s => s.Status == "Completed") * 100.0 / sessions.Count
                    : 0;

                if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                {
                    var csv = "Metric,Value\n"
                        + $"TotalSessions,{sessions.Count}\n"
                        + $"TotalAlerts,{alerts}\n"
                        + $"CompletionRate,{completionRate.ToString("0.##", CultureInfo.InvariantCulture)}\n";
                    return File(Encoding.UTF8.GetBytes(csv), "text/csv", "analytics.csv");
                }

                return Ok(new
                {
                    TotalSessions = sessions.Count,
                    TotalAlerts = alerts,
                    CompletionRate = completionRate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export analytics");
                return BadRequest(new { Error = "Couldn't export analytics" });
            }
        }
    }
}