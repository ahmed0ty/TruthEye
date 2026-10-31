using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class AdminController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IUnitOfWork unitOfWork, ILogger<AdminController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // AI Configuration Management
        [HttpGet("ai-config")]
        public async Task<IActionResult> GetAIConfigs()
        {
            try
            {
                var configs = await _unitOfWork.Repository<AIConfig>().GetAllAsync();
                return Ok(configs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve AI configs");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("ai-config")]
        public async Task<IActionResult> CreateOrUpdateAIConfig([FromBody] AIConfig config)
        {
            try
            {
                var existing = (await _unitOfWork.Repository<AIConfig>()
                    .FindAsync(c => c.ParameterName == config.ParameterName)).FirstOrDefault();

                if (existing != null)
                {
                    existing.Value = config.Value;
                    existing.Threshold = config.Threshold;
                    existing.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Repository<AIConfig>().UpdateAsync(existing);
                }
                else
                {
                    config.CreatedAt = DateTime.UtcNow;
                    await _unitOfWork.Repository<AIConfig>().AddAsync(config);
                }

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("AI Config updated: {ParameterName}", config.ParameterName);

                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update AI config");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] string? role = null)
        {
            try
            {
                var query = _unitOfWork.Repository<User>().GetQueryable(u => u.Role);
                if (!string.IsNullOrEmpty(role))
                    query = query.Where(u => u.Role.Name == role);

                var users = await query
                    .OrderBy(u => u.Id)
                    .Select(u => new
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        u.Email,
                        u.NationalId,
                        u.PhoneNumber,
                        Role = u.Role.Name,
                        u.IsActive,
                        u.LastLogin,
                        u.CreatedAt
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve users");
                return BadRequest(new { Error = ex.Message });
            }
        }

        // الدور بيتحدد من نوع الحساب نفسه، فتغيير RoleId لوحده بيعمل تضارب.
        [HttpPut("users/{userId}/role")]
        public IActionResult UpdateUserRole(int userId, [FromQuery] string roleName) =>
            BadRequest(new { Error = "The role is determined by the account type and can't be changed. Create the account again with the right type." });

        [HttpPut("users/{userId}/status")]
        public async Task<IActionResult> UpdateUserStatus(int userId, [FromQuery] bool isActive)
        {
            try
            {
                var currentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == currentId)
                    return BadRequest(new { Error = "You can't change your own status" });

                var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
                if (user == null)
                    return NotFound(new { Error = "User not found" });

                if (user is SuperAdmin)
                    return BadRequest(new { Error = "The super admin account can't be disabled" });

                user.IsActive = isActive;
                user.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Repository<User>().UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("User status updated: {UserId} to {Status} by {ActorId}", userId, isActive, currentId);
                return Ok(new { Message = "Status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user status");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("analytics/overview")]
        public async Task<IActionResult> GetSystemOverview()
        {
            try
            {
                var totalUsers = await _unitOfWork.Repository<User>().CountAsync();
                var activeExams = await _unitOfWork.Repository<Exam>()
                    .CountAsync(e => e.Status == "Active");
                var totalSessions = await _unitOfWork.Repository<ExamSession>().CountAsync();
                var aiAlerts = await _unitOfWork.Repository<AILog>().CountAsync();

                return Ok(new
                {
                    TotalUsers = totalUsers,
                    ActiveExams = activeExams,
                    TotalSessions = totalSessions,
                    AIAlerts = aiAlerts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve system overview");
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}