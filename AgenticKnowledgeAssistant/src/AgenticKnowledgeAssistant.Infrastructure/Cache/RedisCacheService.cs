using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgenticKnowledgeAssistant.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AgenticKnowledgeAssistant.Infrastructure.Cache;

public class RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger) : IRedisCacheService
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedData = await cache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(cachedData))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(cachedData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Redis connection failed. Falling back.");
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10); // 10 minutes default
            }

            var serialized = JsonSerializer.Serialize(value);
            await cache.SetStringAsync(key, serialized, options, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write to Redis Cache.");
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove from Redis Cache.");
        }
    }
}
