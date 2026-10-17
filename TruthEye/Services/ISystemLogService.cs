using TruthEye.Entities;

namespace TruthEye.Services
{
    public interface ISystemLogService
    {
        Task LogActionAsync(int? adminId, string action, string entityType, int? entityId, string description, string ipAddress);
        Task<IEnumerable<SystemLog>> GetLogsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<SystemLog>> GetLogsByAdminAsync(int adminId);
    }
}
