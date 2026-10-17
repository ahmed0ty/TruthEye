using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using TruthEye.DTOs;
using TruthEye.Services;
using TruthEye.Swagger;

namespace TruthEye.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        #region Authentication Endpoints

        [AllowAnonymous]
        [HttpPost("login")]
        [SwaggerOperation(
            Summary = "Authenticate user",
            Description = "Authenticates user with email/national ID and password, returns JWT access token and refresh token",
            OperationId = "Login"
        )]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                _logger.LogInformation("Login attempt for email/nationalId: {Email}/{NationalId}",
                    loginDto.Email, loginDto.NationalId);

                var tokens = await _authService.LoginAsync(loginDto);

                _logger.LogInformation("Login successful for: {Email}/{NationalId}",
                    loginDto.Email, loginDto.NationalId);

                return Ok(new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = new LoginResponse
                    {
                        Token = tokens.AccessToken,
                        RefreshToken = tokens.RefreshToken
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for: {Email}/{NationalId}",
                    loginDto.Email, loginDto.NationalId);

                return Unauthorized(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        [SwaggerOperation(
            Summary = "Refresh access token",
            Description = "Exchanges a valid refresh token for a new access token and refresh token",
            OperationId = "RefreshToken"
        )]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
        {
            try
            {
                var t = await _authService.RefreshAsync(req.RefreshToken);

                return Ok(new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Message = "Token refreshed",
                    Data = new LoginResponse
                    {
                        Token = t.AccessToken,
                        RefreshToken = t.RefreshToken
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh failed");
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = "Couldn't refresh the session"
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        [SwaggerOperation(
            Summary = "Sign out",
            Description = "Revokes the given refresh token",
            OperationId = "Logout"
        )]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest req)
        {
            await _authService.RevokeRefreshAsync(req.RefreshToken);

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "Signed out",
                Data = true
            });
        }

        [Authorize]
        [HttpGet("profile")]
        [SwaggerOperation(
            Summary = "Get current user profile",
            Description = "Retrieves the current authenticated user's profile information",
            OperationId = "GetProfile"
        )]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetUserByIdAsync(userId);

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "Profile retrieved successfully",
                    Data = user
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user profile for user {UserId}", GetCurrentUserId());
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("register/super-admin")]
        [SwaggerOperation(
            Summary = "Register super admin",
            Description = "Registers the system super admin (first time setup)",
            OperationId = "RegisterSuperAdmin"
        )]
        [ProducesResponseType(typeof(ApiResponse<AdminDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RegisterSuperAdmin([FromBody] RegisterUserDto registerDto)
        {
            try
            {
                if (await _authService.SuperAdminExistsAsync())
                {
                    return StatusCode(403, new ApiErrorResponse
                    {
                        Success = false,
                        Error = "Setup already completed. A super admin already exists."
                    });
                }

                var adminDto = await _authService.RegisterSuperAdminAsync(registerDto);

                _logger.LogInformation("Super admin registered with ID: {AdminId}", adminDto.Id);

                return CreatedAtAction(nameof(GetProfile), new { id = adminDto.Id }, new ApiResponse<AdminDto>
                {
                    Success = true,
                    Message = "Super admin registered successfully",
                    Data = adminDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register super admin");
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [HttpPost("register/admin")]
        [Authorize(Roles = "SuperAdmin")]
        [SwaggerOperation(
            Summary = "Register faculty admin",
            Description = "Registers a new faculty admin (SuperAdmin role required)",
            OperationId = "RegisterFacultyAdmin"
        )]
        [ProducesResponseType(typeof(ApiResponse<AdminDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RegisterFacultyAdmin([FromBody] RegisterAdminDto registerDto)
        {
            try
            {
                var adminDto = await _authService.RegisterFacultyAdminAsync(registerDto);

                _logger.LogInformation("Faculty admin registered with ID: {AdminId}", adminDto.Id);

                return CreatedAtAction(nameof(GetProfile), new { id = adminDto.Id }, new ApiResponse<AdminDto>
                {
                    Success = true,
                    Message = "Faculty admin registered successfully",
                    Data = adminDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register faculty admin");
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        #endregion

        #region Student Management Endpoints (Admin only)

        [HttpPost("register/student")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Register new student",
            Description = "Registers a new student in the current admin's faculty",
            OperationId = "RegisterStudent"
        )]
        [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RegisterStudent([FromBody] RegisterStudentDto registerDto)
        {
            try
            {
                var adminId = GetCurrentUserId();
                _logger.LogInformation("Student registration attempt by admin {AdminId} for email: {Email}",
                    adminId, registerDto.Email);

                var student = await _authService.RegisterStudentAsync(registerDto, adminId);

                _logger.LogInformation("Student registered successfully: {StudentId}", student.Id);

                return Ok(new ApiResponse<StudentDto>
                {
                    Success = true,
                    Message = "Student registered successfully",
                    Data = student
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Student registration failed");
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("students")]
        [SwaggerOperation(
            Summary = "Get all students in faculty",
            Description = "Retrieves all students in the current admin's faculty",
            OperationId = "GetFacultyStudents"
        )]
        [ProducesResponseType(typeof(ApiResponse<List<StudentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetStudents()
        {
            try
            {
                var adminId = GetCurrentUserId();
                var students = await _authService.GetFacultyStudentsAsync(adminId);

                return Ok(new ApiResponse<List<StudentDto>>
                {
                    Success = true,
                    Message = "Students retrieved successfully",
                    Data = students
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve students for admin {AdminId}", GetCurrentUserId());
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("students/{id}")]
        [SwaggerOperation(
            Summary = "Update student",
            Description = "Updates a student in the current admin's faculty",
            OperationId = "UpdateStudent"
        )]
        [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto updateDto)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var student = await _authService.UpdateStudentAsync(id, updateDto, adminId);

                return Ok(new ApiResponse<StudentDto>
                {
                    Success = true,
                    Message = "Student updated successfully",
                    Data = student
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update student {StudentId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("students/{id}")]
        [SwaggerOperation(
            Summary = "Delete student",
            Description = "Soft deletes a student from the current admin's faculty",
            OperationId = "DeleteStudent"
        )]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var result = await _authService.DeleteStudentAsync(id, adminId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Student deleted successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete student {StudentId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        #endregion

        #region Doctor Management Endpoints (Admin only)

        [HttpPost("register/doctor")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Register new doctor",
            Description = "Registers a new doctor in the current admin's faculty",
            OperationId = "RegisterDoctor"
        )]
        [ProducesResponseType(typeof(ApiResponse<DoctorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RegisterDoctor([FromBody] RegisterDoctorDto registerDto)
        {
            try
            {
                var adminId = GetCurrentUserId();
                _logger.LogInformation("Doctor registration attempt by admin {AdminId} for email: {Email}",
                    adminId, registerDto.Email);

                var doctor = await _authService.RegisterDoctorAsync(registerDto, adminId);

                _logger.LogInformation("Doctor registered successfully: {DoctorId}", doctor.Id);

                return Ok(new ApiResponse<DoctorDto>
                {
                    Success = true,
                    Message = "Doctor registered successfully",
                    Data = doctor
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Doctor registration failed");
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("doctors")]
        [SwaggerOperation(
            Summary = "Get all doctors in faculty",
            Description = "Retrieves all doctors in the current admin's faculty",
            OperationId = "GetFacultyDoctors"
        )]
        [ProducesResponseType(typeof(ApiResponse<List<DoctorDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetDoctors()
        {
            try
            {
                var adminId = GetCurrentUserId();
                var doctors = await _authService.GetFacultyDoctorsAsync(adminId);

                return Ok(new ApiResponse<List<DoctorDto>>
                {
                    Success = true,
                    Message = "Doctors retrieved successfully",
                    Data = doctors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve doctors for admin {AdminId}", GetCurrentUserId());
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("doctors/{id}")]
        [SwaggerOperation(
            Summary = "Update doctor",
            Description = "Updates a doctor in the current admin's faculty",
            OperationId = "UpdateDoctor"
        )]
        [ProducesResponseType(typeof(ApiResponse<DoctorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDoctor(int id, [FromBody] UpdateDoctorDto updateDto)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var doctor = await _authService.UpdateDoctorAsync(id, updateDto, adminId);

                return Ok(new ApiResponse<DoctorDto>
                {
                    Success = true,
                    Message = "Doctor updated successfully",
                    Data = doctor
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update doctor {DoctorId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("doctors/{id}")]
        [SwaggerOperation(
            Summary = "Delete doctor",
            Description = "Soft deletes a doctor from the current admin's faculty",
            OperationId = "DeleteDoctor"
        )]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var result = await _authService.DeleteDoctorAsync(id, adminId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Doctor deleted successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete doctor {DoctorId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        #endregion

        #region Proctor Management Endpoints (Admin only)

        [HttpPost("register/proctor")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Register new proctor",
            Description = "Registers a new proctor (Admin role required)",
            OperationId = "RegisterProctor"
        )]
        [ProducesResponseType(typeof(ApiResponse<ProctorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RegisterProctor([FromBody] RegisterProctorDto registerDto)
        {
            try
            {
                var adminId = GetCurrentUserId();
                _logger.LogInformation("Proctor registration attempt by admin {AdminId} for email: {Email}",
                    adminId, registerDto.Email);

                var proctor = await _authService.RegisterProctorAsync(registerDto, adminId);

                _logger.LogInformation("Proctor registered successfully: {ProctorId}", proctor.Id);

                return Ok(new ApiResponse<ProctorDto>
                {
                    Success = true,
                    Message = "Proctor registered successfully",
                    Data = proctor
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Proctor registration failed");
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("proctors")]
        [SwaggerOperation(
            Summary = "Get all proctors",
            Description = "Retrieves all active proctors",
            OperationId = "GetAllProctors"
        )]
        [ProducesResponseType(typeof(ApiResponse<List<ProctorDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProctors()
        {
            try
            {
                var adminId = GetCurrentUserId();
                var proctors = await _authService.GetFacultyProctorsAsync(adminId);

                return Ok(new ApiResponse<List<ProctorDto>>
                {
                    Success = true,
                    Message = "Proctors retrieved successfully",
                    Data = proctors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve proctors for admin {AdminId}", GetCurrentUserId());
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("proctors/{id}")]
        [SwaggerOperation(
            Summary = "Update proctor",
            Description = "Updates a proctor information",
            OperationId = "UpdateProctor"
        )]
        [ProducesResponseType(typeof(ApiResponse<ProctorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProctor(int id, [FromBody] UpdateProctorDto updateDto)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var proctor = await _authService.UpdateProctorAsync(id, updateDto, adminId);

                return Ok(new ApiResponse<ProctorDto>
                {
                    Success = true,
                    Message = "Proctor updated successfully",
                    Data = proctor
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update proctor {ProctorId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("proctors/{id}")]
        [SwaggerOperation(
            Summary = "Delete proctor",
            Description = "Soft deletes a proctor",
            OperationId = "DeleteProctor"
        )]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProctor(int id)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var result = await _authService.DeleteProctorAsync(id, adminId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Proctor deleted successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete proctor {ProctorId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        #endregion

        #region Support Management Endpoints (SuperAdmin only)

        [HttpPost("register/support")]
        [Authorize(Roles = "SuperAdmin")]
        [SwaggerOperation(
            Summary = "Register new support",
            Description = "Registers a new support staff member (SuperAdmin role required)",
            OperationId = "RegisterSupport"
        )]
        [ProducesResponseType(typeof(ApiResponse<SupportDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RegisterSupport([FromBody] RegisterSupportDto registerDto)
        {
            try
            {
                _logger.LogInformation("Support registration attempt for email: {Email}", registerDto.Email);

                var support = await _authService.RegisterSupportAsync(registerDto);

                _logger.LogInformation("Support registered successfully: {SupportId}", support.Id);

                return Ok(new ApiResponse<SupportDto>
                {
                    Success = true,
                    Message = "Support registered successfully",
                    Data = support
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Support registration failed");
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("supports")]
        [SwaggerOperation(
            Summary = "Get all supports",
            Description = "Retrieves all active support staff members",
            OperationId = "GetAllSupports"
        )]
        [ProducesResponseType(typeof(ApiResponse<List<SupportDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSupports()
        {
            try
            {
                var supports = await _authService.GetAllSupportsAsync();

                return Ok(new ApiResponse<List<SupportDto>>
                {
                    Success = true,
                    Message = "Supports retrieved successfully",
                    Data = supports
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve supports");
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPut("supports/{id}")]
        [SwaggerOperation(
            Summary = "Update support",
            Description = "Updates a support staff member information",
            OperationId = "UpdateSupport"
        )]
        [ProducesResponseType(typeof(ApiResponse<SupportDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSupport(int id, [FromBody] UpdateSupportDto updateDto)
        {
            try
            {
                var support = await _authService.UpdateSupportAsync(id, updateDto);

                return Ok(new ApiResponse<SupportDto>
                {
                    Success = true,
                    Message = "Support updated successfully",
                    Data = support
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update support {SupportId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("supports/{id}")]
        [SwaggerOperation(
            Summary = "Delete support",
            Description = "Soft deletes a support staff member",
            OperationId = "DeleteSupport"
        )]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSupport(int id)
        {
            try
            {
                var result = await _authService.DeleteSupportAsync(id);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Support deleted successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete support {SupportId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        #endregion

        #region Admin Management Endpoints (SuperAdmin only)

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("admins")]
        [SwaggerOperation(
            Summary = "Get all admins",
            Description = "Retrieves all active faculty admins",
            OperationId = "GetAllAdmins"
        )]
        [ProducesResponseType(typeof(ApiResponse<List<AdminDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAdmins()
        {
            try
            {
                var admins = await _authService.GetAllAdminsAsync();

                return Ok(new ApiResponse<List<AdminDto>>
                {
                    Success = true,
                    Message = "Admins retrieved successfully",
                    Data = admins
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve admins");
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPut("admins/{id}")]
        [SwaggerOperation(
            Summary = "Update admin",
            Description = "Updates a faculty admin information",
            OperationId = "UpdateAdmin"
        )]
        [ProducesResponseType(typeof(ApiResponse<AdminDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAdmin(int id, [FromBody] UpdateAdminDto updateDto)
        {
            try
            {
                var admin = await _authService.UpdateAdminAsync(id, updateDto);

                _logger.LogInformation("Admin {AdminId} updated successfully", id);

                return Ok(new ApiResponse<AdminDto>
                {
                    Success = true,
                    Message = "Admin updated successfully",
                    Data = admin
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update admin {AdminId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("admins/{id}")]
        [SwaggerOperation(
            Summary = "Delete admin",
            Description = "Soft deletes a faculty admin",
            OperationId = "DeleteAdmin"
        )]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            try
            {
                var result = await _authService.DeleteAdminAsync(id);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Admin deleted successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete admin {AdminId}", id);
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        #endregion

        #region Change Password Endpoint

        [Authorize]
        [HttpPost("change-password")]
        [SwaggerOperation(
            Summary = "Change user password",
            Description = "Allows authenticated users to change their own password",
            OperationId = "ChangePassword"
        )]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Password change attempt for user {UserId}", userId);

                var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);

                _logger.LogInformation("Password changed successfully for user {UserId}", userId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Password changed successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change password for user {UserId}", GetCurrentUserId());
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        #endregion
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RefreshRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}