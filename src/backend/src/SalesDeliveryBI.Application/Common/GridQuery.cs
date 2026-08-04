namespace SalesDeliveryBI.Application.Common;

/// <summary>
/// Client-requested page/sort for a grid, bound from query params. Applied in-memory in the AppService layer
/// AFTER the cached dashboard fetch — the Redis cache key stays scoped to unit+date only (CacheKeys.cs), so
/// paging/sorting never multiplies cache entries or bypasses the CacheWarmupJob's fixed warm set.
/// </summary>
public sealed record GridQuery(int Page = 1, int PageSize = 10, string? SortField = null, bool SortDescending = false)
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 10;

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize => PageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => PageSize,
    };
}
