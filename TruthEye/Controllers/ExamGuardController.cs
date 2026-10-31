using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruthEye.Services;

namespace TruthEye.Controllers
{
    public class LeaveAnswerRequest
    {
        public int QuestionId { get; set; }
        public int? SelectedOptionId { get; set; }
        public string? AnswerText { get; set; }
    }

    public class TerminateRequest
    {
        public string? Reason { get; set; }
        public LeaveAnswerRequest? CurrentAnswer { get; set; }
    }

    [ApiController]
    [Route("api/ExamGuard")]
    [Authorize] // the service checks that the session belongs to the logged-in student
    public class ExamGuardController : ControllerBase
    {
        private readonly IExamSessionService _sessions;

        public ExamGuardController(IExamSessionService sessions)
        {
            _sessions = sessions;
        }

        // the first claim that holds a number is the user id (same token the other controllers use)
        private int? CurrentUserId()
        {
            foreach (var type in new[] { ClaimTypes.NameIdentifier, "sub", "nameid", "id", "userId" })
            {
                if (int.TryParse(User.FindFirstValue(type), out var id)) return id;
            }
            return null;
        }

        [HttpPost("{sessionId:int}/heartbeat")]
        public async Task<IActionResult> Heartbeat(int sessionId)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { success = false, error = "Invalid token" });

            var status = await _sessions.HeartbeatAsync(sessionId, userId.Value);
            return Ok(new { success = true, data = new { status } });
        }

        [HttpPost("{sessionId:int}/terminate")]
        public async Task<IActionResult> Terminate(int sessionId, [FromBody] TerminateRequest? request)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { success = false, error = "Invalid token" });

            var answer = request?.CurrentAnswer == null
                ? null
                : new LeaveAnswer(
                    request.CurrentAnswer.QuestionId,
                    request.CurrentAnswer.SelectedOptionId,
                    request.CurrentAnswer.AnswerText);

            var result = await _sessions.TerminateExamSessionAsync(
                sessionId, userId.Value, request?.Reason ?? "unknown", answer);

            return Ok(new { success = true, data = result });
        }
    }
}