using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using TruthEye.Entities;
using TruthEye.UnitOfWorkk;

namespace TruthEye.API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(IUnitOfWork unitOfWork, ILogger<NotificationHub> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{userRole}");

            _logger.LogInformation("User connected to notifications: {UserId}", userId);
            await base.OnConnectedAsync();
        }

        public async Task MarkNotificationAsRead(string notificationId)
        {
            try
            {
                if (!int.TryParse(notificationId, out var id)) return;

                var currentUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var notification = await _unitOfWork.Repository<Notification>().GetByIdAsync(id);

                if (notification != null && notification.UserId.ToString() == currentUserId)
                {
                    notification.IsRead = true;
                    await _unitOfWork.Repository<Notification>().UpdateAsync(notification);
                    await _unitOfWork.SaveChangesAsync();

                    await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
            }
        }
    }
}