using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using TruthEye.DTOs;
using TruthEye.Middleware;
using TruthEye.Services;
using TruthEye.Swagger;
using TruthEye.Security;

namespace TruthEye.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Roles = AppRoles.FacultyStaff)]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly ILogger<CoursesController> _logger;

        public CoursesController(ICourseService courseService, ILogger<CoursesController> logger)
        {
            _courseService = courseService;
            _logger = logger;
        }

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        #region Course Management

        [HttpPost]
        [SwaggerOperation(Summary = "Create new course", OperationId = "CreateCourse")]
        [ProducesResponseType(typeof(ApiResponse<CourseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto createCourseDto)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var course = await _courseService.CreateCourseAsync(createCourseDto, adminId);

                return Ok(new ApiResponse<CourseDto>
                {
                    Success = true,
                    Message = "Course created successfully",
                    Data = course
                });
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(CreateCourse)); }
        }

        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Update course", OperationId = "UpdateCourse")]
        [ProducesResponseType(typeof(ApiResponse<CourseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto updateCourseDto)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var course = await _courseService.UpdateCourseAsync(id, updateCourseDto, adminId);

                return Ok(new ApiResponse<CourseDto>
                {
                    Success = true,
                    Message = "Course updated successfully",
                    Data = course
                });
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(UpdateCourse)); }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = AppRoles.Managers)]
        [SwaggerOperation(Summary = "Delete course", OperationId = "DeleteCourse")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var result = await _courseService.DeleteCourseAsync(id, adminId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Course deleted successfully",
                    Data = result
                });
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(DeleteCourse)); }
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get all faculty courses", OperationId = "GetFacultyCourses")]
        [ProducesResponseType(typeof(ApiResponse<List<CourseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFacultyCourses()
        {
            try
            {
                var adminId = GetCurrentUserId();
                var courses = await _courseService.GetFacultyCoursesAsync(adminId);

                return Ok(new ApiResponse<List<CourseDto>>
                {
                    Success = true,
                    Message = "Courses retrieved successfully",
                    Data = courses
                });
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(GetFacultyCourses)); }
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get course by ID", OperationId = "GetCourseById")]
        [ProducesResponseType(typeof(ApiResponse<CourseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCourseById(int id)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var course = await _courseService.GetCourseByIdAsync(id, adminId);

                return Ok(new ApiResponse<CourseDto>
                {
                    Success = true,
                    Message = "Course retrieved successfully",
                    Data = course
                });
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(GetCourseById)); }
        }

        #endregion

        #region Doctor Management in Courses

        [HttpPost("{courseId}/doctors/{doctorId}")]
        [SwaggerOperation(Summary = "Assign doctor to course", OperationId = "AssignDoctorToCourse")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AssignDoctorToCourse(int courseId, int doctorId)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var result = await _courseService.AssignDoctorToCourseAsync(courseId, doctorId, adminId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Doctor assigned to course successfully",
                    Data = result
                });
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(AssignDoctorToCourse)); }
        }

        [HttpDelete("{courseId}/doctors/{doctorId}")]
        [SwaggerOperation(Summary = "Remove doctor from course", OperationId = "RemoveDoctorFromCourse")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveDoctorFromCourse(int courseId, int doctorId)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var result = await _courseService.RemoveDoctorFromCourseAsync(courseId, doctorId, adminId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Doctor removed from course successfully",
                    Data = result
                });
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(RemoveDoctorFromCourse)); }
        }

        [HttpGet("{courseId}/doctors")]
        [SwaggerOperation(Summary = "Get course doctors", OperationId = "GetCourseDoctors")]
        [ProducesResponseType(typeof(ApiResponse<List<DoctorDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCourseDoctors(int courseId)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var doctors = await _courseService.GetCourseDoctorsAsync(courseId, adminId);

                return Ok(new ApiResponse<List<DoctorDto>>
                {
                    Success = true,
                    Message = "Course doctors retrieved successfully",
                    Data = doctors
                });
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(GetCourseDoctors)); }
        }

        #endregion

        #region Student Management in Courses

        [HttpPost("{courseId}/students/{studentId}")]
        [SwaggerOperation(Summary = "Assign student to course", OperationId = "AssignStudentToCourse")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AssignStudentToCourse(int courseId, int studentId)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var result = await _courseService.AssignStudentToCourseAsync(courseId, studentId, adminId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Student assigned to course successfully",
                    Data = result
                });
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(AssignStudentToCourse)); }
        }

        [HttpDelete("{courseId}/students/{studentId}")]
        [SwaggerOperation(Summary = "Remove student from course", OperationId = "RemoveStudentFromCourse")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveStudentFromCourse(int courseId, int studentId)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var result = await _courseService.RemoveStudentFromCourseAsync(courseId, studentId, adminId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Student removed from course successfully",
                    Data = result
                });
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(RemoveStudentFromCourse)); }
        }

        [HttpGet("{courseId}/students")]
        [SwaggerOperation(Summary = "Get course students", OperationId = "GetCourseStudents")]
        [ProducesResponseType(typeof(ApiResponse<List<StudentDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCourseStudents(int courseId)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var students = await _courseService.GetCourseStudentsAsync(courseId, adminId);

                return Ok(new ApiResponse<List<StudentDto>>
                {
                    Success = true,
                    Message = "Course students retrieved successfully",
                    Data = students
                });
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(GetCourseStudents)); }
        }

        #endregion

        #region Additional Endpoints

        [HttpGet("doctors/{doctorId}")]
        [SwaggerOperation(Summary = "Get doctor courses", OperationId = "GetDoctorCourses")]
        [ProducesResponseType(typeof(ApiResponse<List<CourseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDoctorCourses(int doctorId)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var courses = await _courseService.GetDoctorCoursesAsync(doctorId, adminId);

                return Ok(new ApiResponse<List<CourseDto>>
                {
                    Success = true,
                    Message = "Doctor courses retrieved successfully",
                    Data = courses
                });
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(GetDoctorCourses)); }
        }

        [HttpGet("students/{studentId}")]
        [SwaggerOperation(Summary = "Get student courses", OperationId = "GetStudentCourses")]
        [ProducesResponseType(typeof(ApiResponse<List<CourseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentCourses(int studentId)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var courses = await _courseService.GetStudentCoursesAsync(studentId, adminId);

                return Ok(new ApiResponse<List<CourseDto>>
                {
                    Success = true,
                    Message = "Student courses retrieved successfully",
                    Data = courses
                });
            }
            catch (Exception ex) { return ApiErrors.Result(ex, _logger, nameof(GetStudentCourses)); }
        }

        #endregion
    }
}