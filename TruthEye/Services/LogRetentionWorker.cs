using Microsoft.EntityFrameworkCore;
using TruthEye.Data;

namespace TruthEye.Services
{
    public class LogRetentionWorker : BackgroundService
    {
        private static readonly TimeSpan LoginLogKeep = TimeSpan.FromDays(90);
        private static readonly TimeSpan Every = TimeSpan.FromHours(24);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<LogRetentionWorker> _logger;

        public LogRetentionWorker(IServiceScopeFactory scopes, ILogger<LogRetentionWorker> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // نستنى شوية بعد التشغيل عشان مانزاحمش الـ startup
            try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
            catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try { await CleanAsync(stoppingToken); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Log retention cleanup failed");
                }

                try { await Task.Delay(Every, stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }

        private async Task CleanAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;

            var logs = await db.SystemLogs
                .Where(l => l.EntityType == ActivityEntities.Login && l.Timestamp < now - LoginLogKeep)
                .ExecuteDeleteAsync(ct);

            var locks = await db.LoginLockouts
                .Where(l => l.UpdatedAt < now.AddDays(-1) && (l.LockedUntil == null || l.LockedUntil < now))
                .ExecuteDeleteAsync(ct);

            var tokens = await db.RefreshTokens
                .Where(t => t.ExpiresAt < now.AddDays(-1) || (t.RevokedAt != null && t.RevokedAt < now.AddDays(-1)))
                .ExecuteDeleteAsync(ct);

            _logger.LogInformation("Retention cleanup: {Logs} login logs, {Locks} lockouts, {Tokens} refresh tokens removed",
                logs, locks, tokens);
        }
    }
}