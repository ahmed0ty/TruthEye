using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TruthEye.DTOs;
using TruthEye.Exceptions;
using TruthEye.Middleware;
using TruthEye.Security;
using TruthEye.Services;
using TruthEye.Swagger;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
namespace TruthEye.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, IMemoryCache cache, IConfiguration config)
        {
            _authService = authService;
            _logger = logger;
            _cache = cache;
            _config = config;
        }

        private const string RtCookie = "te_rt";
        private const string DeviceCookie = "te_dev";

        private CookieOptions RtOptions(DateTimeOffset? expires)
        {
            var cross = _config.GetValue<bool>("Auth:CrossSiteCookies");
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = cross || Request.IsHttps,
                SameSite = cross ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/api/Auth",
                Expires = expires
            };
        }

        private void SetRefreshCookie(string token) =>
            Response.Cookies.Append(RtCookie, token, RtOptions(DateTimeOffset.UtcNow.AddDays(7)));

        private void ClearRefreshCookie() =>
            Response.Cookies.Delete(RtCookie, RtOptions(null));

        private string EnsureDeviceId()
        {
            var id = Request.Cookies[DeviceCookie];
            if (!string.IsNullOrWhiteSpace(id) && id.Length == 32) return id;

            id = Guid.NewGuid().ToString("N");
            var cross = _config.GetValue<bool>("Auth:CrossSiteCookies");
            Response.Cookies.Append(DeviceCookie, id, new CookieOptions
            {
                HttpOnly = true,
                Secure = cross || Request.IsHttps,
                SameSite = cross ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/api/Auth",
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
            return id;
        }

        private bool HasCsrfHeader() =>
            Request.Headers["X-Requested-With"] == "fetch";

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        private IActionResult Success<T>(string message, T data) =>
            Ok(new ApiResponse<T> { Success = true, Message = message, Data = data });

        private IActionResult Fail(Exception ex) =>
            ApiErrors.Result(ex, _logger, ControllerContext.ActionDescriptor.ActionName);

        #region Authentication

        // Logs keep a short hash of the login name, never the name itself
        private static string AccountTag(string? account)
        {
            if (string.IsNullOrWhiteSpace(account)) return "unknown";
            var hash = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(account.Trim().ToLowerInvariant()));
            return Convert.ToHexString(hash)[..12];
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var outcome = await _authService.BeginLoginAsync(loginDto, ip, EnsureDeviceId());
                if (outcome.TwoFactorToken != null)
                {
                    return Success("Two-factor code required", new LoginResponse
                    {
                        TwoFactorRequired = true,
                        TwoFactorToken = outcome.TwoFactorToken
                    });
                }
                var tokens = outcome.Tokens!;
                SetRefreshCookie(tokens.RefreshToken);
                return Success("Login successful", new LoginResponse
                {
                    Token = tokens.AccessToken,
                    MustChangePassword = tokens.MustChangePassword
                });
            }
            catch (AppException ex)
            {
                // Lockout, disabled-account and other-device messages are written for the user
                _logger.LogWarning("Login rejected for {Account}: {Message}", AccountTag(loginDto.Email ?? loginDto.NationalId), ex.Message);
                return Unauthorized(new ApiErrorResponse { Success = false, Error = ex.Message, ErrorCode = ex.Code });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed unexpectedly for {Account}", AccountTag(loginDto.Email ?? loginDto.NationalId));
                return Unauthorized(new ApiErrorResponse { Success = false, Error = "Login failed. Please try again.", ErrorCode = "AUTH_FAILED" });
            }
        }

        [AllowAnonymous]
        [HttpGet("setup-status")]
        public async Task<IActionResult> GetSetupStatus()
        {
            var exists = await _authService.SuperAdminExistsAsync();
            var open = _config.GetValue<bool>("Setup:AllowSuperAdminRegistration") && !exists;
            return Success("Setup status", new { superAdminExists = exists, registrationOpen = open });
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("login/2fa")]
        public async Task<IActionResult> LoginWithTwoFactor([FromBody] TwoFactorLoginDto dto)
        {
            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var tokens = await _authService.CompleteTwoFactorLoginAsync(dto.ChallengeToken, dto.Code, ip, EnsureDeviceId());
                SetRefreshCookie(tokens.RefreshToken);
                return Success("Login successful", new LoginResponse
                {
                    Token = tokens.AccessToken,
                    MustChangePassword = tokens.MustChangePassword
                });
            }
            catch (AppException ex)
            {
                _logger.LogWarning("Two-factor step rejected: {Message}", ex.Message);
                return Unauthorized(new ApiErrorResponse { Success = false, Error = ex.Message, ErrorCode = ex.Code });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Two-factor login failed unexpectedly");
                return Unauthorized(new ApiErrorResponse { Success = false, Error = "Login failed. Please try again.", ErrorCode = "AUTH_FAILED" });
            }
        }

        [Authorize]
        [HttpGet("2fa")]
        public async Task<IActionResult> GetTwoFactorStatus()
        {
            try
            {
                var enabled = await _authService.IsTwoFactorEnabledAsync(GetCurrentUserId());
                return Success("Two-factor status", new { enabled });
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [Authorize]
        [EnableRateLimiting("auth")]
        [HttpPost("2fa/setup")]
        public async Task<IActionResult> BeginTwoFactorSetup()
        {
            try
            {
                var setup = await _authService.BeginTwoFactorSetupAsync(GetCurrentUserId());
                return Success("Add the account to your authenticator app, then enter a code to turn it on", setup);
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [Authorize]
        [EnableRateLimiting("auth")]
        [HttpPost("2fa/enable")]
        public async Task<IActionResult> EnableTwoFactor([FromBody] TwoFactorCodeDto dto)
        {
            try
            {
                var codes = await _authService.EnableTwoFactorAsync(GetCurrentUserId(), dto.Code);
                return Success("Two-factor sign-in is on. Save these recovery codes now; they won't be shown again.", new { recoveryCodes = codes });
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [Authorize]
        [EnableRateLimiting("auth")]
        [HttpPost("2fa/disable")]
        public async Task<IActionResult> DisableTwoFactor([FromBody] TwoFactorDisableDto dto)
        {
            try
            {
                await _authService.DisableTwoFactorAsync(GetCurrentUserId(), dto.Password, dto.Code);
                return Success<object?>("Two-factor sign-in is off", null);
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.Dean)]
        [HttpPost("users/{id:int}/2fa/reset")]
        public async Task<IActionResult> ResetTwoFactor(int id)
        {
            try
            {
                await _authService.ResetTwoFactorAsync(id, GetCurrentUserId());
                return Success<object?>("Two-factor sign-in was reset for this account", null);
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            if (!HasCsrfHeader())
                return StatusCode(403, new ApiErrorResponse { Success = false, Error = "Invalid request" });

            try
            {
                var t = await _authService.RefreshAsync(Request.Cookies[RtCookie] ?? "");
                SetRefreshCookie(t.RefreshToken);
                return Success("Token refreshed", new LoginResponse { Token = t.AccessToken });
            }
            catch (UnauthorizedAccessException ex)
            {
                ClearRefreshCookie();
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
        public async Task<IActionResult> Logout()
        {
            if (!HasCsrfHeader())
                return StatusCode(403, new ApiErrorResponse { Success = false, Error = "Invalid request" });

            await _authService.EndSessionAsync(Request.Cookies[RtCookie] ?? "");
            await _authService.RevokeRefreshAsync(Request.Cookies[RtCookie] ?? "");
            ClearRefreshCookie();
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
            // First-time setup only: the endpoint stays hidden unless the setup flag is switched on
            if (!_config.GetValue<bool>("Setup:AllowSuperAdminRegistration"))
                return NotFound();

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
            try
            {
                var userId = GetCurrentUserId();
                var result = await _authService.ChangePasswordAsync(userId, dto);
                _cache.Remove($"state:{userId}");   // يرفع علامة "لازم يغيّر" فوراً
                return Success("Password changed successfully", result);
            }
            catch (Exception ex) { return Fail(ex); }
        }

        /// <summary>يبعت رابط إعادة تعيين لبريد الحساب. مابيرجعش أي باسورد.</summary>
        [HttpPost("users/{id:int}/reset-password")]
        [Authorize(Roles = AppRoles.FacultyStaffAndSuper)]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            try
            {
                var maskedEmail = await _authService.ResetPasswordAsync(id, GetCurrentUserId());
                _cache.Remove($"state:{id}");   // العلامة تتطبق فوراً مش بعد 30 ثانية
                return Success("Reset link sent", new { sent = true, email = maskedEmail });
            }
            catch (Exception ex) { return Fail(ex); }
        }

        #endregion

        [HttpGet("users/{id:int}/reset-password/preview")]
        [Authorize(Roles = AppRoles.FacultyStaffAndSuper)]
        public async Task<IActionResult> PreviewResetPassword(int id)
        {
            try
            {
                var masked = await _authService.PreviewResetEmailAsync(id, GetCurrentUserId());
                return Success("Reset email preview", new { email = masked });
            }
            catch (Exception ex) { return Fail(ex); }
        }

        /// <summary>يأكد رابط إعادة التعيين ويغيّر الباسورد. عام، والرابط نفسه هو التفويض.</summary>
        [HttpPost("password-reset/confirm")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ConfirmPasswordReset([FromBody] ConfirmPasswordResetDto dto)
        {
            try
            {
                await _authService.ConfirmPasswordResetAsync(dto.Token, dto.NewPassword);
                return Success<object?>("Password updated", null);
            }
            catch (Exception ex) { return Fail(ex); }
        }

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

        [HttpGet("students/paged")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> GetStudentsPaged([FromQuery] PageQuery q)
        {
            try { return Success("Students retrieved successfully", await _authService.GetFacultyStudentsPagedAsync(GetCurrentUserId(), q)); }
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

        [HttpGet("doctors/paged")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> GetDoctorsPaged([FromQuery] PageQuery q)
        {
            try { return Success("Doctors retrieved successfully", await _authService.GetFacultyDoctorsPagedAsync(GetCurrentUserId(), q)); }
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

        [HttpGet("proctors/paged")]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> GetProctorsPaged([FromQuery] PageQuery q)
        {
            try { return Success("Proctors retrieved successfully", await _authService.GetFacultyProctorsPagedAsync(GetCurrentUserId(), q)); }
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
            try { return Success("Support registered successfully", await _authService.RegisterSupportAsync(dto, GetCurrentUserId())); }
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
        public bool MustChangePassword { get; set; }
        public bool TwoFactorRequired { get; set; }
        public string? TwoFactorToken { get; set; }
    }
}