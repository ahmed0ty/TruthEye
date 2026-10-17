// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using System.Security.Claims;
// using TruthEye.DTOs;
// using TruthEye.Entities;
// using TruthEye.Services;

// namespace TruthEye.Controllers
// {
//     [Route("api/[controller]")]
//     [ApiController]
//     [Authorize]
//     public class ExamSessionController : ControllerBase
//     {
//         private readonly IExamSessionService _sessionService;
//         private readonly IExamService _examService;
//         private readonly ILogger<ExamSessionController> _logger;

//         public ExamSessionController(
//             IExamSessionService sessionService,
//             IExamService examService, 
//             ILogger<ExamSessionController> logger)
//         {
//             _sessionService = sessionService;
//             _examService = examService;
//             _logger = logger;
//         }

//         private int GetCurrentUserId()
//         {
//             var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//             if (string.IsNullOrEmpty(userIdClaim))
//             {
//                 throw new UnauthorizedAccessException("User ID not found");
//             }
//             return int.Parse(userIdClaim);
//         }

//         [Authorize(Roles = "Student")]
//         [HttpPost("start")]
//         public async Task<IActionResult> StartExam([FromBody] StartExamSessionDto startDto)
//         {
//             try
//             {
//                 var userId = GetCurrentUserId();
//                 startDto.StudentId = userId;

//                 _logger.LogInformation("Student {StudentId} starting exam {ExamId}", userId, startDto.ExamId);

//                 var existingSessions = await _sessionService.GetStudentExamHistoryAsync(userId);
//                 var alreadyAttempted = existingSessions.Any(s => s.ExamId == startDto.ExamId);

//                 if (alreadyAttempted)
//                 {
//                     return BadRequest(new
//                     {
//                         Success = false,
//                         Error = "You have already attempted this exam. Only one attempt is allowed."
//                     });
//                 }

//                 var session = await _sessionService.StartExamSessionAsync(startDto);
//                 _logger.LogInformation("Exam session started: {SessionId}", session.Id);

//                 return Ok(new { Success = true, Data = session, Message = "Exam session started successfully" });
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Failed to start exam session");
//                 return BadRequest(new { Success = false, Error = ex.Message });
//             }
//         }
//         [Authorize(Roles = "Student")]
//         [HttpPost("{sessionId}/next")]
//         public async Task<IActionResult> MoveToNextQuestion(int sessionId, [FromBody] NextQuestionRequest request)
//         {
//             try
//             {
//                 var userId = GetCurrentUserId();
//                 var session = await _sessionService.GetExamSessionByIdAsync(sessionId);

//                 if (session.StudentId != userId)
//                     return Unauthorized(new { Success = false, Error = "Access denied" });

//                 var navigation = await _sessionService.MoveToNextQuestionAsync(
//                     sessionId, request.CurrentQuestionId, request.Answer);

//                 return Ok(new { Success = true, Data = navigation });
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Failed to move to next question");
//                 return BadRequest(new { Success = false, Error = ex.Message });
//             }
//         }
//         [Authorize(Roles = "Doctor")]
       
//         [Authorize(Roles = "Student")]
//         [HttpGet("{sessionId}/continue")]
//         public async Task<IActionResult> ContinueExam(int sessionId)
//         {
//             try
//             {
//                 var userId = GetCurrentUserId();
//                 var session = await _sessionService.ContinueExamSessionAsync(sessionId);

//                 if (session.StudentId != userId)
//                     return Unauthorized(new { Success = false, Error = "Access denied" });

//                 return Ok(new { Success = true, Data = session, Message = "Exam session continued successfully" });
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Failed to continue exam session: {SessionId}", sessionId);
//                 return BadRequest(new { Success = false, Error = ex.Message });
//             }
//         }
//         [Authorize(Roles = "Student")]
//         [HttpPost("{sessionId}/submit")]
//         public async Task<IActionResult> SubmitExam(int sessionId)
//         {
//             try
//             {
//                 var userId = GetCurrentUserId();
//                 var session = await _sessionService.GetExamSessionByIdAsync(sessionId);

//                 if (session.StudentId != userId)
//                     return Unauthorized(new { Success = false, Error = "Access denied" });

//                 var submitDto = new SubmitExamDto
//                 {
//                     ExamSessionId = sessionId
//                 };

//                 var result = await _sessionService.SubmitExamSessionAsync(sessionId);

//                 return Ok(new
//                 {
//                     Success = true,
//                     Data = result,
//                     Message = "Exam submitted successfully. Your result is ready."
//                 });
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Failed to submit exam");
//                 return BadRequest(new { Success = false, Error = ex.Message });
//             }
//         }
//         [Authorize(Roles = "Student")]
//         [HttpGet("{sessionId}/result")]
//         public async Task<IActionResult> GetExamResult(int sessionId)
//         {
//             try
//             {
//                 var userId = GetCurrentUserId();
//                 var session = await _sessionService.GetExamSessionByIdAsync(sessionId);

