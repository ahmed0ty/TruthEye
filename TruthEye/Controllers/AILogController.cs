using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TruthEye.Entities;
using TruthEye.Services;
using TruthEye.UnitOfWorkk;

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
            "ContextMenu", "NoFace", "MultipleFaces", "ConnectionLost","PhoneDetected"
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
        private async Task<bool> CanViewSession(int sessionId)
        {
            if (User.IsInRole("Proctor") || User.IsInRole("Admin")) return true;

            var s = await _unitOfWork.Repository<ExamSession>().GetByIdAsync(sessionId, x => x.Exam!);
            return s?.Exam != null && s.Exam.DoctorId == CurrentUserId();
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
                return BadRequest(new { success = false, error = ex.Message });
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

        [Authorize(Roles = "Proctor,Doctor,Admin")]
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
            return File(stream, "image/jpeg");
        }

        [Authorize(Roles = "Proctor,Doctor,Admin")]
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
                return BadRequest(new { Error = ex.Message });
            }
        }

        [Authorize(Roles = "Proctor,Admin")]
        [HttpGet("flagged")]
        public async Task<IActionResult> GetFlaggedLogs([FromQuery] double threshold = 0.7)
        {
            try
            {
                return Ok(await _aiLogService.GetFlaggedLogsAsync(threshold));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve flagged AI logs");
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}