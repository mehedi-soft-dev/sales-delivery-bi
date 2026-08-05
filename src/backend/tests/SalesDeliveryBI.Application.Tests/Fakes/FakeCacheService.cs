using SalesDeliveryBI.Application.Abstractions;

namespace SalesDeliveryBI.Application.Tests.Fakes;

/// <summary>Pass-through — always a "miss" that runs the factory, so tests can inspect what QuotationAppService passed in.</summary>
internal sealed class FakeCacheService : ICacheService
{
    /// <summary>Every key passed in call order — GetConversionAsync now makes two calls (summary, then previous-period trend).</summary>
    public List<string> Keys { get; } = [];

    public string? LastKey { get; private set; }
    public TimeSpan? LastTtl { get; private set; }
    public int CallCount { get; private set; }

    public Task<T> GetOrSetAsync<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default)
    {
        Keys.Add(key);
        LastKey = key;
        LastTtl = ttl;
        CallCount++;
        return factory(cancellationToken);
    }
}
