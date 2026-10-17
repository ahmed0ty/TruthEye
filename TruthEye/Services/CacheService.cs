using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace TruthEye.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<CacheService> _logger;
        private readonly bool _useRedis;

        public CacheService(
            IMemoryCache memoryCache,
            IDistributedCache distributedCache,
            ILogger<CacheService> logger,
            IConfiguration configuration)
        {
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
            _logger = logger;
            _useRedis = !string.IsNullOrEmpty(configuration.GetConnectionString("Redis"));
        }

        // Distinguishes a real cache hit from a miss (a default(T) such as 0.0 is a valid cached value)
        private async Task<(bool Found, T? Value)> TryGetAsync<T>(string key)
        {
            try
            {
                if (_useRedis)
                {
                    var cachedData = await _distributedCache.GetStringAsync(key);
                    if (cachedData != null)
                    {
                        _logger.LogInformation("Cache HIT (Redis): {Key}", key);
                        return (true, JsonSerializer.Deserialize<T>(cachedData));
                    }
                }
                else
                {
                    if (_memoryCache.TryGetValue(key, out T? cachedValue))
                    {
                        _logger.LogInformation("Cache HIT (Memory): {Key}", key);
                        return (true, cachedValue);
                    }
                }

                _logger.LogInformation("Cache MISS: {Key}", key);
                return (false, default);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving from cache: {Key}", key);
                return (false, default);
            }
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var (found, value) = await TryGetAsync<T>(key);
            return found ? value : default;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                expiration ??= TimeSpan.FromMinutes(30);

                if (_useRedis)
                {
                    var serializedData = JsonSerializer.Serialize(value);
                    var options = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = expiration
                    };
                    await _distributedCache.SetStringAsync(key, serializedData, options);
                    _logger.LogInformation("Cached in Redis: {Key} for {Duration}", key, expiration);
                }
                else
                {
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = expiration
                    };
                    _memoryCache.Set(key, value, cacheOptions);
                    _logger.LogInformation("Cached in Memory: {Key} for {Duration}", key, expiration);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                if (_useRedis)
                {
                    await _distributedCache.RemoveAsync(key);
                    _logger.LogInformation("Removed from Redis: {Key}", key);
                }
                else
                {
                    _memoryCache.Remove(key);
                    _logger.LogInformation("Removed from Memory: {Key}", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from cache: {Key}", key);
            }
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            var (found, cached) = await TryGetAsync<T>(key);
            if (found)
            {
                return cached!;
            }

            var data = await factory();
            await SetAsync(key, data, expiration);
            return data;
        }
    }
}