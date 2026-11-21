using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.Security;
using TruthEye.Services;
using TruthEye.UnitOfWorkk;
using TruthEye.Middleware;
namespace TruthEye.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotificationService notificationService, IUnitOfWork unitOfWork, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var notifications = await _notificationService.GetUserNotificationsAsync(userId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return ApiErrors.Result(ex, _logger, "Failed to retrieve notifications");
            }
        }

        [HttpPost("{notificationId}/read")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            try
            {
                var me = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var n = await _unitOfWork.Repository<Notification>().GetByIdAsync(notificationId);
                if (n == null || n.UserId != me)
                    return NotFound(new { Error = "Notification not found" });

                await _notificationService.MarkAsReadAsync(notificationId, me);
                return Ok(new { Message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                return ApiErrors.Result(ex, _logger, "Failed to mark notification as read");
            }
        }

        [Authorize(Roles = "Support," + AppRoles.Managers)]
        [HttpPost("send")]
        public async Task<IActionResult> SendNotification(
            [FromQuery] int userId,
            [FromQuery] string message,
            [FromQuery] string type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message) || message.Length > 500)
                    return BadRequest(new { Error = "Message must be 1 to 500 characters" });
                if (type is { Length: > 30 })
                    return BadRequest(new { Error = "Notification type is too long" });

                var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

                // الدعم الفني بيبعت لأي حد. العميد والأمين لأعضاء كليتهم بس.
                if (role != "Support")
                {
                    var actorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                    var actor = await _unitOfWork.Repository<Admin>().GetByIdAsync(actorId);
                    var target = await _unitOfWork.Repository<User>().GetByIdAsync(userId);

                    if (actor == null || !actor.IsActive || target == null)
                        return BadRequest(new { Error = "User not found" });

                    var targetFaculty = target switch
                    {
                        Student s => s.FacultyId,
                        Doctor d => d.FacultyId,
                        Proctor p => p.FacultyId ?? 0,
                        Admin a => a.FacultyId,
                        _ => 0
                    };

                    if (targetFaculty != actor.FacultyId)
                        return StatusCode(403, new { Error = "You can only notify members of your own faculty" });
                }

                await _notificationService.SendNotificationAsync(userId, message, type);
                _logger.LogInformation("Notification sent to user: {UserId}", userId);
                return Ok(new { Message = "Notification sent successfully" });
            }
            catch (Exception ex)
            {
                return ApiErrors.Result(ex, _logger, "Failed to send notification");
            }
        }
    }
}