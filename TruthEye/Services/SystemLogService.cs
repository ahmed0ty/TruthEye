using TruthEye.Entities;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Services
{
    public class SystemLogService : ISystemLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SystemLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task LogActionAsync(int? adminId, string action, string entityType, int? entityId, string description, string ipAddress)
        {
            var log = new SystemLog
            {
                AdminId = adminId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Description = description,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            await _unitOfWork.Repository<SystemLog>().AddAsync(log);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<SystemLog>> GetLogsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            if (startDate.HasValue && endDate.HasValue)
            {
                return await _unitOfWork.Repository<SystemLog>()
                    .FindAsync(l => l.Timestamp >= startDate && l.Timestamp <= endDate);
            }

            return await _unitOfWork.Repository<SystemLog>().GetAllAsync();
        }

        public async Task<IEnumerable<SystemLog>> GetLogsByAdminAsync(int adminId)
        {
            return await _unitOfWork.Repository<SystemLog>()
                .FindAsync(l => l.AdminId == adminId);
        }
    }
}

