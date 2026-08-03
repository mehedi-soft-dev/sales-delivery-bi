namespace SalesDeliveryBI.Application.Common;

/// <summary>
/// Shared by QuotationAppService (request path) and Infrastructure's CacheWarmupJob (Phase 8) —
/// one definition so the two never drift apart.
/// </summary>
public static class DashboardCacheTtls
{
    public static readonly TimeSpan Pipeline = TimeSpan.FromMinutes(3);
    public static readonly TimeSpan Conversion = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan Aging = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Detail = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Summary = TimeSpan.FromMinutes(5);
}
