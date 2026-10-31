using TruthEye.Entities;

namespace TruthEye.Services
{
    public interface ISystemLogService
    {
        // القديم (متوافق مع أي كود بيستخدمه)
        Task LogActionAsync(int? adminId, string action, string entityType, int? entityId, string description, string ipAddress);
        Task<IEnumerable<SystemLog>> GetLogsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<SystemLog>> GetLogsByAdminAsync(int adminId);

        // الجديد
        /// <summary>يضيف السجل من غير Save، فيتحفظ مع عملية الخدمة في نفس الـ SaveChanges (ذرّي).</summary>
        void Stage(AuditEntry entry);
        /// <summary>يضيف ويحفظ. بنستخدمه بعد Create لأن الـ Id مبيبقاش معروف قبل الحفظ. فشله مبيكسرش العملية الأصلية.</summary>
        Task LogAsync(AuditEntry entry);
        Task<ActivityLogPage> QueryAsync(ActivityLogFilter filter);
        Task<SystemLog?> GetByIdAsync(int id);
        /// <summary>يعلّم السجل إنه اتراجع عنه. الحفظ على اللي بينادي.</summary>
        void MarkReverted(SystemLog log, int revertedBy);
    }

    public class AuditEntry
    {
        public int ActorId { get; set; }
        public string ActorRole { get; set; } = string.Empty;
        public int? FacultyId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string Description { get; set; } = string.Empty;
        public object? OldValues { get; set; }
        public object? NewValues { get; set; }
        public string? IpAddress { get; set; }   // لو null بتتاخد من الـ request
    }

    public class ActivityLogFilter
    {
        public int? FacultyId { get; set; }
        public string? EntityType { get; set; }
        public string? Action { get; set; }
        public int? ActorId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    public class ActivityLogDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int? ActorId { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public string? ActorRole { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public bool IsReverted { get; set; }
        public DateTime? RevertedAt { get; set; }
        public bool CanRevert { get; set; }
    }

    public class ActivityLogPage
    {
        public List<ActivityLogDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}