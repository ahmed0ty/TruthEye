
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TruthEye.Services;

namespace TruthEye.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                var notifications = await _notificationService.GetUserNotificationsAsync(userId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve notifications");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{notificationId}/read")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            try
            {
                await _notificationService.MarkAsReadAsync(notificationId);
                return Ok(new { Message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark notification as read: {NotificationId}", notificationId);
                return BadRequest(new { Error = ex.Message });
            }
        }

        [Authorize(Roles = "Admin,Support")]
        [HttpPost("send")]
        public async Task<IActionResult> SendNotification(
            [FromQuery] int userId,
            [FromQuery] string message,
            [FromQuery] string type)
        {
            try
            {
                await _notificationService.SendNotificationAsync(userId, message, type);
                _logger.LogInformation("Notification sent to user: {UserId}", userId);
                return Ok(new { Message = "Notification sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification");
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}

