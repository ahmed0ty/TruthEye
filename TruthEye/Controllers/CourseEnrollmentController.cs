using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruthEye.Services;
using TruthEye.Security;
namespace TruthEye.Controllers
{
    [ApiController]
    [Route("api/Courses/{courseId:int}/students")]
    [Authorize(Roles = AppRoles.FacultyStaffAndSuper)] // use the same roles as CourseController
    public class CourseEnrollmentController : ControllerBase
    {
        private readonly ICourseEnrollmentService _enrollment;

        public CourseEnrollmentController(ICourseEnrollmentService enrollment)
        {
            _enrollment = enrollment;
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> Enroll(int courseId, [FromBody] BulkEnrollRequest request)
        {
            var nothing = (request.StudentIds?.Count ?? 0) == 0
                          && (request.Identifiers?.Count ?? 0) == 0
                          && !request.DepartmentId.HasValue
                          && !request.Year.HasValue;
            if (nothing)
                return BadRequest(new { success = false, error = "Select at least one student." });

            var result = await _enrollment.EnrollAsync(courseId, request);
            return Ok(new { success = true, data = result });
        }

        [HttpPost("bulk-remove")]
        public async Task<IActionResult> Unenroll(int courseId, [FromBody] List<int> studentIds)
        {
            if (studentIds == null || studentIds.Count == 0)
                return BadRequest(new { success = false, error = "Select at least one student." });

            var removed = await _enrollment.UnenrollAsync(courseId, studentIds);
            return Ok(new { success = true, data = new { removed } });
        }
    }
}