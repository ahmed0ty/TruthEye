using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using TruthEye.Data;
using TruthEye.Entities;

namespace TruthEye.Services
{
    public class SystemLogService : ISystemLogService
    {
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<SystemLogService> _logger;

        private static readonly JsonSerializerOptions Json = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public SystemLogService(AppDbContext db, IHttpContextAccessor http, ILogger<SystemLogService> logger)
        {
            _db = db;
            _http = http;
            _logger = logger;
        }

        #region Legacy API

        public async Task LogActionAsync(int? adminId, string action, string entityType, int? entityId, string description, string ipAddress)
        {
            _db.SystemLogs.Add(new SystemLog
            {
                AdminId = adminId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Description = description ?? "",
                IpAddress = ipAddress ?? "",
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<SystemLog>> GetLogsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var q = _db.SystemLogs.AsNoTracking().AsQueryable();
            if (startDate.HasValue && endDate.HasValue)
                q = q.Where(l => l.Timestamp >= startDate.Value && l.Timestamp <= endDate.Value);
            return await q.OrderByDescending(l => l.Timestamp).ToListAsync();
        }

        public async Task<IEnumerable<SystemLog>> GetLogsByAdminAsync(int adminId)
        {
            return await _db.SystemLogs.AsNoTracking()
                .Where(l => l.AdminId == adminId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        #endregion

        #region Audit trail

        public void Stage(AuditEntry entry) => _db.SystemLogs.Add(Build(entry));

        public async Task LogAsync(AuditEntry entry)
        {
            var log = Build(entry);
            _db.SystemLogs.Add(log);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write activity log {Action} {Entity} {EntityId}",
                    entry.Action, entry.EntityType, entry.EntityId);
                // نشيله من الـ tracker عشان ميبوّظش أي Save جاي في نفس الطلب
                _db.Entry(log).State = EntityState.Detached;
            }
        }

        public async Task<SystemLog?> GetByIdAsync(int id) =>
            await _db.SystemLogs.FirstOrDefaultAsync(l => l.Id == id);

        public void MarkReverted(SystemLog log, int revertedBy)
        {
            log.IsReverted = true;
            log.RevertedBy = revertedBy;
            log.RevertedAt = DateTime.UtcNow;
        }

        public async Task<ActivityLogPage> QueryAsync(ActivityLogFilter f)
        {
            var q = _db.SystemLogs.AsNoTracking().AsQueryable();

            if (f.FacultyId.HasValue) q = q.Where(l => l.FacultyId == f.FacultyId.Value);
            if (!string.IsNullOrWhiteSpace(f.EntityType)) q = q.Where(l => l.EntityType == f.EntityType);
            if (!string.IsNullOrWhiteSpace(f.Action)) q = q.Where(l => l.Action == f.Action);
            if (f.ActorId.HasValue) q = q.Where(l => l.AdminId == f.ActorId.Value);
            if (f.From.HasValue) q = q.Where(l => l.Timestamp >= f.From.Value);
            if (f.To.HasValue) q = q.Where(l => l.Timestamp <= f.To.Value);

            var page = Math.Max(1, f.Page);
            var size = Math.Clamp(f.PageSize, 1, 100);

            var total = await q.CountAsync();
            var rows = await q.OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            // أسماء المنفّذين (بما فيهم المحذوفين، لأن مفيش global filter على DbSet مباشرة)
            var ids = rows.Where(r => r.AdminId.HasValue).Select(r => r.AdminId!.Value).Distinct().ToList();
            var names = ids.Count == 0
                ? new Dictionary<int, string>()
                : await _db.Users.AsNoTracking()
                    .Where(u => ids.Contains(u.Id))
                    .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
                    .ToDictionaryAsync(x => x.Id, x => x.Name);

            return new ActivityLogPage
            {
                Total = total,
                Page = page,
                PageSize = size,
                Items = rows.Select(l => new ActivityLogDto
                {
                    Id = l.Id,
                    Timestamp = l.Timestamp,
                    ActorId = l.AdminId,
                    ActorName = l.AdminId.HasValue && names.TryGetValue(l.AdminId.Value, out var n) ? n : "",
                    ActorRole = l.ActorRole,
                    Action = l.Action,
                    EntityType = l.EntityType,
                    EntityId = l.EntityId,
                    Description = l.Description,
                    OldValues = l.OldValues,
                    NewValues = l.NewValues,
                    IsReverted = l.IsReverted,
                    RevertedAt = l.RevertedAt,
                    CanRevert = !l.IsReverted && ActivityActions.IsReversible(l.EntityType, l.Action)
                }).ToList()
            };
        }

        #endregion

        private SystemLog Build(AuditEntry e) => new()
        {
            AdminId = e.ActorId,
            ActorRole = e.ActorRole,
            FacultyId = e.FacultyId,
            Action = e.Action,
            EntityType = e.EntityType,
            EntityId = e.EntityId,
            Description = e.Description ?? "",
            IpAddress = e.IpAddress ?? _http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "",
            OldValues = e.OldValues == null ? null : JsonSerializer.Serialize(e.OldValues, Json),
            NewValues = e.NewValues == null ? null : JsonSerializer.Serialize(e.NewValues, Json),
            Timestamp = DateTime.UtcNow
        };
    }
}