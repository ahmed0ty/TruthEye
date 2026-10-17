//  جديد


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TruthEye.Entities;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Doctor")]
    public class DoctorPortalController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DoctorPortalController> _logger;

        public DoctorPortalController(IUnitOfWork unitOfWork, ILogger<DoctorPortalController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private int CurrentUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet("my-courses")]
        public async Task<IActionResult> MyCourses()
        {
            try
            {
                var doctor = await _unitOfWork.Repository<Doctor>()
    .GetByIdAsync(CurrentUserId(), d => d.TaughtCourses);

                if (doctor == null)
                    return NotFound(new { success = false, error = "Doctor not found" });

                var data = (doctor.TaughtCourses ?? new List<Course>()).Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description
                });

                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load doctor courses");
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("my-exams")]
        public async Task<IActionResult> MyExams()
        {
            try
            {
                var doctorId = CurrentUserId();
                var exams = await _unitOfWork.Repository<Exam>()
                    .FindAsync(e => e.DoctorId == doctorId, e => e.Course, e => e.Questions);

                var data = exams.Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Duration,
                    e.MaxAttempts,
                    e.StartTime,
                    e.EndTime,
                    e.Type,
                    e.RandomizeQuestions,
                    e.RandomizeOptions,
                    e.PassingScore,
                    e.TotalMarks,
                    e.Instructions,
                    e.CourseId,
                    CourseName = e.Course != null ? e.Course.Name : null,
                    e.Status,
                    QuestionsCount = e.Questions != null ? e.Questions.Count : 0
                });

                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load doctor exams");
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
    }
}