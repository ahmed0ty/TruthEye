namespace TruthEye.Services
{
    public class SessionExpiryWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<SessionExpiryWorker> _log;

        public SessionExpiryWorker(IServiceScopeFactory scopes, ILogger<SessionExpiryWorker> log)
        {
            _scopes = scopes;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    try
                    {
                        using var scope = _scopes.CreateScope();
                        var sessions = scope.ServiceProvider.GetRequiredService<IExamSessionService>();
                        var n = await sessions.ExpireOverdueSessionsAsync();
                        if (n > 0) _log.LogInformation("Auto-submitted {Count} overdue exam session(s)", n);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _log.LogError(ex, "Overdue session sweep failed");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
        }
    }
}