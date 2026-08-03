namespace SalesDeliveryBI.Application.Abstractions;

/// <summary>Cache-aside with stampede protection (implemented by RedisCacheService, Infrastructure/Caching).</summary>
public interface ICacheService
{
    Task<T> GetOrSetAsync<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default);
}
