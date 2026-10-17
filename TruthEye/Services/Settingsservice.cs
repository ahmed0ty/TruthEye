using Microsoft.Extensions.Caching.Memory;
using TruthEye.Entities;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Services
{
    public static class SettingKeys
    {
        public const string MaxViolationsBeforeFlag = "MaxViolationsBeforeFlag";
        public const string NoFaceSeconds = "NoFaceSeconds";
        public const string MultipleFacesSeconds = "MultipleFacesSeconds";
    }

    public interface ISettingsService
    {
        Task<int> GetIntAsync(string name, int fallback, int min, int max);
    }

    // Reads integer settings from the AIConfig table. Values are cached for 30 seconds,
    // so a change made in AI Settings takes effect within half a minute.
    public class SettingsService : ISettingsService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly IMemoryCache _cache;

        public SettingsService(IServiceScopeFactory scopes, IMemoryCache cache)
        {
            _scopes = scopes;
            _cache = cache;
        }

        public async Task<int> GetIntAsync(string name, int fallback, int min, int max)
        {
            var raw = await _cache.GetOrCreateAsync("setting:" + name, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                using var scope = _scopes.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var cfg = (await uow.Repository<AIConfig>().FindAsync(c => c.ParameterName == name)).FirstOrDefault();
                return cfg?.Value;
            });

            return int.TryParse(raw, out var v) ? Math.Clamp(v, min, max) : fallback;
        }
    }
}