namespace SalesDeliveryBI.Application.Common;

/// <summary>
/// Bound from appsettings' "CacheTtls" section (Infrastructure/DependencyInjection.cs) — dashboard cache
/// lifetimes are ops-tunable config, not compiled constants. Property values are the fallback used for
/// any key the config section doesn't set. Shared by QuotationAppService (request path) and
/// Infrastructure's CacheWarmupJob (Phase 8) so the two never drift apart.
/// </summary>
public class CacheTtlOptions
{
    public TimeSpan Pipeline { get; set; } = TimeSpan.FromMinutes(3);
    public TimeSpan Conversion { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan Aging { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan Detail { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan Summary { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan Units { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Per docs/requirements/Sales_Delivery_BI_Implementation_Proposal.md §9 ("Order Backlog/Fulfillment").</summary>
    public TimeSpan SalesOrder { get; set; } = TimeSpan.FromMinutes(5);
}