//                 if (session?.StudentId != userId)
//                     return Unauthorized(new { Success = false, Error = "Access denied" });

//                 var result = await _sessionService.GetExamResultAsync(sessionId);
//                 return Ok(new { Success = true, Data = result, Message = "Exam result retrieved successfully" });
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Failed to retrieve exam result for session {SessionId}", sessionId);
//                 return BadRequest(new { Success = false, Error = ex.Message });
//             }
//         }

//         [Authorize(Roles = "Student")]
//         [HttpGet("{sessionId}/answers")]
//         public async Task<IActionResult> GetExamAnswers(int sessionId)
//         {
//             try
//             {
//                 var userId = GetCurrentUserId();
//                 var session = await _sessionService.GetExamSessionByIdAsync(sessionId);

//                 if (session.StudentId != userId)
//                     return Unauthorized(new { Success = false, Error = "Access denied" });

//                 var exam = await _examService.GetExamByIdAsync(session.ExamId);

//                 bool canViewAnswers = DateTime.UtcNow > exam.EndTime;

//                 if (!canViewAnswers)
//                 {
//                     return Ok(new
//                     {
//                         Success = true,
//                         Data = new { CanViewAnswers = false },
//                         Message = "Answers will be available after the exam time has ended."
//                     });
//                 }

//                 var answers = await _sessionService.GetExamAnswersAsync(sessionId);
//                 return Ok(new { Success = true, Data = answers });
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Failed to retrieve exam answers");
//                 return BadRequest(new { Success = false, Error = ex.Message });
//             }
//         }

//         [Authorize(Roles = "Student")]
//         [HttpGet("history")]
//         public async Task<IActionResult> GetExamHistory()
//         {
//             try
//             {
//                 var userId = GetCurrentUserId();
//                 var history = await _sessionService.GetStudentExamHistoryAsync(userId);
//                 return Ok(new { Success = true, Data = history, Message = "Exam history retrieved successfully" });
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Failed to retrieve exam history for user {UserId}", GetCurrentUserId());
//                 return BadRequest(new { Success = false, Error = ex.Message });
//             }
//         }

//         [Authorize(Roles = "Proctor,Admin")]
//         [HttpGet("active")]
//         public async Task<IActionResult> GetActiveSessions()
//         {
//             try
//             {
//                 var sessions = await _sessionService.GetActiveSessionsAsync();
//                 return Ok(new { Success = true, Data = sessions, Message = "Active sessions retrieved successfully" });
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Failed to retrieve active sessions");
//                 return BadRequest(new { Success = false, Error = ex.Message });
//             }
//         }

//         [Authorize(Roles = "Student")]
//         [HttpGet("{sessionId}/summary")]
//         public async Task<IActionResult> GetExamSummary(int sessionId)
//         {
//             try
//             {
//                 var userId = GetCurrentUserId();
//                 var session = await _sessionService.GetExamSessionByIdAsync(sessionId);

//                 if (session?.StudentId != userId)
//                     return Unauthorized(new { Success = false, Error = "Access denied" });

//                 return Ok(new { Success = true, Data = session, Message = "Exam summary retrieved successfully" });
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Failed to retrieve exam summary for session {SessionId}", sessionId);
//                 return BadRequest(new { Success = false, Error = ex.Message });
//             }
//         }
//     }

  
// }

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.Services;

