using Microsoft.EntityFrameworkCore;
using TruthEye.Data;
using TruthEye.Entities;

namespace TruthEye.Services
{
    // Once a day: deletes evidence photos older than Evidence:RetentionDays (default 90, 0 = keep forever).
    // The violation records stay; only the image is removed.
    public class EvidenceRetentionWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly IEvidenceStore _store;
        private readonly IConfiguration _cfg;
        private readonly ILogger<EvidenceRetentionWorker> _log;

        public EvidenceRetentionWorker(
            IServiceScopeFactory scopes,
            IEvidenceStore store,
            IConfiguration cfg,
            ILogger<EvidenceRetentionWorker> log)
        {
            _scopes = scopes;
            _store = store;
            _cfg = cfg;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(2), ct);
                using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
                do
                {
                    await RunOnce(ct);
                } while (await timer.WaitForNextTickAsync(ct));
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
        }

        private async Task RunOnce(CancellationToken ct)
        {
            var days = _cfg.GetValue<int?>("Evidence:RetentionDays") ?? 90;
            if (days <= 0) return;

            var cutoff = DateTime.UtcNow.AddDays(-days);
            var removed = 0;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    using var scope = _scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var batch = await db.Set<AILog>()
                        .Where(l => !string.IsNullOrEmpty(l.ScreenshotPath) && l.Timestamp < cutoff)
                        .Take(200)
                        .ToListAsync(ct);

                    if (batch.Count == 0) break;

                    foreach (var l in batch)
                    {
                        try
                        {
                            await _store.DeleteAsync(l.ScreenshotPath, ct);
                        }
                        catch (ArgumentException)
                        {
                            // not one of our file names (old value): just clear it
                        }
                        l.ScreenshotPath = "";
                    }

                    await db.SaveChangesAsync(ct);
                    removed += batch.Count;
                }

                if (removed > 0) _log.LogInformation("Evidence retention: removed {Count} photo(s) older than {Days} days", removed, days);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogError(ex, "Evidence retention run failed");
            }
        }
    }
}