using TruthEye.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TruthEye.Entities;
using TruthEye.Services;
using TruthEye.UnitOfWorkk;
using TruthEye.Security;
using TruthEye.Entities.Data;
namespace TruthEye.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AILogController : ControllerBase
    {
        private static readonly HashSet<string> KnownEvents = new()
        {
            "TabSwitch", "WindowBlur", "FullscreenExit", "CopyPaste",
            "ContextMenu", "NoFace", "MultipleFaces", "ConnectionLost","PhoneDetected", "PaperDetected"
        };
        private const int MaxEvidencePerSession = 40;

        private readonly IAILogService _aiLogService;
        private readonly IExamSessionService _sessionService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEvidenceStore _store;
        private readonly ILogger<AILogController> _logger;

        public AILogController(
            IAILogService aiLogService,
            IExamSessionService sessionService,
            IUnitOfWork unitOfWork,
            IEvidenceStore store,
            ILogger<AILogController> logger)
        {
            _aiLogService = aiLogService;
            _sessionService = sessionService;
            _unitOfWork = unitOfWork;
            _store = store;
            _logger = logger;
        }

        private int CurrentUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Proctors and admins see every session; a doctor only sees sessions of their own exams
       // دكتور: امتحاناته بس. مراقب / عميد / أمين / تيم فني: جلسات طلاب كليتهم بس
private async Task<bool> CanViewSession(int sessionId)
{
    var s = await _unitOfWork.Repository<ExamSession>()
        .GetByIdAsync(sessionId, x => x.Exam!, x => x.Student!);
    if (s?.Exam == null || s.Student == null) return false;

    var me = CurrentUserId();
    var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

    if (role == "Doctor") return s.Exam.DoctorId == me;

    if (role == "Proctor")
    {
        var p = await _unitOfWork.Repository<Proctor>().GetByIdAsync(me);
        return p is { IsActive: true } && p.FacultyId == s.Student.FacultyId;
    }

    var a = await _unitOfWork.Repository<Admin>().GetByIdAsync(me);
    return a is { IsActive: true } && a.FacultyId == s.Student.FacultyId;
}

        [Authorize(Roles = "Student")]
        [HttpPost]
        public async Task<IActionResult> LogAIEvent(
            [FromQuery] int sessionId,
            [FromQuery] string eventType,
            [FromQuery] double confidence,
            [FromQuery] string? screenshotPath)
        {
            try
            {
                if (!KnownEvents.Contains(eventType))
                    return BadRequest(new { success = false, error = "Unknown event type" });

                var session = await _sessionService.GetExamSessionByIdAsync(sessionId);
                if (session.StudentId != CurrentUserId() || session.Status != "InProgress")
                    return Forbid();

                // The client's screenshotPath is ignored on purpose
                await _aiLogService.LogEventAsync(sessionId, eventType, Math.Clamp(confidence, 0, 1), "");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log AI event");
                return ApiErrors.Result(ex, _logger, "LogAIEvent");
            }
        }

        [Authorize(Roles = "Student")]
        [HttpPost("evidence")]
        [RequestSizeLimit(1_500_000)]
        public async Task<IActionResult> UploadEvidence(
            [FromForm] int sessionId,
            [FromForm] string eventType,
            [FromForm] double confidence,
            IFormFile? file)
        {
            try
            {
                if (!KnownEvents.Contains(eventType))
                    return BadRequest(new { success = false, error = "Unknown event type" });
                if (file == null || file.Length == 0 || file.Length > 1_000_000)
                    return BadRequest(new { success = false, error = "Invalid image" });

                var session = await _sessionService.GetExamSessionByIdAsync(sessionId);
                if (session.StudentId != CurrentUserId() || session.Status != "InProgress")
                    return Forbid();

                var existing = await _aiLogService.GetSessionAILogsAsync(sessionId);
                if (existing.Count(l => !string.IsNullOrEmpty(l.ScreenshotPath)) >= MaxEvidencePerSession)
                    return BadRequest(new { success = false, error = "Evidence limit reached" });

                // JPEG only, decided from the first bytes and not from the file name
                var head = new byte[3];
                await using (var probe = file.OpenReadStream())
                {
                    if (await probe.ReadAsync(head, 0, 3) < 3 || head[0] != 0xFF || head[1] != 0xD8 || head[2] != 0xFF)
                        return BadRequest(new { success = false, error = "Only JPEG images are accepted" });
                }

                var name = $"{Guid.NewGuid():N}.jpg";
                await using (var content = file.OpenReadStream())
                    await _store.SaveAsync(name, content);

                try
                {
                    await _aiLogService.LogEventAsync(sessionId, eventType, Math.Clamp(confidence, 0, 1), name);
                }
                catch
                {
                    await _store.DeleteAsync(name); // don't leave an orphan photo behind
                    throw;
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save evidence");
                return BadRequest(new { success = false, error = "Couldn't save the evidence" });
            }
        }

        [Authorize(Roles = "Proctor,Doctor," + AppRoles.FacultyStaff)]
        [HttpGet("evidence/{name}")]
        public async Task<IActionResult> GetEvidence(string name)
        {
            if (!EvidenceName.IsValid(name)) return NotFound();

            var log = (await _unitOfWork.Repository<AILog>()
                .FindAsync(l => l.ScreenshotPath == name)).FirstOrDefault();
            if (log == null || !await CanViewSession(log.ExamSessionId)) return NotFound();

            var stream = await _store.OpenReadAsync(name);
            if (stream == null) return NotFound();

            Response.Headers.CacheControl = "private, max-age=3600";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(stream, "image/jpeg");
        }

        [Authorize(Roles = "Proctor,Doctor," + AppRoles.FacultyStaff)]
        [HttpGet("session/{sessionId}")]
        public async Task<IActionResult> GetSessionLogs(int sessionId)
        {
            try
            {
                if (!await CanViewSession(sessionId)) return Forbid();
                return Ok(await _aiLogService.GetSessionAILogsAsync(sessionId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve AI logs for session: {SessionId}", sessionId);
                return ApiErrors.Result(ex, _logger, "GetSessionLogs");
            }
        }

       [Authorize(Roles = "Proctor," + AppRoles.FacultyStaff)]
[HttpGet("flagged")]
public async Task<IActionResult> GetFlaggedLogs([FromQuery] double threshold = 0.7)
{
    try
    {
        var me = CurrentUserId();
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        int? faculty = role == "Proctor"
            ? (await _unitOfWork.Repository<Proctor>().GetByIdAsync(me))?.FacultyId
            : (await _unitOfWork.Repository<Admin>().GetByIdAsync(me))?.FacultyId;

        if (faculty == null) return Forbid();

        var logs = await _aiLogService.GetFlaggedLogsAsync(threshold);

        // نفس الفلتر: بس سجلات طلاب كليتي
        var sessionIds = logs.Select(l => l.ExamSessionId).Distinct().ToList();
        var allowed = (await _unitOfWork.Repository<ExamSession>()
            .FindAsync(s => sessionIds.Contains(s.Id) && s.Student!.FacultyId == faculty,
                s => s.Student!))
            .Select(s => s.Id).ToHashSet();

        return Ok(logs.Where(l => allowed.Contains(l.ExamSessionId)));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to retrieve flagged AI logs");
        return BadRequest(new { Error = "Couldn't load flagged logs" });
    }
}
    }
}