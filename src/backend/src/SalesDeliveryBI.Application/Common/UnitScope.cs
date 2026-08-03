namespace SalesDeliveryBI.Application.Common;

/// <summary>
/// Result of <see cref="Abstractions.IUnitAccessGuard.Validate"/> — either no restriction
/// (caller has bi.quotation.viewAllUnits and passed no unitId) or a resolved set of unit ids
/// to filter every query by (never trust the raw request param past this point).
/// </summary>
public sealed class UnitScope
{
    private UnitScope(bool isUnrestricted, IReadOnlyCollection<Guid> unitIds)
    {
        IsUnrestricted = isUnrestricted;
        UnitIds = unitIds;
    }

    public bool IsUnrestricted { get; }
    public IReadOnlyCollection<Guid> UnitIds { get; }

    public static UnitScope Unrestricted() => new(true, Array.Empty<Guid>());

    public static UnitScope RestrictedTo(IReadOnlyCollection<Guid> unitIds) => new(false, unitIds);
}
