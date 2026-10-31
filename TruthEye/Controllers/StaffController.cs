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
    public class StaffController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<StaffController> _logger;

        public StaffController(IAuthService authService, ILogger<StaffController> logger)
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

        /// <summary>الكل (عميد / أمين / تيم فني) يشوف القائمة. CanManage بيقول مين يقدر يعدّل.</summary>
        [HttpGet]
        [Authorize(Roles = AppRoles.FacultyStaff)]
        public async Task<IActionResult> GetStaff([FromQuery] bool includeDisabled = true)
        {
            try { return Success("Staff retrieved successfully", await _authService.GetStaffAsync(GetCurrentUserId(), includeDisabled)); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.Managers)]
        public async Task<IActionResult> Register([FromBody] RegisterStaffDto dto)
        {
            try { return Success("Staff member appointed successfully", await _authService.RegisterStaffAsync(dto, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = AppRoles.Managers)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStaffDto dto)
        {
            try { return Success("Staff member updated successfully", await _authService.UpdateStaffAsync(id, dto, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("{id:int}/disable")]
        [Authorize(Roles = AppRoles.Managers)]
        public async Task<IActionResult> Disable(int id)
        {
            try { return Success("Staff member disabled", await _authService.DisableStaffAsync(id, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("{id:int}/enable")]
        [Authorize(Roles = AppRoles.Managers)]
        public async Task<IActionResult> Enable(int id)
        {
            try { return Success("Staff member enabled", await _authService.EnableStaffAsync(id, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = AppRoles.Managers)]
        public async Task<IActionResult> Delete(int id)
        {
            try { return Success("Staff member removed", await _authService.DeleteStaffAsync(id, GetCurrentUserId())); }
            catch (Exception ex) { return Fail(ex); }
        }
    }
}