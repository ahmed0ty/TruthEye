using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TruthEye.DTOs;
using TruthEye.Entities.Data;
using TruthEye.Security;
using TruthEye.Services;
using TruthEye.Swagger;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Roles = AppRoles.ManagersAndSuper)]
    public class ActivityLogController : ControllerBase
    {
        private readonly ISystemLogService _logs;
        private readonly IAuthService _authService;
        private readonly ICourseService _courseService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ActivityLogController> _logger;

        public ActivityLogController(ISystemLogService logs, IAuthService authService, ICourseService courseService,
            IUnitOfWork unitOfWork, ILogger<ActivityLogController> logger)
        {
            _logs = logs;
            _authService = authService;
            _courseService = courseService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        private string CurrentRole => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        /// <summary>العميد والأمين بيشوفوا سجل كليتهم بس. رئيس الجامعة بيشوف الكل أو يفلتر بـ facultyId.</summary>
        [HttpGet]
        public async Task<IActionResult> GetLogs([FromQuery] ActivityLogFilter filter)
        {
            try
            {
                if (CurrentRole != AppRoles.SuperAdmin)
                {
                    var admin = await _unitOfWork.Repository<Admin>().GetByIdAsync(GetCurrentUserId());
                    if (admin == null || !admin.IsActive)
                        return Unauthorized(new ApiErrorResponse { Success = false, Error = "Account not found or disabled" });

                    filter.FacultyId = admin.FacultyId;   // بنكتب فوق أي قيمة جاية من الـ client
                }

                var page = await _logs.QueryAsync(filter);
                return Ok(new ApiResponse<ActivityLogPage> { Success = true, Message = "Activity log retrieved", Data = page });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load activity log");
                return BadRequest(new ApiErrorResponse { Success = false, Error = ex.Message });
            }
        }

        /// <summary>التراجع عن عملية. للعميد والأمين بس.</summary>
        [HttpPost("{id:int}/revert")]
        [Authorize(Roles = AppRoles.Managers)]
        public async Task<IActionResult> Revert(int id)
        {
            try
            {
                var log = await _logs.GetByIdAsync(id);
                if (log == null)
                    return NotFound(new ApiErrorResponse { Success = false, Error = "Log entry not found" });

                var actorId = GetCurrentUserId();
                var summary = log.EntityType is ActivityEntities.Course or ActivityEntities.CourseStudent or ActivityEntities.CourseDoctor
                    ? await _courseService.RevertCourseActionAsync(id, actorId)
                    : await _authService.RevertMemberActionAsync(id, actorId);

                return Ok(new ApiResponse<string> { Success = true, Message = summary, Data = summary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Revert of log {LogId} failed", id);
                return BadRequest(new ApiErrorResponse { Success = false, Error = ex.Message });
            }
        }
    }
}