using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TruthEye.DTOs;
using TruthEye.Security;
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

        private IActionResult Success<T>(string message, T data) =>
            Ok(new ApiResponse<T> { Success = true, Message = message, Data = data });

        private IActionResult Fail(Exception ex)
        {
            _logger.LogError(ex, "{Action} failed", ControllerContext.ActionDescriptor.ActionName);
            return BadRequest(new ApiErrorResponse { Success = false, Error = ex.Message });
        }

        #region Authentication

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var tokens = await _authService.LoginAsync(loginDto);
                return Success("Login successful", new LoginResponse
                {
                    Token = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for: {Email}/{NationalId}", loginDto.Email, loginDto.NationalId);
                return Unauthorized(new ApiErrorResponse { Success = false, Error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
        {
            try
            {
                var t = await _authService.RefreshAsync(req.RefreshToken);
                return Success("Token refreshed", new LoginResponse { Token = t.AccessToken, RefreshToken = t.RefreshToken });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ApiErrorResponse { Success = false, Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh failed");
                return BadRequest(new ApiErrorResponse { Success = false, Error = "Couldn't refresh the session" });
            }
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest req)
        {
            await _authService.RevokeRefreshAsync(req.RefreshToken);
            return Success("Signed out", true);
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try { return Success("Profile retrieved successfully", await _authService.GetUserByIdAsync(GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [AllowAnonymous]
        [HttpPost("register/super-admin")]
        public async Task<IActionResult> RegisterSuperAdmin([FromBody] RegisterUserDto registerDto)
        {
            try
            {
                if (await _authService.SuperAdminExistsAsync())
                    return StatusCode(403, new ApiErrorResponse { Success = false, Error = "Setup already completed. A super admin already exists." });

                var adminDto = await _authService.RegisterSuperAdminAsync(registerDto);
                return CreatedAtAction(nameof(GetProfile), new { id = adminDto.Id },
                    new ApiResponse<AdminDto> { Success = true, Message = "Super admin registered successfully", Data = adminDto });
            }
            catch (Exception ex) { return Fail(ex); }
        }

        /// <summary>رئيس الجامعة بيعيّن عميد.</summary>
        [HttpPost("register/admin")]
        [Authorize(Roles = AppRoles.SuperAdmin)]
        public async Task<IActionResult> RegisterFacultyAdmin([FromBody] RegisterAdminDto registerDto)
        {
            try
            {
                var adminDto = await _authService.RegisterFacultyAdminAsync(registerDto, GetCurrentUserId());
                return CreatedAtAction(nameof(GetProfile), new { id = adminDto.Id },
                    new ApiResponse<AdminDto> { Success = true, Message = "Faculty admin registered successfully", Data = adminDto });
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try { return Success("Password changed successfully", await _authService.ChangePasswordAsync(GetCurrentUserId(), dto)); }
            catch (Exception ex) { return Fail(ex); }
        }

        #endregion

        #region Students (Dean / Secretary / Technical). Delete: Dean / Secretary

        [HttpPost("register/student")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> RegisterStudent([FromBody] RegisterStudentDto dto)
        {
            try { return Success("Student registered successfully", await _authService.RegisterStudentAsync(dto, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("students")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> GetStudents([FromQuery] bool includeDisabled = false)
        {
            try { return Success("Students retrieved successfully", await _authService.GetFacultyStudentsAsync(GetCurrentUserId(), includeDisabled)); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPut("students/{id}")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto dto)
        {
            try { return Success("Student updated successfully", await _authService.UpdateStudentAsync(id, dto, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("students/{id}/disable")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> DisableStudent(int id)
        {
            try { return Success("Student disabled", await _authService.DisableStudentAsync(id, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("students/{id}/enable")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> EnableStudent(int id)
        {
            try { return Success("Student enabled", await _authService.EnableStudentAsync(id, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpDelete("students/{id}")]
        [Authorize(Roles = AppRoles.Managers)]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            try { return Success("Student deleted successfully", await _authService.DeleteStudentAsync(id, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        #endregion

        #region Doctors

        [HttpPost("register/doctor")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> RegisterDoctor([FromBody] RegisterDoctorDto dto)
        {
            try { return Success("Doctor registered successfully", await _authService.RegisterDoctorAsync(dto, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("doctors")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> GetDoctors([FromQuery] bool includeDisabled = false)
        {
            try { return Success("Doctors retrieved successfully", await _authService.GetFacultyDoctorsAsync(GetCurrentUserId(), includeDisabled)); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPut("doctors/{id}")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> UpdateDoctor(int id, [FromBody] UpdateDoctorDto dto)
        {
            try { return Success("Doctor updated successfully", await _authService.UpdateDoctorAsync(id, dto, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("doctors/{id}/disable")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> DisableDoctor(int id)
        {
            try { return Success("Doctor disabled", await _authService.DisableDoctorAsync(id, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("doctors/{id}/enable")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> EnableDoctor(int id)
        {
            try { return Success("Doctor enabled", await _authService.EnableDoctorAsync(id, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpDelete("doctors/{id}")]
        [Authorize(Roles = AppRoles.Managers)]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            try { return Success("Doctor deleted successfully", await _authService.DeleteDoctorAsync(id, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        #endregion

        #region Proctors

        [HttpPost("register/proctor")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> RegisterProctor([FromBody] RegisterProctorDto dto)
        {
            try { return Success("Proctor registered successfully", await _authService.RegisterProctorAsync(dto, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("proctors")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> GetProctors([FromQuery] bool includeDisabled = false)
        {
            try { return Success("Proctors retrieved successfully", await _authService.GetFacultyProctorsAsync(GetCurrentUserId(), includeDisabled)); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPut("proctors/{id}")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> UpdateProctor(int id, [FromBody] UpdateProctorDto dto)
        {
            try { return Success("Proctor updated successfully", await _authService.UpdateProctorAsync(id, dto, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("proctors/{id}/disable")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> DisableProctor(int id)
        {
            try { return Success("Proctor disabled", await _authService.DisableProctorAsync(id, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("proctors/{id}/enable")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> EnableProctor(int id)
        {
            try { return Success("Proctor enabled", await _authService.EnableProctorAsync(id, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpDelete("proctors/{id}")]
        [Authorize(Roles = AppRoles.Managers)]
        public async Task<IActionResult> DeleteProctor(int id)
        {
            try { return Success("Proctor deleted successfully", await _authService.DeleteProctorAsync(id, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        #endregion

        #region Support (SuperAdmin only)

        [HttpPost("register/support")]
        [Authorize(Roles = AppRoles.SuperAdmin)]
        public async Task<IActionResult> RegisterSupport([FromBody] RegisterSupportDto dto)
        {
            try { return Success("Support registered successfully", await _authService.RegisterSupportAsync(dto)); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("supports")]
        [Authorize(Roles = AppRoles.SuperAdmin)]
        public async Task<IActionResult> GetSupports()
        {
            try { return Success("Supports retrieved successfully", await _authService.GetAllSupportsAsync()); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPut("supports/{id}")]
        [Authorize(Roles = AppRoles.SuperAdmin)]
        public async Task<IActionResult> UpdateSupport(int id, [FromBody] UpdateSupportDto dto)
        {
            try { return Success("Support updated successfully", await _authService.UpdateSupportAsync(id, dto)); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpDelete("supports/{id}")]
        [Authorize(Roles = AppRoles.SuperAdmin)]
        public async Task<IActionResult> DeleteSupport(int id)
        {
            try { return Success("Support deleted successfully", await _authService.DeleteSupportAsync(id)); }
            catch (Exception ex) { return Fail(ex); }
        }

        #endregion

        #region Deans (SuperAdmin only)

        [HttpGet("admins")]
        [Authorize(Roles = AppRoles.SuperAdmin)]
        public async Task<IActionResult> GetAdmins()
        {
            try { return Success("Admins retrieved successfully", await _authService.GetAllAdminsAsync()); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPut("admins/{id}")]
        [Authorize(Roles = AppRoles.SuperAdmin)]
        public async Task<IActionResult> UpdateAdmin(int id, [FromBody] UpdateAdminDto dto)
        {
            try { return Success("Admin updated successfully", await _authService.UpdateAdminAsync(id, dto, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpDelete("admins/{id}")]
        [Authorize(Roles = AppRoles.SuperAdmin)]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            try { return Success("Admin deleted successfully", await _authService.DeleteAdminAsync(id, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
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