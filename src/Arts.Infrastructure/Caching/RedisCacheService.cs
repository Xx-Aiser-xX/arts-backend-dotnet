using System.Text.Json;
using Arts.Application.Interfaces;
using Arts.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Arts.Infrastructure.Caching;

public class RedisCacheService(
    IDistributedCache distributedCache,
    IConnectionMultiplexer redisConnection,
    IOptions<CacheSettings> cacheSettings,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly CacheSettings _settings = cacheSettings.Value;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public Task<T> GetOrCreateAsync<T>(string key, string policyName, Func<Task<T>> factory, CancellationToken ct = default)
    {
        var ttl = _settings.GetTtl(policyName);
        return GetOrCreateAsync(key, factory, ttl, ct);
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        try
        {
            var cachedBytes = await distributedCache.GetAsync(key, ct);
            if (cachedBytes is not null && cachedBytes.Length > 0)
            {
                var cachedData = JsonSerializer.Deserialize<T>(cachedBytes, JsonOptions);
                if (cachedData is not null)
                    return cachedData;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis недоступен при чтении ключа '{CacheKey}'. Запрос перенаправлен напрямую в PostgreSQL", key);
        }

        var freshData = await factory();
        if (freshData is null)
            return default!;

        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(_settings.DefaultTtlMinutes)
            };

            var serializedBytes = JsonSerializer.SerializeToUtf8Bytes(freshData, JsonOptions);
            await distributedCache.SetAsync(key, serializedBytes, options, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось записать кэш в Redis по ключу '{CacheKey}'", key);
        }
        return freshData;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default) 
        => distributedCache.RemoveAsync(key, ct);

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        var endpoints = redisConnection.GetEndPoints();
        var server = redisConnection.GetServer(endpoints.First());
        var db = redisConnection.GetDatabase();

        var keys = server.Keys(pattern: $"{prefix}*").ToArray();
        if (keys.Length > 0)
            await db.KeyDeleteAsync(keys);
    }
}