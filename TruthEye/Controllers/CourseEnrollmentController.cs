using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TruthEye.Middleware;
using TruthEye.Security;
using TruthEye.Services;

namespace TruthEye.Controllers
{
    [ApiController]
    [Route("api/Courses/{courseId:int}/students")]
    [Authorize(Roles = AppRoles.FacultyStaff)]
    public class CourseEnrollmentController : ControllerBase
    {
        private readonly ICourseEnrollmentService _enrollment;
        private readonly ILogger<CourseEnrollmentController> _logger;

        public CourseEnrollmentController(ICourseEnrollmentService enrollment, ILogger<CourseEnrollmentController> logger)
        {
            _enrollment = enrollment;
            _logger = logger;
        }

        private int Me => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpPost("bulk")]
        public async Task<IActionResult> Enroll(int courseId, [FromBody] BulkEnrollRequest request)
        {
            try
            {
                var nothing = (request.StudentIds?.Count ?? 0) == 0
                              && (request.Identifiers?.Count ?? 0) == 0
                              && !request.DepartmentId.HasValue
                              && !request.Year.HasValue;
                if (nothing)
                    return BadRequest(new { success = false, error = "Select at least one student." });

                var result = await _enrollment.EnrollAsync(courseId, request, Me);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(Enroll)); }
        }

        [HttpPost("bulk-remove")]
        public async Task<IActionResult> Unenroll(int courseId, [FromBody] List<int> studentIds)
        {
            try
            {
                if (studentIds == null || studentIds.Count == 0)
                    return BadRequest(new { success = false, error = "Select at least one student." });

                var removed = await _enrollment.UnenrollAsync(courseId, studentIds, Me);
                return Ok(new { success = true, data = new { removed } });
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(Unenroll)); }
        }
    }
}