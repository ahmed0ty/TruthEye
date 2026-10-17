using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TruthEye.Entities;
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
        public async Task<IActionResult> GetAllUsers([FromQuery] string role = null)
        {
            try
            {
                var users = string.IsNullOrEmpty(role)
                    ? await _unitOfWork.Repository<User>().GetAllAsync()
                    : await _unitOfWork.Repository<User>().FindAsync(u => u.Role.Name == role);

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve users");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("users/{userId}/role")]
        public async Task<IActionResult> UpdateUserRole(int userId, [FromQuery] string roleName)
        {
            try
            {
                var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
                if (user == null)
                    return NotFound(new { Error = "User not found" });

                var role = (await _unitOfWork.Repository<Role>()
                    .FindAsync(r => r.Name == roleName)).FirstOrDefault();

                if (role == null)
                    return BadRequest(new { Error = "Invalid role" });

                user.RoleId = role.Id;
                await _unitOfWork.Repository<User>().UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("User role updated: {UserId} to {RoleName}", userId, roleName);
                return Ok(new { Message = "Role updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user role");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("users/{userId}/status")]
        public async Task<IActionResult> UpdateUserStatus(int userId, [FromQuery] bool isActive)
        {
            try
            {
                var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
                if (user == null)
                    return NotFound(new { Error = "User not found" });

                user.IsActive = isActive;
                await _unitOfWork.Repository<User>().UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("User status updated: {UserId} to {Status}", userId, isActive);
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