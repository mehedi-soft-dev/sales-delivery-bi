using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SalesDeliveryBI.Application.Abstractions;
using StackExchange.Redis;

namespace SalesDeliveryBI.Infrastructure.Caching;

/// <summary>
/// Cache-aside with stampede protection: a short-lived Redis lock (SET NX PX) guards recompute on a cache
/// miss, so a TTL expiry under concurrent load triggers one factory call, not a thundering herd on Postgres.
/// </summary>
public class RedisCacheService : ICacheService
{
    private static readonly TimeSpan _lockTtl = TimeSpan.FromMilliseconds(2000);
    private static readonly TimeSpan _lockRetryDelay = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan _lockWaitTimeout = TimeSpan.FromSeconds(5);

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    // Compare-and-delete: only release the lock if it's still the one we set — an expired lock may already
    // have been re-acquired by another request, and deleting it out from under them would defeat the point.
    private const string _releaseLockScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default)
    {
        IDatabase db = _redis.GetDatabase();

        RedisValue cached = await db.StringGetAsync(key);
        if (cached.HasValue)
        {
            return Deserialize<T>(cached);
        }

        return await RecomputeWithLockAsync(db, key, ttl, factory, cancellationToken);
    }

    private async Task<T> RecomputeWithLockAsync<T>(
        IDatabase db,
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken)
    {
        string lockKey = $"{key}:lock";
        string lockToken = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < _lockWaitTimeout)
        {
            if (await db.StringSetAsync(lockKey, lockToken, _lockTtl, When.NotExists))
            {
                return await ComputeAndCacheAsync(db, key, ttl, factory, lockKey, lockToken, cancellationToken);
            }

            await Task.Delay(_lockRetryDelay, cancellationToken);

            RedisValue cached = await db.StringGetAsync(key);
            if (cached.HasValue)
            {
                return Deserialize<T>(cached);
            }
        }

        _logger.LogWarning("Cache lock wait timed out for {CacheKey}; computing without a lock", key);
        return await factory(cancellationToken);
    }

    private static async Task<T> ComputeAndCacheAsync<T>(
        IDatabase db,
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        string lockKey,
        string lockToken,
        CancellationToken cancellationToken)
    {
        try
        {
            // Another request may have populated the cache while we were waiting for the lock.
            RedisValue cached = await db.StringGetAsync(key);
            if (cached.HasValue)
            {
                return Deserialize<T>(cached);
            }

            T value = await factory(cancellationToken);
            await db.StringSetAsync(key, Serialize(value), ttl);
            return value;
        }
        finally
        {
            await db.ScriptEvaluateAsync(_releaseLockScript, [lockKey], [lockToken]);
        }
    }

    private static RedisValue Serialize<T>(T value) => JsonSerializer.Serialize(value, _jsonOptions);

    private static T Deserialize<T>(RedisValue value) => JsonSerializer.Deserialize<T>((string)value!, _jsonOptions)!;
}
