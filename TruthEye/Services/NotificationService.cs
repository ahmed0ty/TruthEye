using Microsoft.AspNetCore.SignalR;
using TruthEye.API.Hubs;
using TruthEye.Entities;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IUnitOfWork unitOfWork,
            IHubContext<NotificationHub> notificationHub,
            ILogger<NotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationHub = notificationHub;
            _logger = logger;
        }

        public async Task SendNotificationAsync(int userId, string message, string type)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                Type = type,
                IsRead = false
            };

            await _unitOfWork.Repository<Notification>().AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            await _notificationHub.Clients.Group($"user:{userId}")
                .SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    message = notification.Message,
                    type = notification.Type,
                    isRead = false,
                    timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Notification sent to user: {UserId}", userId);
        }

        public async Task SendBulkNotificationAsync(string role, string message, string type)
        {
            await _notificationHub.Clients.Group($"role:{role}")
                .SendAsync("ReceiveNotification", new
                {
                    message,
                    type,
                    timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Bulk notification sent to role: {Role}", role);
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId)
        {
            return await _unitOfWork.Repository<Notification>()
                .FindAsync(n => n.UserId == userId);
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _unitOfWork.Repository<Notification>().GetByIdAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _unitOfWork.Repository<Notification>().UpdateAsync(notification);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
