using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    public class ExamController : ControllerBase
    {
        private readonly IExamService _examService;
        private readonly ILogger<ExamController> _logger;

        public ExamController(IExamService examService, ILogger<ExamController> logger)
        {
            _examService = examService;
            _logger = logger;
        }

        [Authorize(Roles = "Student")]
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableExams()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { Error = "User ID not found in token" });
                }

                var userId = int.Parse(userIdClaim);
                var exams = await _examService.GetAvailableExamsAsync(userId);
                return Ok(exams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve available exams");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [Authorize(Roles = "Doctor")]
        [HttpPost]
        public async Task<IActionResult> CreateExam([FromBody] CreateExamDto createExamDto)
        {
            try
            {
                var doctorId = GetCurrentUserId();
                _logger.LogInformation("Creating exam: {Title} by doctor: {DoctorId}", createExamDto.Title, doctorId);
                var exam = await _examService.CreateExamAsync(createExamDto, doctorId);
                _logger.LogInformation("Exam created successfully: {ExamId}", exam.Id);
                return CreatedAtAction(nameof(GetExamById), new { id = exam.Id }, exam);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create exam");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetExamById(int id)
        {
            try
            {
                var exam = await _examService.GetExamByIdAsync(id);
                return Ok(new { Success = true, Data = exam });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve exam: {ExamId}", id);
                return NotFound(new { Success = false, Error = ex.Message });
            }
        }

        [Authorize(Roles = "Doctor")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExam(int id, [FromBody] CreateExamDto updateExamDto)
        {
            try
            {
                await _examService.EnsureOwnerAsync(id, GetCurrentUserId());
                var exam = await _examService.UpdateExamAsync(id, updateExamDto);
                _logger.LogInformation("Exam updated successfully: {ExamId}", id);
                return Ok(exam);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update exam: {ExamId}", id);
                return BadRequest(new { Error = ex.Message });
            }
        }

        [Authorize(Roles = "Doctor")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> SetStatus(int id, [FromQuery] string status)
        {
            try
            {
                var exam = await _examService.SetExamStatusAsync(id, GetCurrentUserId(), status);
                return Ok(new { Success = true, Data = exam });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change exam status");
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [Authorize(Roles = "Doctor")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExam(int id)
        {
            try
            {
                await _examService.EnsureOwnerAsync(id, GetCurrentUserId());
                await _examService.DeleteExamAsync(id);
                _logger.LogInformation("Exam deleted successfully: {ExamId}", id);
                return Ok(new { Success = true, Message = $"Exam with ID {id} deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete exam: {ExamId}", id);
                return BadRequest(new { Error = ex.Message });
            }
        }

        [Authorize(Roles = "Doctor,Admin")]
        [HttpGet("{id}/questions")]
        public async Task<IActionResult> GetExamQuestions(int id)
        {
            try
            {
                var questions = await _examService.GetExamQuestionsAsync(id);
                return Ok(new { Success = true, Data = questions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve exam questions: {ExamId}", id);
                return NotFound(new { Success = false, Error = ex.Message });
            }
        }

        [Authorize(Roles = "Doctor")]
        [HttpPost("questions")]
        public async Task<IActionResult> AddQuestion([FromBody] CreateQuestionDto questionDto)
        {
            try
            {
                await _examService.EnsureOwnerAsync(questionDto.ExamId, GetCurrentUserId());
                var question = await _examService.AddQuestionAsync(questionDto);
                _logger.LogInformation("Question added successfully: {QuestionId}", question.Id);
                return CreatedAtAction(nameof(GetExamById), new { id = questionDto.ExamId }, question);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add question");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [Authorize(Roles = "Doctor")]
        [HttpDelete("questions/{questionId}")]
        public async Task<IActionResult> DeleteQuestion(int questionId)
        {
            try
            {
                await _examService.EnsureQuestionOwnerAsync(questionId, GetCurrentUserId());
                await _examService.DeleteQuestionAsync(questionId);
                _logger.LogInformation("Question deleted successfully: {QuestionId}", questionId);

                return Ok(new { Success = true, Message = $"Question with ID {questionId} deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete question: {QuestionId}", questionId);
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [Authorize(Roles = "Doctor")]
        [HttpGet("my-exams/{examId}/results")]
        public async Task<IActionResult> GetExamResults(int examId)
        {
            try
            {
                var results = await _examService.GetExamResultsAsync(examId, GetCurrentUserId());
                return Ok(new { Success = true, Data = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get exam results");
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [Authorize(Roles = "Doctor")]
        [HttpGet("my-exams/{examId}/analysis")]
        public async Task<IActionResult> GetExamAnalysis(int examId)
        {
            try
            {
                var analysis = await _examService.GetExamAnalysisAsync(examId, GetCurrentUserId());
                return Ok(new { Success = true, Data = analysis });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get exam analysis");
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [Authorize(Roles = "Doctor")]
        [HttpGet("sessions/{sessionId}/details")]
        public async Task<IActionResult> GetStudentExamDetails(int sessionId)
        {
            try
            {
                var details = await _examService.GetStudentExamDetailsAsync(sessionId, GetCurrentUserId());
                return Ok(new { Success = true, Data = details });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get student exam details");
                return BadRequest(new { Success = false, Error = ex.Message });
            }
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
    }
}