namespace TruthEye.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExamSessionController : ControllerBase
    {
        private readonly IExamSessionService _sessionService;
        private readonly IExamService _examService;
        private readonly ILogger<ExamSessionController> _logger;

        public ExamSessionController(
            IExamSessionService sessionService,
            IExamService examService,
            ILogger<ExamSessionController> logger)
        {
            _sessionService = sessionService;
            _examService = examService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new UnauthorizedAccessException("User ID not found");
            }
            return int.Parse(userIdClaim);
        }

        [Authorize(Roles = "Student")]
        [HttpPost("start")]
        public async Task<IActionResult> StartExam([FromBody] StartExamSessionDto startDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                startDto.StudentId = userId;

                _logger.LogInformation("Student {StudentId} starting exam {ExamId}", userId, startDto.ExamId);

                var existingSessions = await _sessionService.GetStudentExamHistoryAsync(userId);
                var alreadyAttempted = existingSessions.Any(s => s.ExamId == startDto.ExamId);

                if (alreadyAttempted)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Error = "You have already attempted this exam. Only one attempt is allowed."
                    });
                }

                var session = await _sessionService.StartExamSessionAsync(startDto);
                _logger.LogInformation("Exam session started: {SessionId}", session.Id);

                return Ok(new { Success = true, Data = session, Message = "Exam session started successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start exam session");
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [Authorize(Roles = "Student")]
        [HttpGet("{sessionId}/take")]
        public async Task<IActionResult> Take(int sessionId)
        {
            try
            {
                var data = await _sessionService.GetExamTakeAsync(sessionId, GetCurrentUserId());
                return Ok(new { Success = true, Data = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load exam for session {SessionId}", sessionId);
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [Authorize(Roles = "Student")]
        [HttpPost("{sessionId}/next")]
        public async Task<IActionResult> MoveToNextQuestion(int sessionId, [FromBody] NextQuestionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var session = await _sessionService.GetExamSessionByIdAsync(sessionId);

                if (session.StudentId != userId)
                    return Unauthorized(new { Success = false, Error = "Access denied" });

                var navigation = await _sessionService.MoveToNextQuestionAsync(
                    sessionId, request.CurrentQuestionId, request.Answer);

                return Ok(new { Success = true, Data = navigation });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move to next question");
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [Authorize(Roles = "Student")]
        [HttpGet("{sessionId}/continue")]
        public async Task<IActionResult> ContinueExam(int sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var session = await _sessionService.ContinueExamSessionAsync(sessionId);

                if (session.StudentId != userId)
                    return Unauthorized(new { Success = false, Error = "Access denied" });

                return Ok(new { Success = true, Data = session, Message = "Exam session continued successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to continue exam session: {SessionId}", sessionId);
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [Authorize(Roles = "Student")]
        [HttpPost("{sessionId}/submit")]
        public async Task<IActionResult> SubmitExam(int sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var session = await _sessionService.GetExamSessionByIdAsync(sessionId);

                if (session.StudentId != userId)
                    return Unauthorized(new { Success = false, Error = "Access denied" });

                var result = await _sessionService.SubmitExamSessionAsync(sessionId);

                return Ok(new
                {
                    Success = true,
                    Data = result,
                    Message = "Exam submitted successfully. Your result is ready."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit exam");
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [Authorize(Roles = "Student")]
        [HttpGet("{sessionId}/result")]
        public async Task<IActionResult> GetExamResult(int sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var session = await _sessionService.GetExamSessionByIdAsync(sessionId);

                if (session?.StudentId != userId)
                    return Unauthorized(new { Success = false, Error = "Access denied" });

                var result = await _sessionService.GetExamResultAsync(sessionId);
                return Ok(new { Success = true, Data = result, Message = "Exam result retrieved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve exam result for session {SessionId}", sessionId);
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [Authorize(Roles = "Student")]
        [HttpGet("{sessionId}/answers")]
        public async Task<IActionResult> GetExamAnswers(int sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var session = await _sessionService.GetExamSessionByIdAsync(sessionId);

                if (session.StudentId != userId)
                    return Unauthorized(new { Success = false, Error = "Access denied" });

                var exam = await _examService.GetExamByIdAsync(session.ExamId);

                bool canViewAnswers = DateTime.UtcNow > exam.EndTime;

                if (!canViewAnswers)
                {
                    return Ok(new
                    {
                        Success = true,
                        Data = new { CanViewAnswers = false },
                        Message = "Answers will be available after the exam time has ended."
                    });
                }

                var answers = await _sessionService.GetExamAnswersAsync(sessionId);
                return Ok(new { Success = true, Data = answers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve exam answers");
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [Authorize(Roles = "Student")]
        [HttpGet("history")]
        public async Task<IActionResult> GetExamHistory()
        {
            try
            {
                var userId = GetCurrentUserId();
                var history = await _sessionService.GetStudentExamHistoryAsync(userId);
                return Ok(new { Success = true, Data = history, Message = "Exam history retrieved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve exam history for user {UserId}", GetCurrentUserId());
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [Authorize(Roles = "Proctor,Admin")]
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveSessions()
        {
            try
            {
                var sessions = await _sessionService.GetActiveSessionsAsync();
                return Ok(new { Success = true, Data = sessions, Message = "Active sessions retrieved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve active sessions");
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [Authorize(Roles = "Student")]
        [HttpGet("{sessionId}/summary")]
        public async Task<IActionResult> GetExamSummary(int sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var session = await _sessionService.GetExamSessionByIdAsync(sessionId);

                if (session?.StudentId != userId)
                    return Unauthorized(new { Success = false, Error = "Access denied" });

                return Ok(new { Success = true, Data = session, Message = "Exam summary retrieved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve exam summary for session {SessionId}", sessionId);
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }
    }
}