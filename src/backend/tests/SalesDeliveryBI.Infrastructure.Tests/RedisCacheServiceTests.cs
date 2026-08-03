using Microsoft.Extensions.Logging.Abstractions;
using SalesDeliveryBI.Infrastructure.Caching;
using StackExchange.Redis;

namespace SalesDeliveryBI.Infrastructure.Tests;

/// <summary>Runs against the real dev Redis (docs/plans/backend/local-environment.md), not a mock.</summary>
public class RedisCacheServiceTests
{
    private const string ConnectionString = "127.0.0.1:6381";

    private static RedisCacheService CreateService(out IConnectionMultiplexer redis)
    {
        redis = ConnectionMultiplexer.Connect(ConnectionString);
        return new RedisCacheService(redis, NullLogger<RedisCacheService>.Instance);
    }

    [Fact]
    public async Task GetOrSetAsync_CacheMiss_CallsFactoryOnceThenServesFromCache()
    {
        RedisCacheService cache = CreateService(out IConnectionMultiplexer redis);
        string key = $"test:{Guid.NewGuid()}";
        int callCount = 0;

        int first = await cache.GetOrSetAsync(key, TimeSpan.FromSeconds(30), _ =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(42);
        });

        int second = await cache.GetOrSetAsync(key, TimeSpan.FromSeconds(30), _ =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(99); // must not run — this call should be a cache hit
        });

        Assert.Equal(42, first);
        Assert.Equal(42, second);
        Assert.Equal(1, callCount);

        await redis.GetDatabase().KeyDeleteAsync(key);
        redis.Dispose();
    }

    [Fact]
    public async Task GetOrSetAsync_ConcurrentRequestsOnColdCache_FactoryRunsExactlyOnce()
    {
        RedisCacheService cache = CreateService(out IConnectionMultiplexer redis);
        string key = $"test:stampede:{Guid.NewGuid()}";
        int callCount = 0;

        async Task<int> Factory(CancellationToken ct)
        {
            Interlocked.Increment(ref callCount);
            await Task.Delay(300, ct);
            return 7;
        }

        Task<int>[] tasks = Enumerable.Range(0, 10)
            .Select(_ => cache.GetOrSetAsync(key, TimeSpan.FromSeconds(30), Factory))
            .ToArray();

        int[] results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal(7, r));
        Assert.Equal(1, callCount);

        await redis.GetDatabase().KeyDeleteAsync(key);
        redis.Dispose();
    }

    [Fact]
    public async Task GetOrSetAsync_CachesNullValue_WithoutRecomputing()
    {
        RedisCacheService cache = CreateService(out IConnectionMultiplexer redis);
        string key = $"test:null:{Guid.NewGuid()}";
        int callCount = 0;

        string? first = await cache.GetOrSetAsync<string?>(key, TimeSpan.FromSeconds(30), _ =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult<string?>(null);
        });

        string? second = await cache.GetOrSetAsync<string?>(key, TimeSpan.FromSeconds(30), _ =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult<string?>("must not run");
        });

        Assert.Null(first);
        Assert.Null(second);
        Assert.Equal(1, callCount);

        await redis.GetDatabase().KeyDeleteAsync(key);
        redis.Dispose();
    }
}
