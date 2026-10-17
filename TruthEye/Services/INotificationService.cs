using TruthEye.Entities;

namespace TruthEye.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(int userId, string message, string type);
        Task SendBulkNotificationAsync(string role, string message, string type);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId);
        Task MarkAsReadAsync(int notificationId);
    }
}
