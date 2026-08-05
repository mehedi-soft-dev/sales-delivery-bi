using System.Globalization;

namespace SalesDeliveryBI.Application.Common;

/// <summary>
/// Cache key = dashboard name + hash of filter params, per docs/plans/backend/architecture.md.
/// Unit ids are sorted so the same effective scope always hashes to the same key regardless of set order.
/// Public so Infrastructure's CacheWarmupJob (Phase 8) can populate the exact same keys QuotationAppService reads.
/// </summary>
public static class CacheKeys
{
    public static string Pipeline(UnitScope scope, bool includeDraft, DateOnly? fromDate, DateOnly? toDate) =>
        $"bi:sales:quotation:pipeline:{ScopeSegment(scope)}:draft:{includeDraft}:{DateRangeSegment(fromDate, toDate)}";

    public static string Conversion(UnitScope scope, DateOnly fromDate, DateOnly toDate) =>
        $"bi:sales:quotation:conversion:{ScopeSegment(scope)}:{Fmt(fromDate)}:{Fmt(toDate)}";

    /// <summary>Backs the trend chart's "previous period" comparison series (docs/requirements §4.2) — a distinct key from Conversion() since it's a separate, narrower cached query (trend only, no KPIs/buyer-performance).</summary>
    public static string ConversionTrend(UnitScope scope, DateOnly fromDate, DateOnly toDate) =>
        $"bi:sales:quotation:conversion:trend:{ScopeSegment(scope)}:{Fmt(fromDate)}:{Fmt(toDate)}";

    public static string Aging(UnitScope scope, bool includeDraft, DateOnly? fromDate, DateOnly? toDate) =>
        $"bi:sales:quotation:aging:{ScopeSegment(scope)}:draft:{includeDraft}:{DateRangeSegment(fromDate, toDate)}";

    public static string Detail(Guid quotationId) => $"bi:sales:quotation:detail:{quotationId}";

    public static string Summary(UnitScope scope) => $"bi:sales:quotation:summary:{ScopeSegment(scope)}";

    public static string Units(UnitScope scope) => $"bi:sales:quotation:units:{ScopeSegment(scope)}";

    public static string SalesOrder(UnitScope scope) => $"bi:sales:order:summary:{ScopeSegment(scope)}";

    public static string Delivery(UnitScope scope) => $"bi:sales:delivery:performance:{ScopeSegment(scope)}";

    public static string Invoice(UnitScope scope) => $"bi:sales:invoice:summary:{ScopeSegment(scope)}";

    public static string Return(UnitScope scope) => $"bi:sales:return:summary:{ScopeSegment(scope)}";

    private static string ScopeSegment(UnitScope scope) => scope.IsUnrestricted
        ? "unit:all"
        : $"unit:{string.Join(',', scope.UnitIds.OrderBy(id => id))}";

    private static string DateRangeSegment(DateOnly? fromDate, DateOnly? toDate) =>
        $"{(fromDate.HasValue ? Fmt(fromDate.Value) : "any")}:{(toDate.HasValue ? Fmt(toDate.Value) : "any")}";

    private static string Fmt(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
