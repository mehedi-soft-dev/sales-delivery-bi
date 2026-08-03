using System.Globalization;

namespace SalesDeliveryBI.Application.Common;

/// <summary>
/// Cache key = dashboard name + hash of filter params, per docs/plans/backend/architecture.md.
/// Unit ids are sorted so the same effective scope always hashes to the same key regardless of set order.
/// Public so Infrastructure's CacheWarmupJob (Phase 8) can populate the exact same keys QuotationAppService reads.
/// </summary>
public static class CacheKeys
{
    public static string Pipeline(UnitScope scope) => $"bi:sales:quotation:pipeline:{ScopeSegment(scope)}";

    public static string Conversion(UnitScope scope, DateOnly fromDate, DateOnly toDate) =>
        $"bi:sales:quotation:conversion:{ScopeSegment(scope)}:{Fmt(fromDate)}:{Fmt(toDate)}";

    public static string Aging(UnitScope scope) => $"bi:sales:quotation:aging:{ScopeSegment(scope)}";

    public static string Detail(Guid quotationId) => $"bi:sales:quotation:detail:{quotationId}";

    public static string Summary(UnitScope scope) => $"bi:sales:quotation:summary:{ScopeSegment(scope)}";

    private static string ScopeSegment(UnitScope scope) => scope.IsUnrestricted
        ? "unit:all"
        : $"unit:{string.Join(',', scope.UnitIds.OrderBy(id => id))}";

    private static string Fmt(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
